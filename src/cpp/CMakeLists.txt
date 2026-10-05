# CMakeLists.txt
# DDG Course Companion - C++ Build Configuration
# Added by Graph Technologies, 2025

cmake_minimum_required(VERSION 3.15)
project(DDGCourseCompanion VERSION 1.2.0 LANGUAGES CXX)

# C++ Standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Build options
option(BUILD_TESTS "Build unit tests" ON)
option(BUILD_EXAMPLES "Build example programs" OFF)
option(DDG_ENABLE_WARNINGS "Enable compiler warnings" ON)

function(ddg_enable_warnings target_name)
    if(NOT DDG_ENABLE_WARNINGS)
        return()
    endif()

    if(MSVC)
        target_compile_options(${target_name} PRIVATE /W4 /permissive-)
    else()
        target_compile_options(${target_name} PRIVATE -Wall -Wextra -Wpedantic)
    endif()
endfunction()

# Find Eigen3
find_package(Eigen3 3.3 QUIET NO_MODULE)
if(NOT Eigen3_FOUND)
    include(FetchContent)

    # Eigen enables its large upstream test suite by default.  Keep those tests
    # out of this project's CTest tree when Eigen is used as a fallback.
    if(DEFINED BUILD_TESTING)
        set(DDG_SAVED_BUILD_TESTING "${BUILD_TESTING}")
        set(DDG_BUILD_TESTING_WAS_DEFINED ON)
    endif()
    set(BUILD_TESTING OFF CACHE BOOL "Build upstream dependency tests" FORCE)

    FetchContent_Declare(
        eigen
        URL https://gitlab.com/libeigen/eigen/-/archive/3.4.0/eigen-3.4.0.tar.gz
        URL_HASH SHA256=8586084f71f9bde545ee7fa6d00288b264a2b7ac3607b974e54d13e7162c1c72
    )
    FetchContent_MakeAvailable(eigen)

    if(DDG_BUILD_TESTING_WAS_DEFINED)
        set(BUILD_TESTING "${DDG_SAVED_BUILD_TESTING}" CACHE BOOL "Build tests" FORCE)
    else()
        unset(BUILD_TESTING CACHE)
    endif()
endif()

# Core library
file(GLOB_RECURSE CORE_SOURCES "core/*.cpp")
file(GLOB_RECURSE CORE_HEADERS "core/*.h")

add_library(ddg-core STATIC ${CORE_SOURCES})
target_include_directories(ddg-core PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(ddg-core PUBLIC Eigen3::Eigen)
ddg_enable_warnings(ddg-core)

# Algorithms library
file(GLOB_RECURSE ALGO_SOURCES "algorithms/*.cpp")
file(GLOB_RECURSE ALGO_HEADERS "algorithms/*.h")

add_library(ddg-algorithms STATIC ${ALGO_SOURCES})
target_include_directories(ddg-algorithms PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(ddg-algorithms PUBLIC ddg-core Eigen3::Eigen)
ddg_enable_warnings(ddg-algorithms)

# Utilities library
file(GLOB_RECURSE UTIL_SOURCES "utils/*.cpp")
file(GLOB_RECURSE UTIL_HEADERS "utils/*.h")

add_library(ddg-utils STATIC ${UTIL_SOURCES})
target_include_directories(ddg-utils PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(ddg-utils PUBLIC ddg-core Eigen3::Eigen)
ddg_enable_warnings(ddg-utils)

# Main executable
add_executable(ddg-demo main.cpp)
target_link_libraries(ddg-demo PRIVATE ddg-core ddg-algorithms ddg-utils)
ddg_enable_warnings(ddg-demo)

# Examples
if(BUILD_EXAMPLES)
    add_subdirectory(examples)
endif()

# Tests
if(BUILD_TESTS)
    enable_testing()
    add_subdirectory(tests)
endif()

# Installation
install(TARGETS ddg-core ddg-algorithms ddg-utils
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib)

install(DIRECTORY core/ algorithms/ utils/
        DESTINATION include/ddg
        FILES_MATCHING PATTERN "*.h")

# Print configuration summary
message(STATUS "")
message(STATUS "DDG Course Companion Configuration:")
message(STATUS "  Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "  C++ standard: ${CMAKE_CXX_STANDARD}")
message(STATUS "  Build tests: ${BUILD_TESTS}")
message(STATUS "  Build examples: ${BUILD_EXAMPLES}")
message(STATUS "  Eigen3 version: ${Eigen3_VERSION}")
message(STATUS "")
