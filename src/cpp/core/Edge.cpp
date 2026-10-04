#include "Edge.h"
#include "HalfEdge.h"
#include "Vertex.h"
#include "Face.h"
#include <cmath>

Vertex* Edge::v0() const {
    return halfedge->vertex;
}

Vertex* Edge::v1() const {
    return halfedge->twin->vertex;
}

bool Edge::isBoundary() const {
    return !halfedge->face || !halfedge->twin->face;
}

double Edge::length() const {
    Eigen::Vector3d v = halfedge->vector();
    return v.norm();
}

double Edge::cotan() const {
    double cotSum = 0.0;
    for (HalfEdge* he : {halfedge, halfedge->twin}) {
        if (!he || !he->face || !he->next || !he->next->next) continue;
        const Eigen::Vector3d p0 = he->source()->position;
        const Eigen::Vector3d p1 = he->target()->position;
        const Eigen::Vector3d opposite = he->next->target()->position;
        const Eigen::Vector3d a = p0 - opposite;
        const Eigen::Vector3d b = p1 - opposite;
        const double cross = a.cross(b).norm();
        if (cross > 1e-14) cotSum += a.dot(b) / cross;
    }
    return cotSum / 2.0;
}

Eigen::Vector3d Edge::midpoint() const {
    return 0.5 * (halfedge->vertex->position + halfedge->twin->vertex->position);
}
