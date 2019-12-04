# FluvioFX

![FluvioFX logo](./Documentation~/images/logo.png)

## 🚧 Active development 🚧

FluvioFX is currently in early active development. While we will try to maintain backwards compatibility, until 1.0 certain features may be added or removed at any time. Use in larger production projects with care.

## Requirements

- Unity 2019.2 or newer
- Visual Effect Graph 6.9.1 or newer

## Installation

First, make sure all required packages are installed. These *must* be installed manually before installing FluvioFX.

Once all required packages are added, add the following line to `dependencies` in your `manifest.json` (in the **Packages** subfolder of your Unity project):

```json
"com.fluvio.fx": "https://github.com/fluviofx/fluviofx.git"
```

Currently, FluvioFX requires a small patch that must be added to the Visual Effect Graph package before usage. This is needed in order to access some internal classes. Once imported, this process should happen automatically. A **FLUVIOFX** compilation file will then be automatically added to the current (and any future) build platforms, which will allow Unity to load the FluvioFX assembly.

**If you have any compiler errors after importing or reimporting packages**, try the following:

1. Run _Tools > FluvioFX > Install..._
2. If the above menu is missing or any files are still broken, try to uninstall and reinstall both FluvioFX and the Visual Effect Graph

This workaround will be removed once the VFX Graph's API has been finalized. See [FluvioFXInstall.cs](./Install/FluvioFXInstall.cs) for the full implementation, including all file modifications.

## Getting started

To create a new fluid VFX asset, navigate to _Assets > Create > Visual Effects > FluvioFX Graph_. You can also add a Fluid Particle System to an existing graph under the _Systems_ menu in the graph.

The default graphs include many helpful sticky notes with more information, so it is highly recommended to use this for initial setup.

## Documentation

~~See full documentaion [here](./Documentation~/index.md).~~ Full documentation is coming soon! For now, take a look at the sticky notes in the graph for help.

## Examples

Coming soon!

## License

[MIT](./LICENSE.md)
