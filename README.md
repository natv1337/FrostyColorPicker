# FrostyColorPicker
A [Frosty Editor](https://github.com/CadeEvs/FrostyToolsuite) plugin for converting Vector colors to and from other color methods. Works on versions 1.0.6.1 and higher.

![Plugin Preview](https://github.com/NatalieWhatever/FrostyColorPicker/blob/master/Resources/plugin-preview.png)

## Installation
Download the [latest release](https://github.com/NatalieWhatever/FrostyColorPicker/releases/latest) and extract the zip file. Move *all* of the DLL files in to the Plugins folder in your FrostyEditor directory.

## Limitations
* Vectors with high values are currently not supported as I don't know how to properly work with them.
* HDR calculation has only been tested with one game and might not translate over to most games.

## Compiling
1) Download the code or clone this repository.
2) Open the .sln or .csproj and add the missing assemblies.
	- The FrostyControls, FrostyCore, and FrostySdk assemblies can be obtained from your FrostyEditor installation.
	- Install the Microsoft.CSharp, Microsoft.Xaml.Behaviors.Wpf, and PixiEditor.ColorPicker nuget packages.
3) Build with any configuration or your choice.

## Credits
* [PixiEditor's ColorPicker package](https://github.com/PixiEditor/ColorPicker) for the color picker.