# Detailed Implementation Plan

## Objective

Turn the repository from a collection of educational implementations into a
coherent, testable DDG course companion without overstating numerical maturity.

## Completed dependency order

### 1. Topology foundation

- Validate triangular input, indices, duplicate directed edges, manifold edge
  incidence, and degenerate geometry.
- Construct explicit boundary twins and boundary loops.
- Make one-ring traversal safe and consistent in C++ and C#.

### 2. Discrete operators

- Correct opposite-angle cotangent evaluation.
- Assemble symmetric Laplacians once per undirected edge.
- Add reusable Dirichlet-constrained solves.
- Include boundary angle defects in Gaussian curvature.

### 3. Algorithms

- Route Heat Method distance recovery through an anchored Poisson solve.
- Implement the C# weak-divergence stage.
- Replace unordered boundary selection with ordered, chord-length circles.
- Fail explicitly for the still-deferred true LSCM implementation.

### 4. Verification

- Add closed and open topology tests.
- Add boundary Gauss–Bonnet tests.
- Add a dependency-free OBJ inspector and four Python tests.
- Align C++, C#, Python, and WASM CI workflows with the source layout.

### 5. Educational web surface

- Provide a responsive rail–canvas–rail workbench.
- Expose mesh fixtures, algorithm controls, topology metrics, theory, and
  diagnostics.
- Use the JS reference engine when WASM output is absent and label the active
  engine honestly.
- Add root and `/workbench` routing for Vercel.

## Deferred work

True LSCM, harmonic basis generation, ARAP, sparse production-scale constraint
elimination, and formal parity datasets remain separate milestones.
