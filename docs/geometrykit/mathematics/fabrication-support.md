# Fabrication-Support Geometry

## Triangle-plane sections

For plane point \(o\), unit normal \(n\), and segment endpoints \(p_0,p_1\), compute signed
distances \(d_0=n\cdot(p_0-o)\) and \(d_1=n\cdot(p_1-o)\). If their signs differ, the segment
crosses the plane at

\[
p(t)=p_0+t(p_1-p_0),\qquad t=\frac{d_0}{d_0-d_1}.
\]

Each triangle emits zero or one independent segment after within-triangle point deduplication.
Coplanar triangles are counted and not emitted: emitting all coplanar edges would duplicate and
topologically confuse ordinary section contours. Joining segments into ordered polylines is a
separate next step because it requires a documented endpoint clustering tolerance and branching
policy.

## Curvature-driven sizing

The initial sizing field is a recommendation for a future remesher, not remeshing itself. It uses
a small-arc sagitta relation \(e\approx \kappa h^2/8\). With curvature scale
\(\kappa\approx\sqrt{|k_i|}\), desired chord error \(e\), and hard bounds, it produces

\[
h_i=\operatorname{clamp}\left(\sqrt{\frac{8e}{\kappa}}, h_{min}, h_{max}\right).
\]

This is a heuristic. It is suitable for concentrating a mesh in high-curvature regions, but does
not guarantee manufacturing error, feature preservation, collision clearance or developability.
