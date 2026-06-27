cmake_minimum_required(VERSION 3.10)
project(raptoreum_client VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(CURL REQUIRED)
find_package(OpenSSL REQUIRED)

add_library(raptoreum_client
    src/raptoreum_client.cpp
)

target_include_directories(raptoreum_client
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_link_libraries(raptoreum_client
    PRIVATE
        CURL::libcurl
        OpenSSL::Crypto
)

add_executable(raptoreum_example example.cpp)
target_link_libraries(raptoreum_example PRIVATE raptoreum_client)
