# Elara Macro

A compact Windows 11 macro recorder/player — C# .NET 8 WinForms, single self-contained `.exe`.

## Features
- Small floating popup window (like DoItAgain), hides to tray
- Global keyboard + mouse recording via low-level Win32 hooks
- Playback via `SendInput` with original or normalized timing
- Loop N times or infinitely (loop count `0` = infinite)
- Mouse move threshold filtering to reduce junk events
- Hotkey rebinding in-app (click any hotkey label)
- Multiple named macros, saved as JSON

## Default Hotkeys

| Action | Key |
|---|---|
| Start / Stop Recording | F9 |
| Play | F10 |
| Pause / Resume | F11 |
| Stop | F12 |

## Build

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
# Debug build
.\build.ps1

# Release publish (framework-dependent, ~200 KB)
.\build.ps1 -Publish
```

Output: `ElaraMacro\bin\Release\net8.0-windows\win-x64\publish\ElaraMacro.exe`

## Data files

Stored in `%APPDATA%\ElaraMacro\`:
- `settings.json` — hotkeys, window position, playback settings
- `macros.json` — saved macro library

## Notes
- Recording captures **all** global keyboard input. Do not record passwords or secrets.
- Playback into elevated windows (UAC prompts, Task Manager) is blocked by Windows security.
- Coordinate-based clicks will break if monitor layout or window position changes between record and playback.
