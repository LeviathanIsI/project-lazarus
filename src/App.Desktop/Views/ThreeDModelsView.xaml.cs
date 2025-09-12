// ThreeDModelsView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using Lazarus.Desktop.ViewModels;
using Assimp;
using HelixToolkit.Wpf.SharpDX;

namespace Lazarus.Desktop.Views
{
    public partial class ThreeDModelsView : UserControl
    {
        private Model3DGroup _modelGroup = new Model3DGroup();
        private HelixToolkit.Wpf.SharpDX.DefaultEffectsManager? _effectsManager;
        private Viewport3DX? _hxViewport;
        private GroupModel3D? _hxRoot;
        private System.Windows.Point _lastMouse;
        private bool _isOrbit, _isPan, _isZoom;
        private Point3D _pivot = new Point3D(0,0,0);
        private Grid? _helixHost;
        private ToggleButton? _wireframeToggle;
        private LineGeometryModel3D? _gridModel;
        private GroupModel3D? _axisGroup;
        private double _lastSceneExtent = 1.0;

        public ThreeDModelsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize Helix viewport (DX11) in code
            try
            {
                _effectsManager = new HelixToolkit.Wpf.SharpDX.DefaultEffectsManager();
                _hxViewport = new Viewport3DX
                {
                    EffectsManager = _effectsManager,
                    Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
                    {
                        Position = new System.Windows.Media.Media3D.Point3D(0, 1, 3),
                        LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -0.3, -3),
                        UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                        FieldOfView = 45
                    },
                    IsShadowMappingEnabled = false
                };
                _hxViewport.BackgroundColor = System.Windows.Media.Color.FromRgb(26, 26, 31);
                _hxRoot = new GroupModel3D();
                _hxViewport.Items.Add(new AmbientLight3D { Color = System.Windows.Media.Colors.Gray });
                _hxViewport.Items.Add(new DirectionalLight3D { Color = System.Windows.Media.Colors.White, Direction = new System.Windows.Media.Media3D.Vector3D(-1, -1, -2) });
                _hxViewport.Items.Add(new DirectionalLight3D { Color = System.Windows.Media.Colors.LightGray, Direction = new System.Windows.Media.Media3D.Vector3D(1, 1, 2) });
                _hxViewport.Items.Add(_hxRoot);

                _helixHost = FindName("HelixHost") as Grid;
                _helixHost?.Children.Clear();
                _helixHost?.Children.Add(_hxViewport);

                _wireframeToggle = FindName("WireframeToggle") as ToggleButton;
                // Initialize grid/axis toggles state
                var gridToggle = FindName("GridToggle") as ToggleButton;
                var axisToggle = FindName("AxisToggle") as ToggleButton;
                if (gridToggle != null) gridToggle.IsChecked = true;
                if (axisToggle != null) axisToggle.IsChecked = true;

                // Hook custom Maya-style mouse controls (Alt+LMB/MMB/RMB)
                _hxViewport.PreviewMouseDown += OnViewportMouseDown;
                _hxViewport.PreviewMouseMove += OnViewportMouseMove;
                _hxViewport.PreviewMouseUp += OnViewportMouseUp;

