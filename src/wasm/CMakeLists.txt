# CMakeLists.txt
# WebAssembly Build Configuration for DDG Course Companion
# Added by Graph Technologies, 2025
# Description: Compile C++ DDG algorithms to WebAssembly using Emscripten

cmake_minimum_required(VERSION 3.15)
project(DDGWebAssembly VERSION 1.2.0)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Emscripten-specific settings
if(EMSCRIPTEN)
    message(STATUS "Configuring for WebAssembly build with Emscripten")
    
    # Emscripten compile flags
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s WASM=1")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s ALLOW_MEMORY_GROWTH=1")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s MODULARIZE=1")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s EXPORT_NAME='DDGModule'")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s EXPORTED_RUNTIME_METHODS=['ccall','cwrap']")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -s ENVIRONMENT='web,node'")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} --bind")
    
    # Optimization flags
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -O3")
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -s ASSERTIONS=0")
    
    # Debug flags
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -g")
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -s ASSERTIONS=1")
    set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -s SAFE_HEAP=1")
else()
    message(FATAL_ERROR "This CMake file is for Emscripten only. Use emcmake cmake ..")
endif()

# Include Eigen (header-only, no special config needed)
find_package(Eigen3 3.3 REQUIRED NO_MODULE)

# Source files
set(CORE_SOURCES
    ../cpp/core/Vertex.cpp
    ../cpp/core/Edge.cpp
    ../cpp/core/Face.cpp
    ../cpp/core/HalfEdge.cpp
    ../cpp/core/Mesh.cpp
)

set(ALGORITHM_SOURCES
    ../cpp/algorithms/CotanLaplacian.cpp
    ../cpp/algorithms/MeanCurvatureFlow.cpp
    ../cpp/algorithms/DiscreteGaussianCurvature.cpp
    ../cpp/algorithms/ConformalParameterization.cpp
    ../cpp/algorithms/HeatMethod.cpp
    ../cpp/algorithms/HodgeDecomposition.cpp
)

set(WASM_BINDINGS
    bindings.cpp
)

# Build WASM module
add_executable(ddg-wasm
    ${CORE_SOURCES}
    ${ALGORITHM_SOURCES}
    ${WASM_BINDINGS}
)

target_include_directories(ddg-wasm PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/../cpp
    ${EIGEN3_INCLUDE_DIR}
)

target_link_libraries(ddg-wasm
    Eigen3::Eigen
)

# Set output name
set_target_properties(ddg-wasm PROPERTIES
    OUTPUT_NAME "ddg"
    SUFFIX ".js"
)

# Installation
install(FILES
    ${CMAKE_CURRENT_BINARY_DIR}/ddg.js
    ${CMAKE_CURRENT_BINARY_DIR}/ddg.wasm
    DESTINATION ${CMAKE_CURRENT_SOURCE_DIR}/../../web/wasm
)

message(STATUS "")
message(STATUS "WebAssembly Build Configuration:")
message(STATUS "  Output: ddg.js + ddg.wasm")
message(STATUS "  Install directory: web/wasm/")
message(STATUS "  Eigen3: ${EIGEN3_INCLUDE_DIR}")
message(STATUS "")
