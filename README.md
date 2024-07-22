![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# Essentials Netgear CLI Plugin (c) 2024

## License

Provided under MIT license

## Overview

This plugin controls Vlan assignments for a Netgear switch interface port.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They are referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

Dependencies will be installed automatically by Visual Studio on opening. Use the Nuget Package Manager in Visual Studio to manage nuget package dependencies. All files will be output to the `output` directory at the root of repository.

### Installing Different versions of PepperDash Core

If a different version of PepperDash Core is needed, use the Visual Studio Nuget Package Manager to install the desired version.

### Usage
The Vlan assignment is set by calling the ChangeVlan method via DevJson. A sample json is provided below:
```json

{
  "deviceKey": "NetgearCLI",
  "methodName": "ChangeVlan",
  "params": ["0/1", 2]
}
```

