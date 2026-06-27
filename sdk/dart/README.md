# Raptoreum (RTM) SDK - Dart

A standard, lightweight, zero-dependency Dart SDK for the Raptoreum Core JSON-RPC interface.

---

## Installation

Add this package to your `pubspec.yaml`:
```yaml
dependencies:
  rtm_sdk:
    path: ../dart # Or standard git/pub package references
```

---

## Quick Start

```dart
import 'package:rtm_sdk/raptoreum.dart';

void main() async {
  final client = RaptoreumClient(host: '127.0.0.1', port: 8766, user: 'user', password: 'pass');
  final balance = await client.getBalance();
  print('Balance: $balance RTM');
}
```
