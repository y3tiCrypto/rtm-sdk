# Raptoreum (RTM) SDK - Elixir

A standard Elixir client wrapper for the Raptoreum Core JSON-RPC interface using `:httpc` and `jason`.

---

## Installation

Add Hex dependency in `mix.exs`:
```elixir
defp deps do
  [
    {:rtm_sdk, "~> 1.0.0"}
  ]
end
```

---

## Quick Start

```elixir
client = Raptoreum.Client.new("127.0.0.1", 8766, "user", "pass")
{:ok, balance} = Raptoreum.Client.get_balance(client)
IO.puts("Balance: #{balance} RTM")
```
