# Codex Implementation Task: elara-macro

## Project Summary
Build a lightweight, single-binary macro recorder/player for Windows 11 (64-bit) written in Go.
The program replicates the core functionality of "Do It Again" (doitagain): record mouse/keyboard actions,
replay them, loop them, and configure behavior via a settings UI — all in one small, portable .exe.

## Repository
https://github.com/WhenMoon-afk/elara-macro

## Project Structure (already scaffolded)
```
elara-macro/
├── main.go               # Entry point, tray icon + hotkey listener init
├── recorder/
│   └── recorder.go       # Hook-based input capture (mouse + keyboard)
├── player/
│   └── player.go         # Replay engine with optional time normalization
├── storage/
│   └── storage.go        # Save/load macros as JSON files
├── settings/
│   └── settings.go       # Settings struct + JSON persistence (already implemented)
├── ui/
│   └── ui.go             # Native Windows GUI (walk library or webview2)
├── go.mod
├── go.sum
├── README.md
└── build.bat             # Builds the release .exe (already implemented)
```

## Core Functional Requirements

### 1. Recording
- Hook into global Windows mouse + keyboard events using `golang.org/x/sys/windows` or `robotgo`
- Capture: mouse moves, left/right/middle clicks, key down/up with timestamps
- Store each event as `{type, x, y, key, timestamp_ms}`
- Show a visual indicator (tray icon change or small HUD overlay) while recording

### 2. Playback
- Replay recorded events in sequence
- Honor original timing OR normalize: if normalization is ON, enforce a configurable fixed
  delay (e.g. 50ms) between all actions instead of original timestamps
- Support pause/resume mid-playback
- Support N loops (1 = play once, 0 = infinite)
- After final loop completes, return to idle state

### 3. Hotkeys (all fully configurable)
Default keybinds (stored in settings, user can remap):
- `F9`  → Start/Stop Recording
- `F10` → Start Playback
- `F11` → Pause/Resume Playback
- `F12` → Stop Everything

### 4. Settings Persistence
File: `%APPDATA%\\elara-macro\\settings.json`

```json
{
  "hotkey_record": "F9",
  "hotkey_play": "F10",
  "hotkey_pause": "F11",
  "hotkey_stop": "F12",
  "loop_count": 1,
  "normalize_timing": false,
  "normalized_delay_ms": 50,
  "last_macro_path": ""
}
```

### 5. UI Requirements
Use one of these approaches (pick the simplest that works cleanly):
- **Option A (preferred):** System tray icon with a right-click context menu + a minimal settings window using the `walk` library (`github.com/lxn/walk`)
- **Option B:** A single webview2 window (`github.com/jchv/go-webview2`) rendering a small HTML/CSS settings panel

The UI must support:
- Remapping each hotkey (click a button, press the desired key)
- Setting loop count (number input, 0 = infinite)
- Toggle: Normalize timing (checkbox)
- Normalize delay input (number input in ms, enabled only when normalize is ON)
- Save/Load macro from file (file picker)
- Status display: "Idle / Recording / Playing (loop 2 of 5)"

### 6. Single Binary Compilation
- `go build -ldflags "-H windowsgui -s -w" -o elara-macro.exe .`
- No DLLs or external files needed at runtime
- Target: `GOARCH=amd64 GOOS=windows`
- `build.bat` is already written and included

## Technical Notes & Constraints

### Input Hooking
Use Windows `SetWindowsHookEx` with `WH_KEYBOARD_LL` and `WH_MOUSE_LL` for global hooks.
You can use the `robotgo` library OR call the Win32 API directly via `golang.org/x/sys/windows`.
Prefer direct Win32 if it keeps the binary smaller.

### Timing
Use `time.Now().UnixMilli()` for capture timestamps.
Playback timing: `time.Sleep(duration)` between events.
Normalize: replace all inter-event gaps with `settings.NormalizedDelayMs`.

### No CGO Preference
Prefer pure-Go or `golang.org/x/sys` over cgo where possible to simplify cross-compilation.
If cgo is unavoidable (e.g., for the walk GUI), that is acceptable.

### Error Handling
- If a hook fails to install, show a MessageBox and exit cleanly.
- If settings file is missing, create it with defaults silently (already handled in settings.go).
- If a macro file fails to load, show an error dialog.

### Keep It Minimal
- No telemetry, no auto-update, no network calls whatsoever
- No installer required — just run the .exe
- Aim for < 20MB binary

## Deliverables
Implement ALL .go files listed in the project structure above.
Each file should be complete and compilable.
The program should build with `go build` after `go mod tidy`.
Include inline comments explaining non-obvious Win32 API usage.

## Already Implemented (do NOT overwrite)
- `settings/settings.go` — complete, do not change
- `build.bat` — complete, do not change

## Testing Guidance
Since automated testing of Win32 hooks is impractical, include a `//go:build ignore` test harness
in `recorder/recorder_manual_test.go` that prints captured events to stdout for manual verification.

## Go Module
Module name: `github.com/WhenMoon-afk/elara-macro`
Minimum Go version: 1.22

## Priority Order
1. `go.mod` update (add any missing dependencies after analysis)
2. `recorder/recorder.go` (core Win32 hook logic)
3. `player/player.go` (replay engine)
4. `storage/storage.go` (JSON save/load)
5. `ui/ui.go` (settings window + tray)
6. `main.go` (wires everything together)
