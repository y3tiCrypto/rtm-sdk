# Monorepo Version Control & Updates Guide

To maintain uniformity across all target languages, the Raptoreum SDK monorepo manages a single unified version configuration.

---

## 📌 Central Configuration (`version.json`)
The central version number is declared in the root [version.json](file:///e:/RTM-Scripts/rtm-sdk/version.json):
```json
{
  "version": "1.0.1"
}
```

---

## ⚙️ Querying the Version Programmatically

Each language SDK exports the version string as a public constant:

### 1. Python
```python
import raptoreum
print(raptoreum.__version__) # "1.0.1"
```

### 2. JavaScript & TypeScript
```typescript
import { SDK_VERSION } from 'rtm-sdk'; // JS
import { SDK_VERSION } from '@rtm-sdk/typescript'; // TS
console.log(SDK_VERSION); // "1.0.1"
```

### 3. Go
```go
import "github.com/Raptor3um/rtm-sdk/go"
fmt.Println(raptoreum.Version) // "1.0.1"
```

### 4. C# & F#
```csharp
using RaptoreumSdk;
Console.WriteLine(RaptoreumClient.Version); // "1.0.1"
```

---

## 🚀 Bumping the Version Monorepo-Wide

To bump version numbers across all package descriptors, constant strings, README files, security logs, and integration installation guides, run the automated version bumper:

```bash
python scripts/bump_version.py <new_version>
```

### Example:
```bash
python scripts/bump_version.py 1.0.1
```

This updates:
1.  Central `version.json`
2.  JavaScript `package.json`
3.  TypeScript `package.json`
4.  C# `RaptoreumSdk.csproj` & `RaptoreumClient.cs` version constants
5.  F# `RaptoreumSdk.fsproj`
6.  Python `raptoreum.py` `__version__`
7.  Go `client.go` `Version` constant
8.  TypeScript `index.ts` & JavaScript `index.js` version constants
9.  All installation examples inside `README.md`
10. Version logs in `SECURITY.md`
11. Version tags and paths in the `docs/` folder
