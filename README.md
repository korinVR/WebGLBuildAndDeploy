# WebGL Build Tools

This is a Unity package to simplify my WebGL build and deployment flow. Only Amazon S3 is supported.

Attention: This tool is only developed for me and will not be useful for everyone. Modify it if needed.  
Attention: It might not work on macOS (I use Windows).

## Requirements

- Python 3
- AWS CLI
- [Browsersync](https://browsersync.io/) (for Start Local HTTPS Server)

## Installation

- Open Unity Package Manager
- Open "Add package from git URL..."
- Add https://github.com/korinVR/WebGLBuildTools.git?path=Package

## Usage

In the project window, run "Create -> WebGL Build Tools -> Deploy Settings," and set S3 region and URI. Now you can use the "WebGL" menu for build and deploy. Enable "Add Timestamp" if you want to add a timestamp to the destination folder.

Please see WebGLBuildToolTest project as an example.
