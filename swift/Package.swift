// swift-tools-version: 5.7
import PackageDescription

let package = Package(
    name: "RaptoreumClient",
    platforms: [
        .macOS(.v10_15), .iOS(.v13)
    ],
    products: [
        .library(name: "RaptoreumClient", targets: ["RaptoreumClient"])
    ],
    targets: [
        .target(name: "RaptoreumClient", path: "Sources"),
        .executableTarget(name: "RaptoreumExample", dependencies: ["RaptoreumClient"], path: "Example")
    ]
)
