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

<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0-dev-v3-routing.63
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "NetgearCli",
    "group": "Group",
    "properties": {
        "control": "SampleValue",
        "password": "SampleString"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- NetgearCli
<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IQueueMessage
- INetworkSwitchPoeVlanManager
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsDevice
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void Dispatch()
- public void ChangeVlan(string port, int vlanID)
- public int GetPortCurrentVlan(string port)
- public void SetPortVlan(string port, uint vlanId)
- public void SetPortPoeState(string port, bool enabled)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->

<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->

<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
