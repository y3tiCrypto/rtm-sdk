# Raptoreum (RTM) SDK - Scala

A lightweight, zero-dependency Scala client for the Raptoreum Core JSON-RPC interface.

---

## Installation

Add in `build.sbt`:
```scala
libraryDependencies += "org.raptoreum" %% "rtm-sdk" % "1.0.0"
```

---

## Quick Start

```scala
import org.raptoreum.RaptoreumClient

val client = new RaptoreumClient("127.0.0.1", 8766, "user", "pass")
val info = client.getBlockchainInfo()
println(info)
```
