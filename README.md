# GeometryKit

GeometryKit began as a companion kit for Keenan Crane's *Discrete Differential Geometry* course.
It is now growing to serve a specific engineering need: a host-neutral, double-precision C# toolkit
for understanding and preparing derived triangle meshes before they move back into a CAD, analysis,
or fabrication workflow.

> Current state: an early, usable foundation. GeometryKit supplies analysis and support geometry; it is not a B-Rep CAD kernel or an authoritative document model.

[![C++ Build](https://github.com/ryanwhois/GeometryKit/actions/workflows/cpp-build.yml/badge.svg)](https://github.com/ryanwhois/GeometryKit/actions/workflows/cpp-build.yml)
[![C# Build](https://github.com/ryanwhois/GeometryKit/actions/workflows/csharp-build.yml/badge.svg)](https://github.com/ryanwhois/GeometryKit/actions/workflows/csharp-build.yml)
[![WASM Build](https://github.com/ryanwhois/GeometryKit/actions/workflows/wasm-build.yml/badge.svg)](https://github.com/ryanwhois/GeometryKit/actions/workflows/wasm-build.yml)
[![Python Checks](https://github.com/ryanwhois/GeometryKit/actions/workflows/python-checks.yml/badge.svg)](https://github.com/ryanwhois/GeometryKit/actions/workflows/python-checks.yml)

## The need it serves

GeometryKit turns a triangle mesh into reliable derived information without taking ownership of the
original CAD model. Its initial C# foundation provides:

- mesh health reports for invalid indices, degeneracy, boundary and non-manifold conditions;
- spatial queries for closest-point projection and ray intersections;
- discrete surface fields, including Gaussian-curvature and graph-geodesic calculations;
- fabrication-support outputs: plane-section segments and curvature-driven target edge lengths.

This makes it suitable as a portable geometry layer between a host application and mesh-based
operations. A CAD host remains responsible for B-Reps, feature history, exact editing and exchange
formats.

## Repository status

| Area | Role | Status |
|---|---|---|
| `src/csharp/GeometryKit` | Primary reusable .NET library | **Foundation** |
| `src/csharp/GeometryKit.Tests` | Executable workflow specifications | **Foundation** |
| C# DDG companion | Course implementations and examples | **Reference / partial** |
| C++ DDG companion | Course implementations, tests and examples | **Reference / partial** |
| Python and web workbench | Inspection and interactive learning tools | **Supported reference** |

The original DDG companion remains in the repository because it explains the mathematical lineage
and provides experiments that inform GeometryKit. It is not the public API of the toolkit.

## Quick start

```bash
dotnet test src/csharp/GeometryKit.Tests/GeometryKit.Tests.csproj
```

A minimal C# workflow:

```csharp
using GeometryKit.Core;
using GeometryKit.Mesh;

var mesh = new TriangleMesh(vertices, triangles);
var health = MeshHealthAnalyzer.Analyse(mesh);
var index = new MeshSpatialIndex(mesh);
var point = index.ClosestPoint(queryPoint);
```

See the [GeometryKit overview](docs/geometrykit/README.md) for the implemented workflows,
mathematical assumptions and architecture decisions.

## Repository layout

```text
src/csharp/GeometryKit/        Primary C# geometry toolkit
src/csharp/GeometryKit.Tests/  Tests for public GeometryKit workflows
src/csharp/                    Original DDG course companion (reference implementation)
src/cpp/                       Original C++ DDG course companion
src/wasm/                      WASM bindings and build configuration
docs/geometrykit/              Toolkit architecture and mathematics
docs/                          DDG notes, formulas, tutorials and project status
examples/                      Example usage notes and Python helper script
web/                           Interactive scientific workbench and legacy WASM pages
```

## Documentation

- [GeometryKit overview](docs/geometrykit/README.md)
- [GeometryKit architecture decisions](docs/geometrykit/adr/)
- [GeometryKit mathematics](docs/geometrykit/mathematics/)
- [DDG documentation index](docs/README.md)
- [Implementation audit](docs/status/IMPLEMENTATION_AUDIT.md)

## Development direction

1. Stabilise the current public mesh and spatial-query API.
2. Add adapters for established mesh tooling without exposing their types publicly.
3. Add sparse-solver-backed heat geodesics and tolerance-aware section stitching.
4. Add constrained remeshing driven by the generated sizing fields.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build/test workflow, contribution expectations and
documentation standards.

## License

MIT ([LICENSE](LICENSE)).
