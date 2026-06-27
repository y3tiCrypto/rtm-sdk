# Raptoreum (RTM) SDK - Perl

A standard Perl module wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

```bash
perl Makefile.PL
make
make install
```

---

## Quick Start

```perl
use lib 'lib';
use RaptoreumClient;

my $client = RaptoreumClient->new(
    host => '127.0.0.1', 
    port => 8766, 
    user => 'user', 
    password => 'pass'
);

my $balance = $client->getbalance();
print "Balance: $balance RTM\n";
```
