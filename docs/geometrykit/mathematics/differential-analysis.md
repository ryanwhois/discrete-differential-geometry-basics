# Discrete Differential Analysis

## Gaussian curvature by angle defect

For a vertex \(i\), sum the incident triangle angles \(\theta_{if}\). The integrated Gaussian
curvature is

\[
K_i = \begin{cases}
2\pi - \sum_f \theta_{if}, & i \text{ interior},\\
\pi - \sum_f \theta_{if}, & i \text{ boundary}.
\end{cases}
\]

The first density estimate uses the barycentric area

\[
A_i = \frac13 \sum_{f\ni i} A_f, \qquad k_i = K_i/A_i.
\]

For a closed triangulated surface, \(\sum_i K_i = 2\pi\chi\). For a topological disc the
boundary-aware formula includes its boundary turning contribution, so the same total applies.

## Mean-curvature normal

For edge \((i,j)\), sum half of the cotangents opposite that edge:

\[
w_{ij}=\frac12(\cot\alpha_{ij}+\cot\beta_{ij}),\qquad
\mathbf{Hn}_i=\frac{1}{2A_i}\sum_j w_{ij}(p_i-p_j).
\]

On a boundary, only the existing incident triangle contributes. The implementation reports the
vector and its magnitude; choosing a signed scalar requires a reliable orientation convention and
is intentionally left to the caller.

## Geodesic baseline

`EdgeGeodesics` applies Dijkstra's algorithm to mesh edges weighted by Euclidean edge length. It is
exact on the 1-skeleton but not an approximation of a smooth surface geodesic across triangle
interiors. It is included as a dependable baseline and explicitly not named `HeatMethod`.

The next DDG increment will add the heat method:

\[
(M - tL)u=M\delta,\qquad X=-\frac{\nabla u}{\|\nabla u\|},\qquad
L\phi=\nabla\cdot X,
\]

using a tested sparse solver, a selectable mass matrix and an explicit null-space constraint.