                // Disable default Helix mouse gestures to avoid conflicts
                // Default Helix gestures are suppressed by handling Preview* events
            }
            catch { }
            HookPreviewLoader();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            HookPreviewLoader();
        }

        private void HookPreviewLoader()
        {
            if (DataContext is ThreeDModelsViewModel vm)
            {
                vm.SetPreviewLoader(LoadModel);
            }
        }

        private bool LoadModel(string? path)
        {
            try
            {
                _modelGroup.Children.Clear();
                try { _hxRoot?.Children.Clear(); } catch { }
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                {
                    PreviewOverlay.Visibility = Visibility.Visible;
                    return false;
                }

                var ext = System.IO.Path.GetExtension(path) ?? string.Empty;
                Model3D? model = null;
                if (string.Equals(ext, ".obj", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".stl", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".fbx", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".gltf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".glb", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryLoadWithHelixAssimp(path))
                    {
                        FitHelixToView();
                        PreviewOverlay.Visibility = Visibility.Collapsed;
                        return true;
                    }
                }

                if (model != null)
                {
                    _modelGroup.Children.Add(model);
                    FitToView(_modelGroup.Bounds);
                    PreviewOverlay.Visibility = Visibility.Collapsed;
                    return true;
                }

                PreviewHint.Text = "Preview not available (see logs).";
                PreviewOverlay.Visibility = Visibility.Visible;
                return false;
            }
            catch
            {
                PreviewHint.Text = "Failed to load model.";
                PreviewOverlay.Visibility = Visibility.Visible;
                return false;
            }
        }

        private bool TryLoadWithHelixAssimp(string path)
        {
            try
            {
                using var ctx = new AssimpContext();
                var flags = PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.ImproveCacheLocality;
                var scene = ctx.ImportFile(path, flags);
                if (scene == null || !scene.HasMeshes) return false;

                var defaultMat = HelixToolkit.Wpf.SharpDX.PhongMaterials.Gray;
                // Track bounds for pivot centering
                var min = new SharpDX.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new SharpDX.Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var mesh in scene.Meshes)
                {
                    if (mesh.VertexCount <= 0 || mesh.FaceCount <= 0) continue;

                    var positions = new SharpDX.Vector3[mesh.VertexCount];
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        var v = mesh.Vertices[i];
                        positions[i] = new SharpDX.Vector3(v.X, v.Y, v.Z);
                        if (positions[i].X < min.X) min.X = positions[i].X;
                        if (positions[i].Y < min.Y) min.Y = positions[i].Y;
                        if (positions[i].Z < min.Z) min.Z = positions[i].Z;
                        if (positions[i].X > max.X) max.X = positions[i].X;
                        if (positions[i].Y > max.Y) max.Y = positions[i].Y;
                        if (positions[i].Z > max.Z) max.Z = positions[i].Z;
                    }

                    var indicesList = new System.Collections.Generic.List<int>();
                    foreach (var face in mesh.Faces)
                    {
                        if (face.IndexCount == 3)
                        {
                            indicesList.Add(face.Indices[0]);
                            indicesList.Add(face.Indices[1]);
                            indicesList.Add(face.Indices[2]);
                        }
                        else if (face.IndexCount > 3)
                        {
                            for (int k = 1; k < face.IndexCount - 1; k++)
                            {
                                indicesList.Add(face.Indices[0]);
                                indicesList.Add(face.Indices[k]);
                                indicesList.Add(face.Indices[k + 1]);
                            }
                        }
                    }

                    if (positions.Length == 0 || indicesList.Count == 0) continue;

                    var geom = new HelixToolkit.Wpf.SharpDX.MeshGeometry3D
                    {
                        Positions = new HelixToolkit.Wpf.SharpDX.Vector3Collection(positions),
                        Indices = new HelixToolkit.Wpf.SharpDX.IntCollection(indicesList)
                    };

                    // Material: attempt diffuse map from Assimp materials
                    HelixToolkit.Wpf.SharpDX.Material? mat = defaultMat;
                    try
                    {
                        if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < scene.Materials.Count)
                        {
                            var am = scene.Materials[mesh.MaterialIndex];
                            if (am.HasTextureDiffuse)
                            {
                                var tex = am.TextureDiffuse;
                                var baseDir = System.IO.Path.GetDirectoryName(path);
                                var texPath = System.IO.Path.Combine(baseDir ?? string.Empty, tex.FilePath);
                                if (System.IO.File.Exists(texPath))
                                {
                                    using var fs = System.IO.File.OpenRead(texPath);
                                    var pm = new HelixToolkit.Wpf.SharpDX.PhongMaterial
                                    {
                                        DiffuseColor = new SharpDX.Color4(1,1,1,1),
                                        DiffuseMap = new HelixToolkit.Wpf.SharpDX.TextureModel(fs)
                                    };
                                    mat = pm;
                                }
                            }
                            else if (am.HasColorDiffuse)
                            {
                                var c = am.ColorDiffuse; // Assimp Color4D
                                mat = new HelixToolkit.Wpf.SharpDX.PhongMaterial { DiffuseColor = new SharpDX.Color4(c.R, c.G, c.B, c.A) };
                            }
                        }
                    }
                    catch { mat = defaultMat; }

                    var model = new HelixToolkit.Wpf.SharpDX.MeshGeometryModel3D
                    {
                        Geometry = geom,
                        Material = mat,
                        CullMode = SharpDX.Direct3D11.CullMode.Back
                    };
                    _hxRoot?.Children.Add(model);
                }

                // Set pivot to scene center
                var cx = (min.X + max.X) * 0.5f; var cy = (min.Y + max.Y) * 0.5f; var cz = (min.Z + max.Z) * 0.5f;
                _pivot = new Point3D(cx, cy, cz);
                _lastSceneExtent = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));

                // Build grid/axis according to current extents
                var gridEnabled = (FindName("GridToggle") as ToggleButton)?.IsChecked ?? true;
                var axisEnabled = (FindName("AxisToggle") as ToggleButton)?.IsChecked ?? true;
                if (gridEnabled) EnsureGrid(new Point3D(cx, min.Y, cz), _lastSceneExtent);
                if (axisEnabled) EnsureAxis(new Point3D(cx, cy, cz), _lastSceneExtent * 0.6);
                return _hxRoot != null && _hxRoot.Children.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void FitHelixToView()
        {
            try { _hxViewport?.ZoomExtents(); } catch { }
        }

        // UI Actions
        private void OnResetView(object sender, RoutedEventArgs e) => FitHelixToView();

        private void OnToggleWireframe(object sender, RoutedEventArgs e)
        {
            try
            {
                var tgl = (sender as ToggleButton) ?? _wireframeToggle;
                bool enabled = (tgl?.IsChecked ?? false);
                ApplyWireframe(_hxRoot, enabled);
            }
            catch { }
        }

        private static void ApplyWireframe(GroupModel3D? root, bool enabled)
        {
            if (root == null) return;
            foreach (var child in root.Children)
            {
                if (child is MeshGeometryModel3D m)
                {
                    m.RenderWireframe = enabled;
                    // Set color so the lines pop against dark background
                    m.WireframeColor = System.Windows.Media.Colors.LightSkyBlue;
                }
                else if (child is GroupModel3D g)
                {
                    ApplyWireframe(g, enabled);
                }
            }
        }

        // Grid and Axis
        private void EnsureGrid(Point3D center, double size)
        {
            if (_hxRoot == null) return;
            var half = Math.Max(1.0, size * 0.75);
            var step = Math.Max(half / 10.0, 0.1);
            var positions = new HelixToolkit.Wpf.SharpDX.Vector3Collection();
            var indices = new HelixToolkit.Wpf.SharpDX.IntCollection();
            int idx = 0;
            for (double x = -half; x <= half + 1e-6; x += step)
            {
                positions.Add(new SharpDX.Vector3((float)(center.X + x), (float)center.Y, (float)(center.Z - half)));
                positions.Add(new SharpDX.Vector3((float)(center.X + x), (float)center.Y, (float)(center.Z + half)));
                indices.Add(idx++); indices.Add(idx++);
            }
            for (double z = -half; z <= half + 1e-6; z += step)
            {
                positions.Add(new SharpDX.Vector3((float)(center.X - half), (float)center.Y, (float)(center.Z + z)));
                positions.Add(new SharpDX.Vector3((float)(center.X + half), (float)center.Y, (float)(center.Z + z)));
                indices.Add(idx++); indices.Add(idx++);
            }
            var geom = new LineGeometry3D { Positions = positions, Indices = indices };
            _gridModel ??= new LineGeometryModel3D { Color = System.Windows.Media.Colors.DimGray, Thickness = 0.8, IsHitTestVisible = false };
            _gridModel.Geometry = geom;
            if (!_hxRoot.Children.Contains(_gridModel)) _hxRoot.Children.Add(_gridModel);
        }

        private void EnsureAxis(Point3D origin, double length)
        {
            if (_hxRoot == null) return;
            _axisGroup ??= new GroupModel3D();
            _axisGroup.Children.Clear();
            // X (red)
            _axisGroup.Children.Add(BuildAxisLine(origin, new System.Windows.Media.Media3D.Vector3D(length, 0, 0), System.Windows.Media.Colors.IndianRed));
            // Y (green)
            _axisGroup.Children.Add(BuildAxisLine(origin, new System.Windows.Media.Media3D.Vector3D(0, length, 0), System.Windows.Media.Colors.DarkSeaGreen));
            // Z (blue)
            _axisGroup.Children.Add(BuildAxisLine(origin, new System.Windows.Media.Media3D.Vector3D(0, 0, length), System.Windows.Media.Colors.SteelBlue));
            if (!_hxRoot.Children.Contains(_axisGroup)) _hxRoot.Children.Add(_axisGroup);
        }

        private static LineGeometryModel3D BuildAxisLine(Point3D origin, System.Windows.Media.Media3D.Vector3D dir, System.Windows.Media.Color color)
        {
            var positions = new HelixToolkit.Wpf.SharpDX.Vector3Collection
            {
                new SharpDX.Vector3((float)origin.X, (float)origin.Y, (float)origin.Z),
                new SharpDX.Vector3((float)(origin.X + dir.X), (float)(origin.Y + dir.Y), (float)(origin.Z + dir.Z))
            };
            var indices = new HelixToolkit.Wpf.SharpDX.IntCollection { 0, 1 };
            var geom = new LineGeometry3D { Positions = positions, Indices = indices };
            return new LineGeometryModel3D { Geometry = geom, Color = color, Thickness = 1.5, IsHitTestVisible = false };
        }

        private void OnToggleGrid(object sender, RoutedEventArgs e)
        {
            var tgl = sender as ToggleButton; var enable = tgl?.IsChecked ?? false;
            if (_hxRoot == null) return;
            if (enable)
            {
                if (_gridModel == null) EnsureGrid(_pivot, Math.Max(1.0, _lastSceneExtent));
                else if (!_hxRoot.Children.Contains(_gridModel)) _hxRoot.Children.Add(_gridModel);
            }
            else
            {
                if (_gridModel != null && _hxRoot.Children.Contains(_gridModel)) _hxRoot.Children.Remove(_gridModel);
            }
        }

        private void OnToggleAxis(object sender, RoutedEventArgs e)
        {
            var tgl = sender as ToggleButton; var enable = tgl?.IsChecked ?? false;
            if (_hxRoot == null) return;
            if (enable)
            {
                if (_axisGroup == null || _axisGroup.Children.Count == 0) EnsureAxis(_pivot, Math.Max(0.6, _lastSceneExtent * 0.6));
                else if (!_hxRoot.Children.Contains(_axisGroup)) _hxRoot.Children.Add(_axisGroup);
            }
            else
            {
                if (_axisGroup != null && _hxRoot.Children.Contains(_axisGroup)) _hxRoot.Children.Remove(_axisGroup);
            }
        }

        // View presets
        private void OnViewFront(object sender, RoutedEventArgs e)
        {
            if (_hxViewport?.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam) return;
            var dist = Math.Max(1.0, _lastSceneExtent * 1.8);
            cam.Position = new Point3D(_pivot.X, _pivot.Y, _pivot.Z + dist);
            cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(_pivot.X - cam.Position.X, _pivot.Y - cam.Position.Y, _pivot.Z - cam.Position.Z);
            cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
        }

        private void OnViewTop(object sender, RoutedEventArgs e)
        {
            if (_hxViewport?.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam) return;
            var dist = Math.Max(1.0, _lastSceneExtent * 1.8);
            cam.Position = new Point3D(_pivot.X, _pivot.Y + dist, _pivot.Z);
            cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(_pivot.X - cam.Position.X, _pivot.Y - cam.Position.Y, _pivot.Z - cam.Position.Z);
            cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -1);
        }

        private void OnViewIso(object sender, RoutedEventArgs e)
        {
            if (_hxViewport?.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam) return;
            var dist = Math.Max(1.0, _lastSceneExtent * 2.2);
            var dir = new System.Windows.Media.Media3D.Vector3D(1, 1, 1); dir.Normalize();
            cam.Position = new Point3D(_pivot.X + dir.X * dist, _pivot.Y + dir.Y * dist, _pivot.Z + dir.Z * dist);
            cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(_pivot.X - cam.Position.X, _pivot.Y - cam.Position.Y, _pivot.Z - cam.Position.Z);
            cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
        }

        // Maya-style navigation handlers
        private void OnViewportMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_hxViewport == null) return;
            if (Keyboard.Modifiers != ModifierKeys.Alt)
            {
                // Block default Helix gestures so we only use Maya-style
                e.Handled = true; return;
            }
            _lastMouse = e.GetPosition(_hxViewport);
            _isOrbit = e.ChangedButton == System.Windows.Input.MouseButton.Left;
            _isPan   = e.ChangedButton == System.Windows.Input.MouseButton.Middle;
            _isZoom  = e.ChangedButton == System.Windows.Input.MouseButton.Right;
            e.Handled = true;
        }

        private void OnViewportMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isOrbit = _isPan = _isZoom = false; e.Handled = true;
        }

        private void OnViewportMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_hxViewport == null || _hxViewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam) return;
            var cur = e.GetPosition(_hxViewport);
            var dx = cur.X - _lastMouse.X;
            var dy = cur.Y - _lastMouse.Y;
            if (_isOrbit || _isPan || _isZoom)
            {
                if (_isOrbit)
                {
                    Orbit(cam, dx, dy);
                }
                else if (_isPan)
                {
                    Pan(cam, dx, dy);
                }
                else if (_isZoom)
                {
                    Zoom(cam, dy);
                }
                _lastMouse = cur; e.Handled = true;
            }
        }

        private static System.Windows.Media.Media3D.Vector3D Normalize(System.Windows.Media.Media3D.Vector3D v)
        {
            if (v.Length == 0) return v; v.Normalize(); return v;
        }

        private void Orbit(HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam, double dx, double dy)
        {
            // Convert to degrees; sensitivity tuned for typical mice
            double yaw = dx * 0.25;   // around world up
            double pitch = -dy * 0.25; // around camera right

            var look = new System.Windows.Media.Media3D.Vector3D(cam.LookDirection.X, cam.LookDirection.Y, cam.LookDirection.Z);
            var up = new System.Windows.Media.Media3D.Vector3D(cam.UpDirection.X, cam.UpDirection.Y, cam.UpDirection.Z);
            var right = Normalize(System.Windows.Media.Media3D.Vector3D.CrossProduct(look, up));
            up = Normalize(System.Windows.Media.Media3D.Vector3D.CrossProduct(right, look));

            var yawRot = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(0, 1, 0), yaw), _pivot);
            var pitchRot = new RotateTransform3D(new AxisAngleRotation3D(right, pitch), _pivot);
            var tg = new Transform3DGroup(); tg.Children.Add(yawRot); tg.Children.Add(pitchRot);

            // Rotate position around pivot
            var newPos = tg.Transform(cam.Position);
            // Rotate orientation vectors (around origin)
            var yawMat = new RotateTransform3D(new AxisAngleRotation3D(new System.Windows.Media.Media3D.Vector3D(0, 1, 0), yaw)).Value;
            var pitchMat = new RotateTransform3D(new AxisAngleRotation3D(right, pitch)).Value;
            var m = yawMat; m.Append(pitchMat);
            var upV = m.Transform(cam.UpDirection);

            cam.Position = newPos;
            cam.UpDirection = Normalize(new System.Windows.Media.Media3D.Vector3D(upV.X, upV.Y, upV.Z));
            var newLook = new System.Windows.Media.Media3D.Vector3D(_pivot.X - cam.Position.X, _pivot.Y - cam.Position.Y, _pivot.Z - cam.Position.Z);
            cam.LookDirection = newLook;
        }

        private void Pan(HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam, double dx, double dy)
        {
            var look = new System.Windows.Media.Media3D.Vector3D(cam.LookDirection.X, cam.LookDirection.Y, cam.LookDirection.Z);
            var up = new System.Windows.Media.Media3D.Vector3D(cam.UpDirection.X, cam.UpDirection.Y, cam.UpDirection.Z);
            var right = Normalize(System.Windows.Media.Media3D.Vector3D.CrossProduct(look, up));
            up = Normalize(System.Windows.Media.Media3D.Vector3D.CrossProduct(right, look));
            var dist = look.Length; var scale = dist * 0.0015; // tune pan speed
            var t = (-dx * scale) * right + (dy * scale) * up;
            cam.Position += t; _pivot += t; cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(_pivot.X - cam.Position.X, _pivot.Y - cam.Position.Y, _pivot.Z - cam.Position.Z);
        }

        private void Zoom(HelixToolkit.Wpf.SharpDX.PerspectiveCamera cam, double dy)
        {
            var look = new System.Windows.Media.Media3D.Vector3D(cam.LookDirection.X, cam.LookDirection.Y, cam.LookDirection.Z);
            var dist = Math.Max(0.01, look.Length);
            var factor = Math.Exp(dy * 0.002); // smooth exponential zoom
            var newDist = Math.Min(1e6, Math.Max(0.01, dist * factor));
            var dir = look; dir.Normalize();
            cam.LookDirection = dir * newDist;
            cam.Position = new Point3D(_pivot.X - cam.LookDirection.X, _pivot.Y - cam.LookDirection.Y, _pivot.Z - cam.LookDirection.Z);
        }

        // Helix path removed in this pass

        private Model3D? LoadWithAssimp(string path)
        {
            try
            {
                using var ctx = new AssimpContext();
                var flags = PostProcessSteps.Triangulate
                         | PostProcessSteps.GenerateSmoothNormals
                         | PostProcessSteps.JoinIdenticalVertices
                         | PostProcessSteps.ImproveCacheLocality;
                var scene = ctx.ImportFile(path, flags);
                if (scene == null || !scene.HasMeshes) return null;

                var group = new Model3DGroup();
                var mat = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));

                foreach (var mesh in scene.Meshes)
                {
                    if (mesh.VertexCount <= 0 || mesh.FaceCount <= 0) continue;

                    var positions = new Point3DCollection(mesh.VertexCount);
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        var v = mesh.Vertices[i];
                        positions.Add(new Point3D(v.X, v.Y, v.Z));
                    }

                    var indices = new Int32Collection();
                    foreach (var face in mesh.Faces)
                    {
                        // We requested triangulation; guard anyway
                        if (face.IndexCount == 3)
                        {
                            indices.Add(face.Indices[0]);
                            indices.Add(face.Indices[1]);
                            indices.Add(face.Indices[2]);
                        }
                        else if (face.IndexCount > 3)
                        {
                            // Simple fan triangulation fallback
                            for (int k = 1; k < face.IndexCount - 1; k++)
                            {
                                indices.Add(face.Indices[0]);
                                indices.Add(face.Indices[k]);
                                indices.Add(face.Indices[k + 1]);
                            }
                        }
                    }

                    if (positions.Count > 0 && indices.Count > 0)
                    {
                        var mg = new System.Windows.Media.Media3D.MeshGeometry3D
                        {
                            Positions = positions,
                            TriangleIndices = indices
                        };
                        var gm = new System.Windows.Media.Media3D.GeometryModel3D(mg, mat) { BackMaterial = mat };
                        group.Children.Add(gm);
                    }
                }

                return group.Children.Count > 0 ? group : null;
            }
            catch
            {
                return null;
            }
        }

        private System.Windows.Media.Media3D.GeometryModel3D? LoadObj(string path)
        {
            var positions = new System.Collections.Generic.List<Point3D>();
            var triangles = new Int32Collection();
            try
            {
                foreach (var line in System.IO.File.ReadLines(path))
                {
                    var l = line.Trim();
                    if (l.Length == 0 || l.StartsWith("#")) continue;
                    var parts = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts[0] == "v" && parts.Length >= 4)
                    {
                        if (double.TryParse(parts[1], out var x) && double.TryParse(parts[2], out var y) && double.TryParse(parts[3], out var z))
                            positions.Add(new Point3D(x, y, z));
                    }
                    else if (parts[0] == "f" && parts.Length >= 4)
                    {
                        var idx = new int[parts.Length - 1];
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var tok = parts[i].Split('/');
                            if (int.TryParse(tok[0], out var vi))
                            {
                                if (vi < 0) vi = positions.Count + 1 + vi;
                                idx[i - 1] = vi - 1;
                            }
                        }
                        for (int t = 1; t < idx.Length - 1; t++)
                        {
                            triangles.Add(idx[0]); triangles.Add(idx[t]); triangles.Add(idx[t + 1]);
                        }
                    }
                }

                if (positions.Count == 0 || triangles.Count == 0) return null;
                var mesh = new System.Windows.Media.Media3D.MeshGeometry3D { Positions = new Point3DCollection(positions), TriangleIndices = triangles };
                var mat = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));
                var model = new System.Windows.Media.Media3D.GeometryModel3D(mesh, mat) { BackMaterial = mat };
                return model;
            }
            catch { return null; }
        }

        private System.Windows.Media.Media3D.GeometryModel3D? LoadAsciiStl(string path)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(path);
                var positions = new System.Collections.Generic.List<Point3D>();
                var triangles = new Int32Collection();
                int indexBase = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i].Trim();
                    if (l.StartsWith("vertex"))
                    {
                        var p1 = ParseVertex(lines[i]);
                        var p2 = ParseVertex(lines[++i]);
                        var p3 = ParseVertex(lines[++i]);
                        positions.Add(p1); positions.Add(p2); positions.Add(p3);
                        triangles.Add(indexBase++); triangles.Add(indexBase++); triangles.Add(indexBase++);
                    }
                }
                if (positions.Count == 0) return null;
                var mesh = new System.Windows.Media.Media3D.MeshGeometry3D { Positions = new Point3DCollection(positions), TriangleIndices = triangles };
                var mat = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));
                return new System.Windows.Media.Media3D.GeometryModel3D(mesh, mat) { BackMaterial = mat };
            }
            catch { return null; }
        }

        private static Point3D ParseVertex(string line)
        {
            var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double.TryParse(parts.Length > 1 ? parts[1] : "0", out var x);
            double.TryParse(parts.Length > 2 ? parts[2] : "0", out var y);
            double.TryParse(parts.Length > 3 ? parts[3] : "0", out var z);
            return new Point3D(x, y, z);
        }

        private void FitToView(Rect3D bounds)
        {
            try { _hxViewport?.ZoomExtents(); } catch { }
        }
    }
}
