# Test suite for DDG Course Companion
# Added by Graph Technologies, 2025

include(FetchContent)

# Fetch Catch2
FetchContent_Declare(
    Catch2
    GIT_REPOSITORY https://github.com/catchorg/Catch2.git
    GIT_TAG v3.4.0
)
FetchContent_MakeAvailable(Catch2)

# Test executable
add_executable(ddg-tests
    test_mesh.cpp
    test_laplacian.cpp
    test_curvature.cpp
)

target_link_libraries(ddg-tests
    PRIVATE
    ddg-core
    ddg-algorithms
    Catch2::Catch2WithMain
)
ddg_enable_warnings(ddg-tests)

# Register tests with CTest
include(CTest)
include(Catch)
catch_discover_tests(ddg-tests)
