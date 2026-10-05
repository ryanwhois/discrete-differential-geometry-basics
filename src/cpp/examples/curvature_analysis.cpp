// curvature_analysis.cpp
// Example: Computing and analyzing discrete Gaussian curvature
// Added by Graph Technologies, 2025

#include <iostream>
#include <Eigen/Dense>
#include "../core/Mesh.h"
#include "../algorithms/DiscreteGaussianCurvature.h"

int main() {
    constexpr double pi = 3.14159265358979323846;
    std::cout << "DDG Example: Curvature Analysis\n";
    std::cout << "================================\n\n";
    
    // Create cube mesh
    Eigen::MatrixXd V(8, 3);
    V << -1, -1, -1,
          1, -1, -1,
          1,  1, -1,
         -1,  1, -1,
         -1, -1,  1,
          1, -1,  1,
          1,  1,  1,
         -1,  1,  1;
    
    Eigen::MatrixXi F(12, 3);
    F << 0, 1, 2,  0, 2, 3,
         4, 7, 6,  4, 6, 5,
         0, 4, 5,  0, 5, 1,
         2, 6, 7,  2, 7, 3,
         0, 3, 7,  0, 7, 4,
         1, 5, 6,  1, 6, 2;
    
    Mesh mesh;
    mesh.build(V, F);
    
    std::cout << "Mesh: Cube" << std::endl;
    std::cout << "  V = " << mesh.numVertices() << ", ";
    std::cout << "E = " << mesh.numEdges() << ", ";
    std::cout << "F = " << mesh.numFaces() << std::endl;
    std::cout << "  χ = " << mesh.eulerCharacteristic() << std::endl << std::endl;
    
    // Compute Gaussian curvature
    Eigen::VectorXd K = DiscreteGaussianCurvature::compute(mesh);
    
    std::cout << "Gaussian Curvature per vertex:\n";
    for (int i = 0; i < K.size(); i++) {
        std::cout << "  v[" << i << "]: K = " << K(i) 
                  << " (" << (K(i) * 180.0 / pi) << "°)" << std::endl;
    }
    
    // Verify Gauss-Bonnet theorem
    double totalK = DiscreteGaussianCurvature::totalCurvature(mesh);
    double expectedK = 2.0 * pi * mesh.eulerCharacteristic();
    
    std::cout << "\nGauss-Bonnet Verification:\n";
    std::cout << "  Total curvature: " << totalK << std::endl;
    std::cout << "  Expected (2πχ):   " << expectedK << std::endl;
    std::cout << "  Error: " << std::abs(totalK - expectedK) << std::endl;
    
    if (std::abs(totalK - expectedK) < 0.01) {
        std::cout << "  ✓ Gauss-Bonnet theorem verified!" << std::endl;
    } else {
        std::cout << "  ✗ Gauss-Bonnet error exceeds tolerance" << std::endl;
    }
    
    return 0;
}
