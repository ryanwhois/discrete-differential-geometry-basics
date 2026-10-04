# Changelog

## [Unreleased]

## [1.2.0] - 2026-10-05

### Added
- Explicit boundary halfedges, closed boundary-loop traversal, and topology validation in both mesh cores.
- Constrained Poisson helpers, boundary-aware Gaussian curvature, and open-mesh Gauss–Bonnet tests.
- Implemented C# Heat Method weak-divergence stage and boundary-circle constrained solve.
- Dependency-free Python OBJ inspector with parser, topology, and degeneracy tests.
- Responsive interactive DDG workbench with JavaScript fallback and optional WASM engine.
- Root Vercel routing for the workbench.

### Changed
- Cotangent Laplacians are assembled once per undirected edge and are symmetric by construction.
- Boundary-circle parameterization follows boundary order and uses chord-length spacing.
- Project metadata now reports version 1.2.0.

### Deferred
- True LSCM, harmonic basis generation, ARAP, and formal cross-language parity datasets.

### Changed
- Replaced overstated top-level docs with explicit maturity labels.
- Updated C++/C#/WASM workflow action versions and corrected C# test execution path.
- Improved C++ CMake warning defaults and fixed broken C++ test target list.

### Added
- `docs/status/IMPLEMENTATION_AUDIT.md`
- `docs/status/NEXT_RELEASE_PLAN.md`
- `docs/tutorials/README.md`
- C# invalid topology input test in `MeshTests`

### Fixed
- C# mesh build now validates triangle input and invalid indices.
- C# vertex traversal methods now guard against null twin/next paths.
- C# cotan Laplacian no longer uses fixed placeholder weight.
- C# edge cotangent calculation now guards against near-zero denominators.
- Oriented tetrahedron fixtures updated in C++ and C# tests for consistent closed-mesh topology.
