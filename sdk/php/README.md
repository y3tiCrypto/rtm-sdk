# Raptoreum (RTM) SDK - PHP

A standard PHP SDK for the Raptoreum Core JSON-RPC interface.

---

## Installation

Install via Composer:
```bash
composer require raptoreum/rtm-sdk
```

---

## Quick Start

```php
<?php
require 'vendor/autoload.php';

use Raptoreum\RaptoreumClient;

$client = new RaptoreumClient('127.0.0.1', 8766, 'user', 'pass');
$balance = $client->getBalance();
echo "Balance: $balance RTM\n";
```
