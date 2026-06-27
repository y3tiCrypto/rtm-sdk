# Raptoreum (RTM) SDK - Ruby

A standard Ruby gem client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

Install via Gem:
```bash
gem install rtm-sdk
```

---

## Quick Start

```ruby
require 'raptoreum'

client = Raptoreum::Client.new(
  host: '127.0.0.1', 
  port: 8766, 
  user: 'user', 
  password: 'pass'
)

balance = client.getbalance
puts "Balance: #{balance} RTM"
```
