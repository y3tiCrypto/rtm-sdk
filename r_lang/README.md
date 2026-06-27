# Raptoreum (RTM) SDK - R

A lightweight R client wrapper for the Raptoreum Core JSON-RPC interface, built on `httr` and `jsonlite`.

---

## Installation

Within R, install dependencies:
```R
install.packages(c("httr", "jsonlite"))
```

---

## Quick Start

```R
source("raptoreum.R")

client <- RaptoreumClient("127.0.0.1", 8766, "user", "pass")
balance <- client$getbalance()
print(balance)
```
