# Unity WebGL Build and Deploy

This is a Unity package to simplify my WebGL build-and-deploy flow. Only Amazon S3 is supported for deployment.

Attention: This utility is only designed for me and has many dependencies, and will not be useful for everyone. Use it just as a reference!
Attention: It might not work on macOS (I use Windows).

## Requirements

- Python 3
- AWS CLI
- [Browsersync](https://browsersync.io/) (for "Start Local HTTPS Server")

## Installation

- Open Unity Package Manager
- Open "Add package from git URL..."
- Add https://github.com/korinVR/WebGLBuildAndDeploy.git?path=Package

## Usage

In the project window, run "Create -> WebGL Build Tools -> Deploy Settings," and set your S3 region and URI. Enable "Add Timestamp" if you want to add a timestamp to the destination folder.

Now you can build and deploy with the "WebGL" menu. When a build is finished, the .wasm and .data size will be reported and [Build Report Inspector](https://github.com/Unity-Technologies/BuildReportInspector) will automatically open.

Please see the WebGLBuildAndDeploy project as an example.
