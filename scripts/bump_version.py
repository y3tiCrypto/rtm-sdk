import os
import sys
import json
import re

def main():
    root_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    version_file = os.path.join(root_dir, "version.json")
    
    if not os.path.exists(version_file):
        print(f"Error: central version file not found at {version_file}")
        sys.exit(1)
        
    with open(version_file, "r") as f:
        config = json.load(f)
        
    current_version = config.get("version")
    
    if len(sys.argv) < 2:
        print(f"Current SDK Version: {current_version}")
        print("To bump the version, run: python scripts/bump_version.py <new_version>")
        sys.exit(0)
        
    new_version = sys.argv[1]
    print(f"Bumping SDK version from {current_version} to {new_version}...")
    
    # 1. Update version.json
    config["version"] = new_version
    with open(version_file, "w") as f:
        json.dump(config, f, indent=2)
    print("Updated version.json")
        
    # 2. Update JavaScript package.json
    js_package = os.path.join(root_dir, "sdk", "javascript", "package.json")
    if os.path.exists(js_package):
        with open(js_package, "r") as f:
            js_cfg = json.load(f)
        js_cfg["version"] = new_version
        with open(js_package, "w") as f:
            json.dump(js_cfg, f, indent=2)
        print("Updated sdk/javascript/package.json")
            
    # 3. Update TypeScript package.json
    ts_package = os.path.join(root_dir, "sdk", "typescript", "package.json")
    if os.path.exists(ts_package):
        with open(ts_package, "r") as f:
            ts_cfg = json.load(f)
        ts_cfg["version"] = new_version
        with open(ts_package, "w") as f:
            json.dump(ts_cfg, f, indent=2)
        print("Updated sdk/typescript/package.json")
            
    # 4. Update C# csproj
    cs_proj = os.path.join(root_dir, "sdk", "csharp", "RaptoreumSdk", "RaptoreumSdk.csproj")
    if os.path.exists(cs_proj):
        with open(cs_proj, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf"<Version>{re.escape(current_version)}</Version>",
            f"<Version>{new_version}</Version>",
            content
        )
        with open(cs_proj, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/csharp/RaptoreumSdk/RaptoreumSdk.csproj")
            
    # 5. Update F# fsproj
    fs_proj = os.path.join(root_dir, "sdk", "fsharp", "RaptoreumSdk", "RaptoreumSdk.fsproj")
    if os.path.exists(fs_proj):
        with open(fs_proj, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf"<Version>{re.escape(current_version)}</Version>",
            f"<Version>{new_version}</Version>",
            content
        )
        with open(fs_proj, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/fsharp/RaptoreumSdk/RaptoreumSdk.fsproj")
            
    # 6. Update Python module __version__
    py_client = os.path.join(root_dir, "sdk", "python", "raptoreum.py")
    if os.path.exists(py_client):
        with open(py_client, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf'__version__ = "{re.escape(current_version)}"',
            f'__version__ = "{new_version}"',
            content
        )
        with open(py_client, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/python/raptoreum.py")
            
    # 7. Update Go Version constant
    go_client = os.path.join(root_dir, "sdk", "go", "client.go")
    if os.path.exists(go_client):
        with open(go_client, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf'const Version = "{re.escape(current_version)}"',
            f'const Version = "{new_version}"',
            content
        )
        with open(go_client, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/go/client.go")
            
    # 8. Update TypeScript SDK_VERSION
    ts_src = os.path.join(root_dir, "sdk", "typescript", "src", "index.ts")
    if os.path.exists(ts_src):
        with open(ts_src, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf"export const SDK_VERSION = '{re.escape(current_version)}';",
            f"export const SDK_VERSION = '{new_version}';",
            content
        )
        with open(ts_src, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/typescript/src/index.ts")
            
    # 9. Update JavaScript SDK_VERSION
    js_src = os.path.join(root_dir, "sdk", "javascript", "src", "index.js")
    if os.path.exists(js_src):
        with open(js_src, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf"const SDK_VERSION = '{re.escape(current_version)}';",
            f"const SDK_VERSION = '{new_version}';",
            content
        )
        with open(js_src, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/javascript/src/index.js")
            
    # 10. Update C# Version constant
    cs_client = os.path.join(root_dir, "sdk", "csharp", "RaptoreumSdk", "RaptoreumClient.cs")
    if os.path.exists(cs_client):
        with open(cs_client, "r", encoding="utf-8") as f:
            content = f.read()
        content = re.sub(
            rf'public const string Version = "{re.escape(current_version)}";',
            f'public const string Version = "{new_version}";',
            content
        )
        with open(cs_client, "w", encoding="utf-8") as f:
            f.write(content)
        print("Updated sdk/csharp/RaptoreumSdk/RaptoreumClient.cs")
            
    # 11. Update Markdown files (README.md, SECURITY.md, and all docs/*.md)
    md_files = [
        os.path.join(root_dir, "README.md"),
        os.path.join(root_dir, "SECURITY.md")
    ]
    docs_dir = os.path.join(root_dir, "docs")
    if os.path.exists(docs_dir):
        for file in os.listdir(docs_dir):
            if file.endswith(".md"):
                md_files.append(os.path.join(docs_dir, file))
                
    for filepath in md_files:
        if os.path.exists(filepath):
            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()
            content = content.replace(current_version, new_version)
            with open(filepath, "w", encoding="utf-8") as f:
                f.write(content)
            print(f"Updated version references in {os.path.relpath(filepath, root_dir)}")
            
    print("Monorepo version bump completed successfully!")

if __name__ == "__main__":
    main()
