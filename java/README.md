# Raptoreum (RTM) SDK - Java

A standard Java SDK for the Raptoreum Core JSON-RPC interface using Java 11's `HttpClient` and Google `Gson`.

---

## Installation

Add this dependency to your `pom.xml`:
```xml
<dependency>
    <groupId>org.raptoreum</groupId>
    <artifactId>rtm-sdk</artifactId>
    <version>1.0.0</version>
</dependency>
```

---

## Quick Start

```java
import org.raptoreum.RaptoreumClient;

public class Main {
    public static void main(String[] args) throws Exception {
        RaptoreumClient client = new RaptoreumClient("127.0.0.1", 8766, "user", "pass", false);
        double balance = client.getBalance();
        System.out.println("Balance: " + balance + " RTM");
    }
}
```
