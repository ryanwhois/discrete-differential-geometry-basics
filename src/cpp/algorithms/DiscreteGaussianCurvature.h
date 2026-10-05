#pragma once
#include "../core/Mesh.h"
#include <Eigen/Dense>

class DiscreteGaussianCurvature {
public:
    // Compute Gaussian curvature at each vertex (angle defect)
    static Eigen::VectorXd compute(const Mesh& mesh);
    
    // Compute total Gaussian curvature (should equal 2πχ)
    static double totalCurvature(const Mesh& mesh);
    static double gaussBonnetError(const Mesh& mesh);
};
