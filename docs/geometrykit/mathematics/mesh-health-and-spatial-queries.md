# Mesh Health and Spatial Queries

## Validity model

For every indexed triangle \((i,j,k)\), the first precondition is that the three indices are in
range and distinct. Its unsigned area is

\[
A_f = \frac12 \left\|(p_j-p_i) \times (p_k-p_i)\right\|.
\]

A triangle is flagged degenerate when its doubled area falls below a tolerance scaled by its local
edge length. This is intentionally a diagnostic rather than an automatic deletion: deleting faces
can change boundaries, connected components and material assignment.

Each undirected edge is counted. One incident face is a boundary edge; two is manifold; more than
two is non-manifold. The initial spatial and differential operators reject degenerate/non-manifold
input because both closest-point tie-breaking and cotangent weights become ambiguous or unstable.

## BVH queries

The spatial index recursively partitions triangle centroids and stores an axis-aligned bounding box
at each node. A closest-point search visits nodes in increasing lower-bound distance from the
query point and discards a node once its AABB lower bound exceeds the current best triangle result.

Ray tests use the slab intersection test for BVH pruning and Möller–Trumbore for individual
triangles. A hit includes triangle index, barycentric coordinates and parameter \(t\), allowing a
host to interpolate its own attributes without GeometryKit owning them.

## Limitations

- Current BVH splitting is median-centroid, not surface-area heuristic optimised.
- Open meshes are valid for projection and ray casting; "inside" classification is deliberately
  absent until closed/oriented semantics and winding-number policy are explicit.
