# Sidebar — A Longhorn-style Sidebar

## Note

* Sidebar is an independent project inspired by the LongBar project (https://sourceforge.net/projects/longbar/). It is not a branch, fork, or continuation of LongBar.
* This project contains portions of code and XAML derived from LongBar, which are used and modified in accordance with the terms of the Microsoft Public License (Ms-PL).
* Information regarding third-party code and attribution can be found in the `NOTICE` file. The original LongBar license text is included in the `LICENSE - LongBar` file.
* This project utilizes AI assistance in some aspects of development.
* Sep. 23, 2026: Due to new ideas about the project’s direction and the difficulty of maintaining and updating the old system, the project will undergo a major overhaul soon. This overhaul will also be incompatible with the previous tiles. Therefore, tile developers for this project, please take note.

## Known Issues of Notification History Tile

Since the Notification History Tile captures notifications using a rather hacky approach (see [https://github.com/windowsmodern/windows-notification-server](https://github.com/windowsmodern/windows-notification-server)), leftover `NotifyServer.exe` processes may remain running if the tile is not properly disposed, or if the sidebar with pinned notification history entries is not closed correctly. Such residual processes will keep intercepting notifications, which prevents notifications that should be shown by `explorer.exe` from appearing normally.

The simplest way to handle a lingering `NotifyServer.exe` process is to terminate it directly once it appears (note that this may cause `explorer.exe` to crash and restart). Alternatively, locate `NotifyServer.exe` in its directory and run this command line:

```
NotifyServer.exe -unregister-exit
```

This shuts down NotifyServer cleanly, and it is exactly what happens when the Notification History Tile is disposed properly.

## Required Runtime Environment

- Microsoft .NET Framework 4.0
- Microsoft .NET Framework 3.0 (3.5 SP1)
- Microsoft Visual C++ 2015 Redistributable

## Release

Download the latest release from the Releases section.

<a href="https://github.com/WindowsModern/sidebar/releases" target="_blank"><img src="https://img.shields.io/github/v/release/WindowsModern/sidebar"></a>

## SDK

Download: [https://github.com/WindowsModern/sidebar/releases/tag/sdk2026.10.22](https://github.com/windowsmodern/sidebar/releases/tag/sdk2026.11.24)

## Acknowledgements

Sidebar was inspired by the LongBar project. Thanks to the original LongBar authors for their work.

## Screenshot

<img width="1024" height="768" alt="Windows XP Professional-2026-10-22-10-24-30" src="https://github.com/user-attachments/assets/2beb5aca-10df-4f89-a5c1-64545a969a27" />


