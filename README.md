# Stream Deck Azure DevOps plugin
Source code of Azure DevOps plugin for [Elgato Stream Deck][Stream Deck]. This project works currently only on Windows devices.
This project is created by using the [Stream Deck C# Toolkit][Stream Deck C# Toolkit Homepage].

## How to use this plugin
Install the plugin from Stream Deck Store and create PAT token with read & execute rights to build and releases.
### Configurations:
Organization name: The name of the Azure DevOps organization. The Azure DevOps url contains the organization name: dev.azure.com/{organization name}.<br />
Project name: The name of the project like it is in the project URL. So spaces must be replaced with %20. For example Example Project is Example%20Project<br />
PAT: The personal access token with read and execute permissions for build and release pipelines. Dont create PAT tokens with full access!<br />
Pipeline type: Build or release depending on what kind of action you want to trigger<br />
Definition Id: The build or release definition ID. Open the pipeline in edit mode and copy the ID from URL. For example: https://dev.azure.com/{organization name}/{projectname}/_apps/hub/ms.vss-ciworkflow.build-ci-hub?_a=edit-build-definition&id={definition ID}

## References
* [Stream Deck C# Toolkit Homepage](https://github.com/FritzAndFriends/StreamDeckToolkit)
* [Stream Deck Page][Stream Deck]
* [Stream Deck SDK Documentation][Stream Deck SDK]

<!-- References -->
[Stream Deck]: https://www.elgato.com/en/gaming/stream-deck "Elgato's Stream Deck landing page for the hardware, software, and SDK"
[Stream Deck C# Toolkit Homepage]: https://github.com/FritzAndFriends/StreamDeckToolkit "C# Stream Deck library"
[Stream Deck software]: https://www.elgato.com/gaming/downloads "Download the Stream Deck software"
[Stream Deck SDK]: https://developer.elgato.com/documentation/stream-deck "Elgato's online SDK documentation"
[Style Guide]: https://developer.elgato.com/documentation/stream-deck/sdk/style-guide/ "The Stream Deck SDK Style Guide"
[Manifest file]: https://developer.elgato.com/documentation/stream-deck/sdk/manifest "Definition of elements in the manifest.json file"
