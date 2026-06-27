# Raptoreum (RTM) SDK - Erlang

A standard Erlang client wrapper for the Raptoreum Core JSON-RPC interface, built on `inets` and `ssl`.

---

## Installation

Using `rebar3`:
```erlang
{deps, [
    {rtm_sdk, "1.0.0"}
]}.
```

---

## Quick Start

```erlang
Client = raptoreum:new("127.0.0.1", 8766, "user", "pass", false),
{ok, Response} = raptoreum:get_balance(Client),
io:format("Balance JSON: ~s~n", [Response]).
```
