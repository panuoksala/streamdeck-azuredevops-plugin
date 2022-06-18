# Stream Deck Azure DevOps plugin

Source code of Azure DevOps plugin by Panu Oksala for [Elgato Stream Deck][Stream Deck]. This project works currently only on Windows devices.
Project is created by using the [Stream Deck C# Toolkit][Stream Deck C# Toolkit Homepage].
Use GitHub issues to submit any bugs / feature requests.

## How to use this plugin

Install the plugin from Stream Deck Store and create Azure DevOps PAT token with **read & execute** permissions for builds and releases.

### Configurations

| Setting                   | Description|
|---------------------------|------------|
| Title                     |Overlay text on top of the Azure DevOps icon.|
| Organization URL          |The Azure DevOps URL with or without https://. For example : dev.azure.com/{your organization}.|
| Project name              |The personal access token with **read and execute** permissions for build and release pipelines. Dont create PAT tokens with full access!|
| PAT                       |The personal access token with read and execute permissions for build and release pipelines. Dont create PAT tokens with full access!|
| Pipeline type             |Build or release depending on what kind of action you want to trigger|
| Definition Id             |The build or release definition ID. Open the pipeline in edit mode and copy the ID from URL.|
| Branch name               |Leave empty to use pipelines default branch, or specify branch name that you want to build.|
| Tap action                |What happens when StreamDeck button is pressed|
| Long press action         |What happens if the StreamDeck button is pressed over one second|
| Status update frequency   |How often the build/release status is requested automatically from Azure DevOps. Makes API calls with given interval.|
| Errors                    |Shows the possible error message that is received when button action is invoked.|

On success build or release init the Stream Deck button will show OK sign for a short while.
If Stream Deck shows red question icon on top right corner of the button, check logs from plugin folder for more details.

## Contribution guide

1. Install Stream Deck application
2. Clone the repository
3. Build with Visual Studio
4. Visual Studio should automatically add Azure DevOps button into your Stream Deck app / device.
5. To debug the app just run it and attach debugger into StreamDeck application (you might have two so try both).
6. If you experience problems try to run the Visual Studio in Administrator mode.


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


[![.NET](https://github.com/panuoksala/streamdeck-azuredevops-plugin/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/panuoksala/streamdeck-azuredevops-plugin/actions/workflows/dotnet.yml)
