# GeometryKit

GeometryKit is the production-oriented C# track beside the DDG course companion. It focuses on
derived triangle-mesh work: health checks, spatial queries, discrete surface analysis and
fabrication-support outputs. It is **not** a B-Rep CAD kernel and it does not own a host document.

## Implemented vertical slice

| Workflow | API | Deliberate boundary |
|---|---|---|
| Mesh health and queries | `MeshHealthAnalyzer`, `MeshSpatialIndex` | Diagnostics are non-mutating; callers choose repair. |
| Surface analysis | `SurfaceAnalysis` | Curvature and graph-geodesic fields are derived from an immutable mesh snapshot. |
| Fabrication support | `MeshSectioner`, `CurvatureSizing` | Returns section segments and target edge-length fields; it does not silently edit connectivity. |

The public geometry values use `Point3d`, not the course companion's `System.Numerics.Vector3`.
That is an intentional double-precision, host-neutral boundary.

## Quick example

```csharp
using GeometryKit.Analysis;
using GeometryKit.Core;
using GeometryKit.Fabrication;
using GeometryKit.Mesh;

var mesh = new TriangleMesh(
    [new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(0, 1, 0)],
    [new IndexedTriangle(0, 1, 2)]);

var health = MeshHealthAnalyzer.Analyse(mesh);
var index = new MeshSpatialIndex(mesh);
var projected = index.ClosestPoint(new Point3d(.2, .2, 1));
var curvature = SurfaceAnalysis.GaussianCurvature(mesh);
var section = MeshSectioner.Intersect(mesh,
    new Plane3d(new Point3d(0, 0, 0), new Point3d(0, 0, 1)));
```

## Running tests

```bash
dotnet test src/csharp/GeometryKit.Tests/GeometryKit.Tests.csproj
```

See [ADR](adr/) for architecture decisions and [mathematics](mathematics/) for the equations,
assumptions and known limitations behind the first algorithms.

## Next implementation increment

1. Add a `geometry3Sharp` adapter behind this API; do not expose `g3` types publicly.
2. Add an actual sparse-solver package and heat-method geodesics.
3. Stitch section segments into tolerance-aware ordered polylines.
4. Add constrained remeshing that consumes the generated sizing field.
