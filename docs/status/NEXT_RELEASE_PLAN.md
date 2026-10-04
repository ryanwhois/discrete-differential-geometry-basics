# Next Release Plan

## Release decision

The current branch is a **v1.2.0 candidate**. It crosses the minor-release
threshold by adding explicit boundary topology, reusable constrained solves,
implemented Heat divergence, and a new interactive web application.

## Release gates

1. All C++, C#, Python, and WASM workflows pass on the pull request.
2. Closed tetrahedron and open disk invariants pass in both language cores.
3. Cotan matrices remain symmetric with near-zero row sums.
4. Open-disk curvature has a near-zero Gauss–Bonnet residual.
5. The deployed workbench loads at both `/` and `/web/index.html`.
6. No documentation describes deferred LSCM, harmonic bases, or ARAP as complete.

## Post-1.2 priorities

1. Establish versioned cross-language mesh fixtures and golden outputs.
2. Replace dense constraint elimination with sparse reduced systems.
3. Implement and validate true LSCM.
4. Implement tree–cotree generators and harmonic bases.
5. Add ARAP only after solver and parity infrastructure are stable.

These are intentionally separate from the 1.2.0 release candidate.
