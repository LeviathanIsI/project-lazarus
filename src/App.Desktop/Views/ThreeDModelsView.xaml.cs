// ThreeDModelsView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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
                _hxRoot = new GroupModel3D();
                _hxViewport.Items.Add(new AmbientLight3D { Color = System.Windows.Media.Colors.Gray });
                _hxViewport.Items.Add(new DirectionalLight3D { Color = System.Windows.Media.Colors.White, Direction = new System.Windows.Media.Media3D.Vector3D(-1, -1, -2) });
                _hxViewport.Items.Add(new DirectionalLight3D { Color = System.Windows.Media.Colors.LightGray, Direction = new System.Windows.Media.Media3D.Vector3D(1, 1, 2) });
                _hxViewport.Items.Add(_hxRoot);
                HelixHost.Children.Clear();
                HelixHost.Children.Add(_hxViewport);
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

                var mat = HelixToolkit.Wpf.SharpDX.PhongMaterials.Gray;
                foreach (var mesh in scene.Meshes)
                {
                    if (mesh.VertexCount <= 0 || mesh.FaceCount <= 0) continue;

                    var positions = new SharpDX.Vector3[mesh.VertexCount];
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        var v = mesh.Vertices[i];
                        positions[i] = new SharpDX.Vector3(v.X, v.Y, v.Z);
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

                    var model = new HelixToolkit.Wpf.SharpDX.MeshGeometryModel3D
                    {
                        Geometry = geom,
                        Material = mat,
                        CullMode = SharpDX.Direct3D11.CullMode.Back
                    };
                    _hxRoot?.Children.Add(model);
                }

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
