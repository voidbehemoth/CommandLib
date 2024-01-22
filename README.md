# CommandLib
CommandLib is a [SalemModLoader](https://github.com/Curtbot9000/SalemModLoader) library for [Town of Salem 2](https://store.steampowered.com/app/2140510/Town_of_Salem_2/) by BlankMediaGames.

CommandLib is developed by VoidBehemoth and is unaffiliated with with BlankMediaGames.

## Development
Are you a modder that wants to use this library? Here are some details on how Commands work in CommandLib.

Everything that one should need to add a Command can be accessed by importing 'CommandLib.API'.

All Commands inherit from the Command abstract class. They must override the Execute method, which will be called after the user runs your Command.

CommandLib passes in the user's input, split into words, to the Execute method. The method returns a Tuple containing an integer and a string. The integer represents whether the Command was successfully ran or not, and the string will be sent to the user if the Command fails. The string will not be referenced if the Command is successful, so it can be null in that case.

Optionally, Commands can implement the IHelpMessage interface, which makes the built-in Command '/help' provide more helpful information to the user.

Happy modding!

## Building
Want to build the latest (potentially unreleased) version of the mod yourself? Follow these steps:

1. Make sure you have the latest version of the repo on your client.
2. Create a file in the same directory as AutoGG.csproj called SteamLibrary.targets and copy the following into it:
```
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <SteamLibraryPath>REPLACE</SteamLibraryPath>
    </PropertyGroup>
</Project>
```
3. Replace the text 'REPLACE' with the location of your 'SteamLibrary' folder (or 'ApplicationSupport/Steam' on OSX).
4. Build the mod using either the [dotnet cli](https://dotnet.microsoft.com/en-us/download), [Visual Studio](https://visualstudio.microsoft.com/), or some other means.

NOTE: This repository is licensed under the GNU General Public License v3.0. Learn more about what this means [here](https://www.tldrlegal.com/license/gnu-general-public-license-v3-gpl-3).