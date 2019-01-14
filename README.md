# FluvioFX

![FluvioFX logo](./Documentation~/images/logo.png)

## 🚧 Active development 🚧

FluvioFX is currently in early active development. While we will try to maintain backwards compatibility, until 1.0 certain features may be added or removed at any time. Use in larger production projects with care.

## Requirements

Unity version: 2018.3+

Since Unity's Visual Effect Graph currently requires HDRP, FluvioFX requires it as well. It must be installed and configured separately (see _[The High Definition Render Pipeline: Getting Started Guide for Artists](https://blogs.unity3d.com/2018/09/24/the-high-definition-render-pipeline-getting-started-guide-for-artists/)_ for more information)

### Additional performance considerations

Unity's Visual Effect Graph has a key limitation preventing the implementation of a broadphase step in the physics simulation. This has a moderate performance impact on all platforms, and severe impact on certain platforms (Metal in particular). We expect to fix this as soon as we get or figure out a possible resolution. (See [#1](https://github.com/thinksquirrel/fluviofx/issues/1) for details)

## Installation

To install this project, add the following line to `dependencies` in your `manifest.json` (in the **Packages** subfolder of your Unity project):

```json
"com.thinksquirrel.fluviofx": "https://github.com/thinksquirrel/fluviofx.git"
```

Currently, FluvioFX requires a small patch that must be added to the Visual Effect Graph before usage. This is needed in order to access some internal classes. Once installed, a dialog will pop up to take you through this process. A **FLUVIOFX** compilation file will then be automatically added to the current (and any future) build platforms.

**If you have any compiler errors after installing/updating packages**, try the following:

1. Run _Tools > FluvioFX > Install..._
2. If the above menu is missing or any files are still broken, try to uninstall and reinstall both FluvioFX and the Visual Effect Graph

This workaround will be removed once the VFX Graph's API has been finalized. See [FluvioFXInstall.cs](./Install/FluvioFXInstall.cs) for the full implementation.

## Getting started

To create a new fluid VFX asset, navigate to _Assets > Create > Visual Effects > FluvioFX Graph_

## Documentation

~~See full documentaion [here](./Documentation~/index.md).~~ Documentation is coming soon!

## Examples

Coming soon!

## License

[MIT](./LICENSE.md)
