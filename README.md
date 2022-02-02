# Collapse Comments

[![Build status](https://ci.appveyor.com/api/projects/status/cl487k8r8rcafc1d?svg=true)](https://ci.appveyor.com/project/mrlacey/collapsecomments)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![Works with Visual Studio 2017](https://img.shields.io/static/v1.svg?label=VS&message=2017&color=5F2E96)
![Works with Visual Studio 2019](https://img.shields.io/static/v1.svg?label=VS&message=2019&color=5F2E96)
![Works with Visual Studio 2022](https://img.shields.io/static/v1.svg?label=VS&message=2022&color=A853C7)

Download this extension from the [VS Gallery](https://marketplace.visualstudio.com/items?itemName=MattLaceyLtd.CollapseComments)
or get the [CI build](http://vsixgallery.com/extension/CollapseComments.a1dfaad6-6e8d-420a-807b-ebbbc0e7a6bf/).

---------------------------------------

Simple VSIX extension that adds a command to collapse comments in the open file.

## Features

- Ctrl+M, Ctrl+C collapses all comments (and using/Import statements)
- Ctrl+M, Ctrl+D expands all comments and closes all other areas
- Ctrl+M, Ctrl+F toggles all comments
- _Should_ work with all file types that have the concept of comments
- Can also collapse (or expand) using/import directives
- Can be configured to collapse comments when a document is opened
- Also support collapsing multi-line C# strings

![Commands shown in document context menu](./art/screenshot-menu.png)

See the [change log](CHANGELOG.md) for changes and roadmap.

## Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the [Extensibility Essentials (2017)](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ExtensibilityEssentials) or [Extensibility Essentials 2019](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.ExtensibilityEssentials2019)
extension for Visual Studio which enables some features
used by this project.
