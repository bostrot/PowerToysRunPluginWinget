# Winget Plugin for PowerToys Run (WIP)

This is a plugin for [PowerToys Run](https://github.com/microsoft/PowerToys/wiki/PowerToys-Run-Overview) that allows you to search and install packages from the [Winget](https://github.com/microsoft/winget-cli) package manager.

**Note:** This plugin is still a work in progress. It is not yet ready for production use. Installing and finding of winget packages should work.

## Features

- Search for packages from the Winget repository
- Install packages directly from PowerToys Run
- View package details and version information

## Installation

1. Download the latest release of the Winget Plugin from the [releases page](https://github.com/bostrot/PowerToysRunPluginWinget/releases).
2. Extract the contents of the zip file to your PowerToys modules directory (usually `C:\Program Files\PowerToys\src\modules\launcher\Plugins`).
3. Restart PowerToys.

## Usage

1. Open PowerToys Run (default shortcut is `Alt+Space`).
2. Type `winget` followed by your search query.
3. Select a package from the search results and press `Enter` to install it.

## Build

1. Clone the [PowerToys repo](https://github.com/microsoft/PowerToys).
2. cd into the `PowerToys` directory.
3. Initialize the submodules: `git submodule update --init --recursive`
4. Clone this repo into the `PowerToys/src/modules/launcher/Plugins` directory. (`git clone https://github.com/bostrot/PowerToysRunPluginWinget PowerToys/src/modules/launcher/Plugins/Community.PowerToys.Run.Plugin.Winget`)
5. Open the `PowerToys.sln` solution in Visual Studio.
6. Add this project to the `PowerToys.sln` solution. (Right-click on the `PowerToys` solution in the Solution Explorer (under the path PowerToys/src/modules/launcher/Plugins) and select `Add > Existing Project...` and select the `Community.PowerToys.Run.Plugin.Winget.csproj` file.)
7. Build the solution.
8. Run the `PowerToys` project.

## Contributing

Contributions are welcome! Please see our [contributing guidelines](CONTRIBUTING.md) for more information.

## License

This project is licensed under the [MIT License](LICENSE).

## Create your own PowerToys Run Plugin

I wrote an article about that in my blog which might help you to get started: [How to create a PowerToys Run plugin](https://senpai.club/how-to-create-a-powertoys-run-plugin/index.html)
