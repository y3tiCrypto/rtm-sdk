# Raptoreum (RTM) SDK - Haskell

A standard Haskell client wrapper for the Raptoreum Core JSON-RPC interface, built on `http-conduit` and `aeson`.

---

## Installation

Add to your `.cabal` build dependencies:
```cabal
build-depends: rtm-sdk
```

---

## Quick Start

```haskell
import Raptoreum
import Data.Aeson

main :: IO ()
main = do
    let client = newClient "127.0.0.1" 8766 "user" "pass" False
    res <- request client "getblockchaininfo" []
    case res of
        Left err -> putStrLn $ "Error: " ++ err
        Right val -> print val
```
