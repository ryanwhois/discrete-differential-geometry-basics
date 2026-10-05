# DDG Course Companion

Companion implementations and notes for Keenan Crane's *Discrete Differential Geometry* course.

> Current state: educational and improving, not production-complete.

[![C++ Build](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/cpp-build.yml/badge.svg)](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/cpp-build.yml)
[![C# Build](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/csharp-build.yml/badge.svg)](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/csharp-build.yml)
[![WASM Build](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/wasm-build.yml/badge.svg)](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/wasm-build.yml)
[![Python Checks](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/python-checks.yml/badge.svg)](https://github.com/ryanwhois/discrete-differential-geometry-basics/actions/workflows/python-checks.yml)

## Status Snapshot

| Area | Status | Notes |
|---|---|---|
| C++ core mesh + DDG algorithms | **Partial** | Boundary-safe topology, symmetric cotan operators, constrained solves; Hodge bases remain experimental |
| C# core mesh + DDG algorithms | **Partial** | Boundary-safe parity for topology, curvature, Laplacian, Heat divergence, and boundary mapping |
| Python tooling | **Supported utility** | Dependency-free OBJ topology inspector plus optional visualizer |
| Web companion | **Interactive reference** | Responsive workbench with a JS reference engine and optional WASM acceleration |
| Test coverage | **Partial** | Closed/open topology and Gauss–Bonnet coverage; broader parity datasets remain future work |

See `docs/status/IMPLEMENTATION_AUDIT.md` for the detailed audit.

## Implemented Algorithms by Language

| Algorithm | C++ | C# | Maturity |
|---|:---:|:---:|---|
| Cotan Laplacian | ✅ | ✅ | Partial (C# currently relies on simplified edge cotan path) |
| Mean Curvature Flow | ✅ | ✅ | Partial |
| Discrete Gaussian Curvature | ✅ | ✅ | Partial |
| Conformal Parameterization | ✅ | ✅ | Partial/Experimental |
| Heat Method | ✅ | ✅ | Partial/Experimental |
| Hodge Decomposition | ✅ | ✅ | Experimental |

Legend: **Complete** = solidly validated and robust, **Partial** = implemented with known gaps, **Experimental** = scaffold/initial implementation, **Planned** = not yet implemented.

## Quick Start

### C++

```bash
cd src/cpp
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j4
cd build
ctest --output-on-failure
```

### C#

```bash
cd src/csharp
dotnet restore
dotnet build --configuration Release
cd Tests
dotnet test --configuration Release
```

### WebAssembly

WASM bindings and demos live under `src/wasm/`. Open `web/index.html` for
the interactive workbench; it falls back honestly to the JavaScript reference
engine when compiled WASM artifacts are absent.

## Repository Layout

```text
src/cpp/        C++ core mesh, algorithms, tests, examples
src/csharp/     C# core mesh, algorithms, tests, CLI examples
src/wasm/       Emscripten bindings and wasm build config
docs/           Chapters, formulas, assignments, tutorials, status docs
examples/       Example usage notes and Python helper script
web/            Interactive scientific workbench and legacy WASM pages
```

## Documentation

- `docs/README.md`
- `docs/algorithms/README.md`
- `docs/formulas/index.md`
- `docs/assignments/README.md`
- `docs/tutorials/README.md`
- `docs/status/IMPLEMENTATION_AUDIT.md`
- `docs/status/NEXT_RELEASE_PLAN.md`

## Roadmap (Realistic)

### 1.2.0 release candidate
- Explicit boundary halfedges and topology validation in C++ and C#
- Symmetric cotan assembly and constrained Poisson infrastructure
- Boundary-aware curvature, Heat divergence, and harmonic disk mapping
- Dependency-free Python inspection and an interactive web workbench

### Later
- True LSCM assembly, harmonic basis generation, and ARAP deformation
- Cross-language parity datasets and larger numerical regression suites

## Contributing

See `CONTRIBUTING.md` for build/test workflow, contribution expectations, and documentation standards.

## License

MIT (`LICENSE`).
