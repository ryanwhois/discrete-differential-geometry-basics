# GeometryKit — Project Summary

GeometryKit started as a companion kit for Keenan Crane's *Discrete Differential Geometry* course.
It is becoming a focused C# library for derived triangle-mesh workflows: health assessment, spatial
queries, surface analysis and fabrication support. The original C++, C# and WASM course material
remains as an educational and experimental reference layer.

## Technical reality

- `src/csharp/GeometryKit` is the primary reusable library, with double-precision public geometry
  types and an immutable triangle-mesh boundary.
- It currently supports mesh health reports, closest-point and ray queries, Gaussian curvature,
  graph geodesics, planar section segments and curvature-driven sizing fields.
- The original DDG implementations in C++ and C# provide useful mathematical reference material,
  but several algorithms remain partial or experimental.
- CI validates C++, C#, Python and WASM builds; GeometryKit has its own workflow tests.

## Boundaries

GeometryKit analyses derived meshes. A CAD host remains authoritative for B-Reps, feature history,
exact edits and exchange formats. The library does not promise lossless mesh-to-B-Rep conversion.

## Maturity labels

| Label | Meaning |
|---|---|
| Foundation | A small, coherent public API with workflow tests; further robustness and breadth are planned. |
| Partial | Implemented with known correctness, robustness or test gaps. |
| Experimental | Early scaffold or incomplete numerical/topology handling. |
| Planned | Intended but not implemented. |

## Build and test

### GeometryKit

```bash
dotnet test src/csharp/GeometryKit.Tests/GeometryKit.Tests.csproj
```

### Course companion and reference tools

The C++, original C# companion and WASM build instructions remain in their source directories and
in the documentation index. They are maintained as reference material while GeometryKit becomes
the primary C# API.

## Development direction

1. Stabilise the mesh-health and spatial-query contracts.
2. Introduce established mesh-library adapters behind GeometryKit types.
3. Add sparse-solver-backed geodesics and tolerance-aware section stitching.
4. Add constrained remeshing that consumes the sizing field.

See [the GeometryKit overview](docs/geometrykit/README.md),
[architecture decisions](docs/geometrykit/adr/), and the
[implementation audit](docs/status/IMPLEMENTATION_AUDIT.md).
