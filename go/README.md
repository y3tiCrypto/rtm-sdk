# Raptoreum (RTM) SDK - Go

An idiomatic, zero-dependency Go client for the Raptoreum Core JSON-RPC interface.

---

## Installation

```bash
go get github.com/Raptor3um/rtm-sdk/go
```

---

## Quick Start

```go
package main

import (
	"fmt"
	"github.com/Raptor3um/rtm-sdk/go"
)

func main() {
	client := rtm.NewClient("127.0.0.1", 8766, "your_user", "your_pass", false)

	blocks, err := client.GetBlockCount()
	if err != nil {
		panic(err)
	}
	fmt.Printf("Current Height: %d\n", blocks)
}
```
