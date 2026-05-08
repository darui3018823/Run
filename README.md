# Run — Windows Run Dialog Replacement

A lightweight Windows Run dialog replacement that intercepts Win+R and provides dark mode support, command history, and SYSTEM-level execution.

## Features

| Shortcut | Action |
|---|---|
| Win+R | Open dialog |
| Enter / OK | Run command |
| Ctrl+Shift+Enter | Run as Administrator (UAC) |
| Ctrl+Alt+Enter | Run as SYSTEM via psexec |
| ↑ / ↓ | Navigate command history |
| Escape | Close |

- **Dark / Light mode** — follows Windows system theme automatically
- **Command history** — persisted across sessions
- **System tray** — double-click or right-click to open; option to run at startup
- **Browse** — file picker for executables
- **Focus on open** — keyboard input works immediately without clicking

## Requirements

- Windows 10 / 11
- .NET 8 (Windows)
- [PsExec](https://learn.microsoft.com/sysinternals/downloads/psexec) in `PATH` *(optional — required for Ctrl+Alt+Enter)*

## Build

```
dotnet build -c Release
```

Output: `bin\Release\net8.0-windows\Run.exe`

## Install

1. Copy `Run.exe` anywhere (e.g. `%LOCALAPPDATA%\Run\`)
2. Launch it — it will appear in the system tray
3. Enable **Run at Startup** from the tray menu to register it with Windows

> The app registers itself in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Notes

- Only one instance runs at a time (mutex-guarded)
- The app suppresses Start Menu side effects when intercepting Win+R
- Win+R from other apps is intercepted globally via `WH_KEYBOARD_LL`
