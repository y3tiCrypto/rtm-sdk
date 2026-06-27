# Raptoreum (RTM) SDK - Kotlin

A JVM-compatible Kotlin SDK for the Raptoreum Core JSON-RPC interface using native JVM HttpClient and Gson.

---

## Installation

Add this dependency in Gradle:
```kotlin
implementation("org.raptoreum:rtm-sdk:1.0.0")
```

---

## Quick Start

```kotlin
import org.raptoreum.RaptoreumClient

fun main() {
    val client = RaptoreumClient("127.0.0.1", 8766, "user", "pass")
    val balance = client.getBalance()
    println("Balance: $balance RTM")
}
```
