cmake_minimum_required(VERSION 3.22.1)
project(knossos_vulkan_driver LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}")
set(BUILD_SHARED_LIBS OFF)
add_subdirectory("${ADRENOTOOLS_DIR}" adrenotools)

add_library(fsovulkan SHARED fsovulkan.cpp)
target_compile_definitions(fsovulkan PRIVATE VK_USE_PLATFORM_ANDROID_KHR)
target_link_libraries(fsovulkan PRIVATE adrenotools dl log)
add_dependencies(fsovulkan main_hook hook_impl)
