using GeometryKit.Analysis;
using GeometryKit.Core;
using GeometryKit.Mesh;

namespace GeometryKit.Fabrication;

public sealed record CurvatureSizingOptions(double MaximumChordError, double MinimumEdgeLength, double MaximumEdgeLength)
{
    public CurvatureSizingOptions Validate()
    {
        if (MaximumChordError <= 0) throw new ArgumentOutOfRangeException(nameof(MaximumChordError));
        if (MinimumEdgeLength <= 0 || MaximumEdgeLength < MinimumEdgeLength) throw new ArgumentOutOfRangeException(nameof(MinimumEdgeLength));
        return this;
    }
}

/// <summary>
/// Generates a per-vertex target edge length for an external remesher. This package does not yet
/// mutate connectivity; returning the sizing field preserves deterministic and host-safe behaviour.
/// </summary>
public static class CurvatureSizing
{
    public static IReadOnlyList<double> TargetEdgeLengths(TriangleMesh mesh, CurvatureSizingOptions options, GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        options = options.Validate();
        var curvature = SurfaceAnalysis.GaussianCurvature(mesh, tolerance);
        var target = new double[mesh.VertexCount];
        for (var i = 0; i < target.Length; i++)
        {
            // For small circular arcs, sagitta e ≈ |k| h² / 8; use |K|½ as a local curvature scale.
            var kappa = Math.Sqrt(Math.Abs(curvature.Density[i]));
            var raw = kappa <= 1e-15 ? options.MaximumEdgeLength : Math.Sqrt(8.0 * options.MaximumChordError / kappa);
            target[i] = Math.Clamp(raw, options.MinimumEdgeLength, options.MaximumEdgeLength);
        }
        return target;
    }
}
