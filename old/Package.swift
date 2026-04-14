// swift-tools-version: 5.9
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "UOCommander",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(
            name: "UOCommander",
            targets: ["UOCommander"]
        ),
    ],
    targets: [
        .executableTarget(
            name: "UOCommander",
            path: "Sources"
        ),
    ]
)
