using GeometryKit.Analysis;
using GeometryKit.Core;
using GeometryKit.Fabrication;
using GeometryKit.Mesh;
using Xunit;

namespace GeometryKit.Tests;

public sealed class GeometryKitWorkflowTests
{
    [Fact]
    public void Mesh_health_reports_a_single_open_component()
    {
        var mesh = OpenSquare();
        var report = MeshHealthAnalyzer.Analyse(mesh);

        Assert.True(report.IsQueryable);
        Assert.False(report.IsClosed);
        Assert.Equal(4, report.BoundaryEdgeCount);
        Assert.Equal(1, report.ConnectedComponentCount);
        Assert.Equal(1.0, report.SurfaceArea, 12);
    }

    [Fact]
    public void Spatial_index_projects_and_raycasts_against_a_mesh_snapshot()
    {
        var index = new MeshSpatialIndex(OpenSquare());
        var projection = index.ClosestPoint(new Point3d(0.25, 0.75, 2));
        var hit = index.Raycast(new Ray3d(new Point3d(0.25, 0.75, 2), new Point3d(0, 0, -1)));

        Assert.NotNull(projection);
        Assert.Equal(0.0, projection!.Point.Z, 12);
        Assert.Equal(2.0, projection.Distance, 12);
        Assert.NotNull(hit);
        Assert.Equal(2.0, hit!.Parameter, 12);
    }

    [Fact]
    public void Gaussian_curvature_satisfies_discrete_gauss_bonnet_for_a_tetrahedron()
    {
        var result = SurfaceAnalysis.GaussianCurvature(Tetrahedron());
        Assert.Equal(4.0 * Math.PI, result.TotalIntegratedCurvature, 10);
    }

    [Fact]
    public void Edge_geodesics_returns_shortest_paths_on_the_mesh_graph()
    {
        var result = SurfaceAnalysis.EdgeGeodesics(OpenSquare(), [0]);
        Assert.Equal(0, result.Distances[0], 12);
        Assert.Equal(1, result.Distances[1], 12);
        Assert.Equal(1, result.Distances[3], 12);
        Assert.Equal(Math.Sqrt(2), result.Distances[2], 12);
    }

    [Fact]
    public void Mesh_section_returns_triangle_plane_segments()
    {
        var result = MeshSectioner.Intersect(Tetrahedron(), new Plane3d(new Point3d(0, 0, 0.25), new Point3d(0, 0, 1)));
        Assert.Equal(3, result.Segments.Count);
        Assert.All(result.Segments, segment =>
        {
            Assert.Equal(0.25, segment.Start.Z, 12);
            Assert.Equal(0.25, segment.End.Z, 12);
        });
    }

    [Fact]
    public void Curvature_sizing_respects_requested_bounds()
    {
        var target = CurvatureSizing.TargetEdgeLengths(Tetrahedron(), new CurvatureSizingOptions(0.01, 0.05, 0.25));
        Assert.All(target, length => Assert.InRange(length, 0.05, 0.25));
    }

    private static TriangleMesh OpenSquare() => new(
        [new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0)],
        [new(0, 1, 2), new(0, 2, 3)]);

    private static TriangleMesh Tetrahedron() => new(
        [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(0, 0, 1)],
        [new(0, 2, 1), new(0, 1, 3), new(0, 3, 2), new(1, 2, 3)]);
}
