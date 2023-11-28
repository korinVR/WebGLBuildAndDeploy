# Unity WebGL Build and Deploy

This is a Unity package to simplify my WebGL build-and-deploy flow. Only Amazon S3 is supported for deployment.

Attention: This utility has many dependencies and will not be useful for everyone. Modify it, or use it just as a reference!  

![menu](https://user-images.githubusercontent.com/882466/167887860-090ed9ee-d0c6-47ac-84b4-00b5a0ff2b87.png)

## Requirements

- Python 3
- AWS CLI
- [Browsersync](https://browsersync.io/) (for "Start Local HTTPS Server")

## Installation

- Open Unity Package Manager
- Open "Add package from git URL..."
- Add https://github.com/korinVR/WebGLBuildAndDeploy.git?path=Package

In macOS, register the AWS CLI installation path on "Settings > WebGL Build and Deploy" because Unity can't find the path.

## Usage

In the project window, run "Create -> WebGL Build and Deploy -> Deploy Settings," and set your S3 region and URI. Enable "Add Timestamp" if you want to append a timestamp to the destination folder.

Now you can build and deploy with the "WebGL" menu. When a build is finished, the .wasm and .data size will be reported. It will help build size optimization.

Please see the WebGLBuildAndDeploy project as an example.
