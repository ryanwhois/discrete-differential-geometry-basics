#include <iostream>
#include <Eigen/Dense>
#include "core/Mesh.h"
#include "algorithms/CotanLaplacian.h"
#include "algorithms/MeanCurvatureFlow.h"
#include "algorithms/DiscreteGaussianCurvature.h"

int main(int argc, char** argv) {
    constexpr double pi = 3.14159265358979323846;
    std::cout << "DDG Course Companion - Demo Program\n";
    std::cout << "===================================\n\n";
    
    // Create a simple cube mesh for demonstration
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
    F << 0, 1, 2,  0, 2, 3,  // Front
         4, 7, 6,  4, 6, 5,  // Back
         0, 4, 5,  0, 5, 1,  // Bottom
         2, 6, 7,  2, 7, 3,  // Top
         0, 3, 7,  0, 7, 4,  // Left
         1, 5, 6,  1, 6, 2;  // Right
    
    // Build mesh
    Mesh mesh;
    mesh.build(V, F);
    
    std::cout << "Mesh Statistics:\n";
    std::cout << "  Vertices: " << mesh.numVertices() << "\n";
    std::cout << "  Edges: " << mesh.numEdges() << "\n";
    std::cout << "  Faces: " << mesh.numFaces() << "\n";
    std::cout << "  Euler characteristic: " << mesh.eulerCharacteristic() << "\n\n";
    
    // Compute Gaussian curvature
    std::cout << "Computing Gaussian curvature...\n";
    Eigen::VectorXd K = DiscreteGaussianCurvature::compute(mesh);
    double totalK = DiscreteGaussianCurvature::totalCurvature(mesh);
    double expectedK = 2 * pi * mesh.eulerCharacteristic();
    
    std::cout << "  Total curvature: " << totalK << "\n";
    std::cout << "  Expected (2πχ): " << expectedK << "\n";
    std::cout << "  Error: " << std::abs(totalK - expectedK) << "\n\n";
    
    // Build Laplacian
    std::cout << "Building cotan Laplacian...\n";
    auto L = CotanLaplacian::build(mesh);
    auto M = CotanLaplacian::buildMassMatrix(mesh);
    
    std::cout << "  Laplacian size: " << L.rows() << " x " << L.cols() << "\n";
    std::cout << "  Non-zeros: " << L.nonZeros() << "\n\n";
    
    // Mean curvature flow
    std::cout << "Running mean curvature flow...\n";
    double timestep = 0.001;
    int numSteps = 10;
    
    for (int i = 0; i < numSteps; i++) {
        MeanCurvatureFlow::step(mesh, timestep);
        if (i % 2 == 0) {
            std::cout << "  Step " << i << " complete\n";
        }
    }
    
    std::cout << "\nDemo complete!\n";
    std::cout << "\nFor more examples, see examples/ directory\n";
    
    return 0;
}
