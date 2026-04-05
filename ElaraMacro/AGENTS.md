# AGENTS.md

## Goal
Maintain a compact, robust Windows macro recorder/player with a small popup UI similar to DoItAgain.

## Constraints
- Keep implementation simple and local-first.
- Prefer explicit state handling over hidden behavior.
- Avoid unnecessary dependencies.
- Preserve compact popup behavior and tray operation.
- Do not remove defensive persistence, hook isolation, or stop-hotkey safety.

## Architecture
- UI thread handles forms and tray UI.
- Hook installation lives on a dedicated STA thread with its own message loop.
- Recording and playback are state-gated (AppState enum).
- JSON persistence is atomic (write temp, File.Replace).

## UX
- Close button hides to tray; only Quit in the tray menu exits.
- Stop hotkey must always work during playback.
- Unsaved recordings are never silently discarded.
- Loop count 0 = infinite.
