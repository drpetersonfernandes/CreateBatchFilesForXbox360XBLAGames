# Batch File Creator for Xbox 360 XBLA Games

A Windows desktop utility for simplifying the creation of batch files to launch Xbox 360 XBLA (Xbox Live Arcade) games using the Xenia emulator.

## Overview

Batch File Creator for Xbox 360 XBLA Games is a Windows application that provides a simple user interface for automatically generating `.bat` files for your game collection. It uses the **Xenia emulator** to launch the games, and this tool automates the process of creating individual, easy-to-use shortcuts for each title.

⭐ **If you find this tool useful, please give us a Star on GitHub!** ⭐

## Features

### Core Features
- **Batch File Creation**: Automatically scans a root folder and creates individual `.bat` files for each XBLA game found.
- **Simple Workflow**: A clean, single-window interface for selecting the Xenia executable and the games folder.
- **Automatic Game Discovery**: Intelligently searches within each game's directory for the specific XBLA executable file structure (typically inside a `000D0000` subdirectory).
- **User-Friendly Interface**: Designed for simplicity and ease of use.

### Logging & Reporting
- **Real-time Logging**: A detailed log window shows the progress of the operation, including which files are created, which folders are skipped, and any errors encountered.
- **Silent Error Reporting**: Automatically reports unhandled exceptions and potential issues to the developer, helping to improve application stability without interrupting the user.

### Advanced Features
- **Lightweight and Portable**: No installation required; just run the executable.
- **Memory Management**: Proper resource disposal and cleanup.

## Supported Formats

### Input
- A root folder containing individual subfolders for each Xbox 360 XBLA game. The application expects a standard XBLA directory structure where the main game file is located within a path like `Content/0000000000000000/TITLE_ID/000D0000/`.

### Output
- **`.bat` (Windows Batch) files**, one for each game, placed in the root games folder.

## Requirements

- **Operating System**: Windows 7 or later
- **Runtime**: [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
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

## About Launching XBLA Games with Xenia

Xenia is an experimental open-source emulator for the Xbox 360. To launch a specific game directly, you would normally need to use the command prompt and provide the path to both the Xenia executable and the game file.

This tool automates that process by creating simple batch (`.bat`) files that contain the necessary commands. This allows you to launch your favorite XBLA games with a simple double-click, making it much easier to integrate them into game launchers or your desktop.

## Troubleshooting

### Common Issues
- **"No valid game folders found"**: This usually means the folder you selected as the "Games Folder" does not contain any subdirectories. Ensure it's the parent folder of all your game folders.
- **"Game file not found" / Files are skipped**: The tool specifically looks for a game file inside a `000D0000` subfolder within each game's directory. If your game rip has a different structure, it will be skipped. The application will attempt to log the directory structure of skipped folders to help with debugging.
- **Permissions**: Ensure the application has permission to write new `.bat` files into your selected "Games Folder". Running as an administrator may help if you encounter access errors.

### Error Reporting
- The application automatically reports critical errors to the developer for analysis.
- Always check the log window for detailed error messages and information about why a file might have been skipped.

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