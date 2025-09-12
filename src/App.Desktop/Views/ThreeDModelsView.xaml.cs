// ThreeDModelsView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Lazarus.Desktop.ViewModels;
using Assimp;

namespace Lazarus.Desktop.Views
{
    public partial class ThreeDModelsView : UserControl
    {
        private Model3DGroup _modelGroup = new Model3DGroup();

        public ThreeDModelsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try { ModelRoot.Content = _modelGroup; } catch { }
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
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                {
                    PreviewOverlay.Visibility = Visibility.Visible;
                    return false;
                }

                var ext = System.IO.Path.GetExtension(path) ?? string.Empty;
                Model3D? model = null;
                if (string.Equals(ext, ".obj", StringComparison.OrdinalIgnoreCase))
                {
                    model = LoadObj(path);
                }
                else if (string.Equals(ext, ".stl", StringComparison.OrdinalIgnoreCase))
                {
                    model = LoadAsciiStl(path);
                }
                else if (string.Equals(ext, ".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    model = LoadWithAssimp(path);
                }

                if (model != null)
                {
                    _modelGroup.Children.Add(model);
                    FitToView(_modelGroup.Bounds);
                    PreviewOverlay.Visibility = Visibility.Collapsed;
                    return true;
                }

                PreviewHint.Text = "Preview not available for this format (try OBJ/STL/FBX).";
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
                var mat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));

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
                        var mg = new MeshGeometry3D
                        {
                            Positions = positions,
                            TriangleIndices = indices
                        };
                        var gm = new GeometryModel3D(mg, mat) { BackMaterial = mat };
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

        private GeometryModel3D? LoadObj(string path)
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
                var mesh = new MeshGeometry3D { Positions = new Point3DCollection(positions), TriangleIndices = triangles };
                var mat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));
                var model = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
                return model;
            }
            catch { return null; }
        }

        private GeometryModel3D? LoadAsciiStl(string path)
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
                var mesh = new MeshGeometry3D { Positions = new Point3DCollection(positions), TriangleIndices = triangles };
                var mat = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(210, 210, 210)));
                return new GeometryModel3D(mesh, mat) { BackMaterial = mat };
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
            if (bounds.IsEmpty) return;
            var size = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
            if (size <= 0) size = 1;
            var center = new Point3D(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);
            PreviewCamera.Position = new Point3D(center.X, center.Y, center.Z + size * 2.5);
            PreviewCamera.LookDirection = new System.Windows.Media.Media3D.Vector3D(
                center.X - PreviewCamera.Position.X,
                center.Y - PreviewCamera.Position.Y,
                center.Z - PreviewCamera.Position.Z);
            PreviewCamera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
        }
    }
}
