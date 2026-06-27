# Raptoreum (RTM) SDK - Clojure

A standard Clojure client wrapper for the Raptoreum Core JSON-RPC interface, built on `clj-http` and `cheshire`.

---

## Installation

Add Leiningen dependency inside `project.clj`:
```clojure
[rtm-sdk "1.0.0"]
```

---

## Quick Start

```clojure
(require '[raptoreum :as rtm])

(def client (rtm/make-client :host "127.0.0.1" :port 8766 :user "user" :password "pass"))
(def balance (rtm/get-balance client))
(println "Balance: " balance)
```
