# elara-macro

A lightweight, portable macro recorder and player for Windows 11 (64-bit).
Record mouse and keyboard actions, replay them, and loop them — all in a single `.exe`.

## Features

- 🔴 **Record** — captures all mouse clicks, movements, and keypresses
- ▶️ **Play** — replays recordings with original or normalized timing
- 🔁 **Loop** — repeat N times or infinitely
- ⌨️ **Configurable hotkeys** — remap Record/Play/Pause/Stop to any key
- ⏱️ **Timing normalization** — optionally enforce fixed delay between actions
- 💾 **Save/Load macros** — persist recordings as JSON files
- 🪟 **No install needed** — single portable .exe

## Default Hotkeys

| Action | Default Key |
|--------|-------------|
| Start/Stop Recording | F9 |
| Start Playback | F10 |
| Pause/Resume | F11 |
| Stop All | F12 |

## Building

```bat
build.bat
```

Requires Go 1.22+ and GCC (for cgo / walk library).

## Settings

Stored at `%APPDATA%\elara-macro\settings.json`. Edit via the settings window or directly.

## License

MIT
