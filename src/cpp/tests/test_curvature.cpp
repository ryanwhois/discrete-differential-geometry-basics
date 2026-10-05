#include <catch2/catch_test_macros.hpp>
#include <catch2/matchers/catch_matchers_floating_point.hpp>
#include "../core/Mesh.h"
#include "../algorithms/DiscreteGaussianCurvature.h"
#include <Eigen/Dense>
#include <cmath>

TEST_CASE("Gauss-Bonnet includes boundary angle defects", "[curvature][boundary]") {
    Eigen::MatrixXd V(4, 3);
    V << 0, 0, 0,
         1, 0, 0,
         1, 1, 0,
         0, 1, 0;
    Eigen::MatrixXi F(2, 3);
    F << 0, 1, 2,
         0, 2, 3;
    Mesh mesh;
    mesh.build(V, F);

    REQUIRE_THAT(
        DiscreteGaussianCurvature::totalCurvature(mesh),
        Catch::Matchers::WithinAbs(2.0 * std::acos(-1.0), 1e-10));
    REQUIRE(DiscreteGaussianCurvature::gaussBonnetError(mesh) < 1e-10);
}
