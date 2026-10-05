#include "DiscreteGaussianCurvature.h"
#include <cmath>
#include <algorithm>

Eigen::VectorXd DiscreteGaussianCurvature::compute(const Mesh& mesh) {
    constexpr double pi = 3.14159265358979323846;
    Eigen::VectorXd K = Eigen::VectorXd::Zero(mesh.numVertices());
    
    for (const auto& v : mesh.vertices) {
        double angleSum = 0.0;
        for (HalfEdge* he : v->outgoingHalfEdges()) {
            if (he->face) {
                const Eigen::Vector3d e1 = he->target()->position - v->position;
                const Eigen::Vector3d e2 = he->next->target()->position - v->position;
                const double denom = e1.norm() * e2.norm();
                if (denom <= 1e-14) continue;
                double angle = std::acos(std::clamp(e1.dot(e2) / denom, -1.0, 1.0));
                angleSum += angle;
            }
        }
        K(v->index) = (v->isBoundary() ? pi : 2.0 * pi) - angleSum;
    }
    
    return K;
}

double DiscreteGaussianCurvature::gaussBonnetError(const Mesh& mesh) {
    constexpr double pi = 3.14159265358979323846;
    return std::abs(totalCurvature(mesh) - 2.0 * pi * mesh.eulerCharacteristic());
}

double DiscreteGaussianCurvature::totalCurvature(const Mesh& mesh) {
    return compute(mesh).sum();
}
