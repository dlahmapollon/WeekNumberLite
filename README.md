# WeekNumber Lite

Personal fork of [voltura/WeekNumberLite](https://github.com/voltura/WeekNumberLite).

Shows the current ISO 8601 week number as a tray icon in the Windows taskbar. The tooltip shows the week and today's date, the context menu only offers "Beenden". The icon follows the light/dark taskbar theme and refreshes at midnight, after resume from standby and when the system time changes.

## Build

Requires Windows 10/11 with .NET Framework 4.8 (part of Windows) and the Visual Studio Build Tools (MSBuild).

```bat
build.bat
```

The result is `bin\Release\WeekNumberLite.exe`. To start it automatically, put a shortcut to it into `shell:startup`.


## Build validation

Builds require MSBuild and the .NET Framework 4.8 Developer Pack.
See [Windows build and icon regression](tests/README.md) for local test commands,
CI artifacts and the desktop acceptance checklist.
