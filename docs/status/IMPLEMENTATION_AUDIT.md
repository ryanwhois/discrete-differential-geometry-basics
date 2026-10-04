# Implementation Audit — v1.2.0 Candidate

## Current capability

| Area | C++ | C# | Maturity |
|---|:---:|:---:|---|
| Explicit manifold boundary topology | Yes | Yes | Validated on closed and disk fixtures |
| Cotan Laplacian and barycentric mass | Yes | Yes | Symmetric assembly; broader conditioning tests needed |
| Constrained Poisson solve | Yes | Yes | Dense constraint elimination; suitable for educational-scale meshes |
| Gaussian curvature | Yes | Yes | Interior and boundary angle defects; Gauss–Bonnet tests |
| Mean curvature flow | Yes | Yes | Implemented; solver diagnostics and boundary policies need expansion |
| Boundary-circle parameterization | Yes | Yes | Ordered, chord-length boundary with constrained harmonic solve |
| Heat Method | Yes | Yes | Heat, normalized face gradient, weak divergence, anchored Poisson |
| Hodge decomposition | Partial | Partial | Decomposition exists; harmonic bases and full DEC validation deferred |
| Interactive web workbench | JS + optional WASM | — | Reference/educational surface |

## Correctness changes in this release

- Every manifold edge owns two halfedges; open edges receive an explicit
  boundary twin and boundary halfedges form closed loops.
- Duplicate directed edges, non-manifold edges, invalid indices, degenerate
  faces, and incomplete pointer relations are rejected or reported.
- Cotangent weights are evaluated at the angle opposite each edge and inserted
  symmetrically.
- Boundary Gaussian curvature uses the \(\pi-\sum\theta\) defect, so disk
  meshes satisfy Gauss–Bonnet.
- Singular Poisson systems are anchored through explicit Dirichlet constraints.
- The C# Heat Method no longer returns the zero-divergence placeholder result.

## Deliberately deferred

- True least-squares conformal maps (the C++ entry point fails explicitly
  instead of presenting a harmonic map as LSCM).
- Harmonic one-form basis construction and tree–cotree generators.
- ARAP deformation.
- Production-scale sparse constraint elimination, preconditioners, and
  cross-language golden datasets.

## Verification

- Python: four dependency-free parser/topology tests.
- JavaScript: syntax and deployed interaction checks.
- Native compilation: required through the C++, C#, and WASM GitHub Actions
  matrices because those toolchains are not available in the recovery runtime.
