# Raptoreum (RTM) SDK - C++

A standard C++ client wrapper for the Raptoreum Core JSON-RPC interface, built on `libcurl`.

---

## Prerequisites
*   CMake (>= 3.10)
*   libcurl library and headers (e.g. `libcurl4-openssl-dev` on Debian/Ubuntu)

---

## Build Instructions

```bash
cd cpp
mkdir build
cd build
cmake ..
cmake --build .
```

---

## Quick Start

```cpp
#include "raptoreum_client.hpp"
#include <iostream>

int main() {
    raptoreum::RaptoreumClient client("127.0.0.1", 8766, "your_user", "your_pass");

    try {
        std::string blockCountJson = client.getblockcount();
        std::cout << "Blocks JSON: " << blockCountJson << std::endl;
    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << std::endl;
    }
    return 0;
}
```
