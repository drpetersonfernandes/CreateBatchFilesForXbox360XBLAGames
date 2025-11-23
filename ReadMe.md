# Batch File Creator for Xbox 360 XBLA Games

A Windows desktop utility for simplifying the creation of batch files to launch Xbox 360 XBLA (Xbox Live Arcade) games using the Xenia emulator.

## Overview

Batch File Creator for Xbox 360 XBLA Games is a Windows application that provides a simple user interface for automatically generating `.bat` files for your game collection. It uses the **Xenia emulator** to launch the games, and this tool automates the process of creating individual, easy-to-use shortcuts for each title.

⭐ **If you find this tool useful, please give us a Star on GitHub!** ⭐

## Features

### Core Features
- **Batch File Creation**: Automatically scans a root folder and creates individual `.bat` files for each XBLA game found.
- **Simple Workflow**: A clean, single-window interface with a menu and status bar for selecting the Xenia executable and the games folder.
- **Automatic Game Discovery**: Intelligently searches within each game's directory for the specific XBLA file structure (typically inside a `000D0000` subdirectory), with a robust fallback mechanism.
- **User-Friendly Interface**: Designed for simplicity and ease of use.

### Logging & Reporting
- **Real-time Logging**: A detailed log window shows the progress of the operation, including which files are created, which folders are skipped, and any errors encountered.
- **Status Bar**: Provides at-a-glance feedback on the application's current status.
- **Silent Error Reporting**: Automatically reports unhandled exceptions and potential issues to the developer, helping to improve application stability without interrupting the user.

### Advanced Features
- **Lightweight and Portable**: No installation required; just run the executable.
- **Robust Batch Files**: Generated batch files change to the Xenia directory before launch, preventing issues with logs and configuration files.

## Supported Formats

### Input
- A root folder containing individual subfolders for each Xbox 360 XBLA game. The application first attempts to find the main game file within a specific XBLA directory structure (typically inside a `000D0000` subdirectory, e.g., `Content/0000000000000000/TITLE_ID/000D0000/`).
- If the `000D0000` directory is not found, the tool will fall back to selecting the first file it finds recursively within the game's folder. This provides flexibility for various game rip structures.

### Output
- **`.bat` (Windows Batch) files**, one for each game, placed in the root games folder.

## Requirements

- **Runtime**: [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Dependencies**: A working copy of the **Xenia emulator** (`xenia.exe`).

## Installation

1. Download the latest release from the [GitHub releases page](https://github.com/drpetersonfernandes/CreateBatchFilesForXbox360XBLAGames/releases).
2. Extract the ZIP file to a folder of your choice.
3. Run `CreateBatchFilesForXbox360XBLAGames.exe`.

## Usage

### Creating Batch Files

1.  **Select Xenia Path**: Click "Browse" next to "Xenia Path" and locate your `xenia.exe` file.
2.  **Select Games Folder**: Click "Browse" next to "Games Folder" and select the root folder that contains all your individual XBLA game folders.
3.  **Create Batch Files**: Click the "Create Batch Files" button.
4.  **Monitor Progress**: The application will scan the folders and create the `.bat` files. The log window will display the results. The generated batch files will appear in the same folder you selected as the "Games Folder", ready to be used.

## How It Works (Technical Details)

The application performs the following steps:
1.  **Xenia Path Selection**: You select the `xenia.exe` file. This path is used to construct the batch file command.
2.  **Games Folder Selection**: You select a root directory containing subfolders, where each subfolder represents an XBLA game.
3.  **Game Discovery**: For each subfolder in the "Games Folder":
    *   It first searches for a `000D0000` subdirectory (e.g., `GameTitle/Content/0000000000000000/TITLE_ID/000D0000/`). If found, it takes the first file within that directory as the game's main executable.
    *   If `000D0000` is not found, it performs a recursive search within the game's subfolder and selects the first file it encounters. This provides a fallback for less common directory structures.
4.  **Batch File Generation**: For each discovered game file, a `.bat` file is created in the root "Games Folder" with the name of the game's subfolder. The content of the batch file is structured to:
    *   Change the current directory to where `xenia.exe` is located (`cd /d "C:\Path\To\Xenia"`). This ensures Xenia can find its configuration and log files correctly.
    *   Launch Xenia with the specific game file as an argument (`start "" "xenia.exe" "C:\Path\To\Game\GameFile.xex"`).

## Troubleshooting

### Common Issues
- **"No valid game folders found"**: This usually means the folder you selected as the "Games Folder" does not contain any subdirectories. Ensure it's the parent folder of all your game folders.
- **"Game file not found" / Files are skipped**:
    - The tool primarily looks for a game file inside a `000D0000` subfolder within each game's directory, which is common for XBLA titles.
    - If `000D0000` is not found, it will attempt to use the first file found recursively within the game's folder.
    - If your game rip has a significantly different structure and no files are found, it will be skipped. The application will log the directory structure of skipped folders to help with debugging.
- **Permissions**: Ensure the application has permission to write new `.bat` files into your selected "Games Folder". Running as an administrator may help if you encounter access errors.
- **Incorrect .NET Runtime**: Ensure you have the correct [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed.

## Acknowledgements

- **Xenia Project**: This tool is a companion utility for the excellent [Xenia emulator](https://xenia.jp/). All credit for the emulation itself goes to the Xenia team.
- **Development**: Created by [Pure Logic Code](https://www.purelogiccode.com).

## Support & Contributing

- **Website**: [purelogiccode.com](https://www.purelogiccode.com)
- **GitHub**: [CreateBatchFilesForXbox360XBLAGames Repository](https://github.com/drpetersonfernandes/CreateBatchFilesForXbox360XBLAGames)
- **Issues**: Report bugs and request features on the GitHub issues page.

---
⭐ **Remember to Star this repository if you find it useful!** ⭐

If you like the software, consider a donation on [purelogiccode.com/donate](https://www.purelogiccode.com/donate).