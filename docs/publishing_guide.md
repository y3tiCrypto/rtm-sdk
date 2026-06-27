# SDK Packaging & Publishing Maintenance Guide

This document is a reference guide for repository maintainers when deploying updates of the RTM Multi-Language SDK to the respective package registries.

---

## 🐍 1. Python (PyPI)
*   **Directory**: `sdk/python/`
*   **Tooling**: `setuptools`, `wheel`, `twine`
*   **Procedure**:
    1.  Update the version in `sdk/python/setup.py`.
    2.  Build the source and binary wheels:
        ```bash
        cd sdk/python
        python setup.py sdist bdist_wheel
        ```
    3.  Upload to PyPI:
        ```bash
        twine upload dist/*
        ```

---

## 📦 2. JavaScript & TypeScript (NPM)
*   **Directories**: `sdk/javascript/`, `sdk/typescript/`
*   **Tooling**: `npm`
*   **Procedure**:
    1.  Update the version in `package.json`.
    2.  For TypeScript, build javascript targets:
        ```bash
        cd sdk/typescript
        npm run build
        ```
    3.  Log in and publish:
        ```bash
        npm login
        npm publish --access public
        ```

---

## 🐘 3. PHP (Composer / Packagist)
*   **Directory**: `sdk/php/`
*   **Tooling**: Git, Packagist integration
*   **Procedure**:
    1.  Update `composer.json` version metadata (or let VCS tagging handle it).
    2.  Commit changes and push a new git tag to GitHub:
        ```bash
        git tag -a v1.0.1 -m "Release v1.0.1"
        git push origin v1.0.1
        ```
    3.  Packagist automatically detects the tag via the repository webhook and publishes the package.

---

## 🐹 4. Go (Go Modules)
*   **Directory**: `sdk/go/`
*   **Tooling**: Git tags
*   **Procedure**:
    1.  Go does not use a central upload registry. It resolves modules directly from version control.
    2.  Tag the release using a sub-path prefix (crucial for mono-repos):
        ```bash
        git tag sdk/go/v1.0.1
        git push origin sdk/go/v1.0.1
        ```
    3.  Developers can now download the module using:
        ```bash
        go get github.com/Raptor3um/rtm-sdk/sdk/go@v1.0.1
        ```

---

## 🦀 5. Rust (Crates.io)
*   **Directory**: `sdk/rust/`
*   **Tooling**: `cargo`
*   **Procedure**:
    1.  Update version inside `sdk/rust/Cargo.toml`.
    2.  Log in to crates.io and run verification:
        ```bash
        cd sdk/rust
        cargo login <api-token>
        cargo publish --dry-run
        ```
    3.  Publish:
        ```bash
        cargo publish
        ```

---

## 💼 6. C# & F# (.NET NuGet)
*   **Directories**: `sdk/csharp/`, `sdk/fsharp/`
*   **Tooling**: `dotnet cli`
*   **Procedure**:
    1.  Increment `<Version>` in the `.csproj` or `.fsproj` metadata.
    2.  Package the binaries:
        ```bash
        cd sdk/csharp/RaptoreumSdk
        dotnet pack -c Release
        ```
    3.  Push to NuGet gallery:
        ```bash
        dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json
        ```
