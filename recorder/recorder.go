//go:build windows

// Package recorder captures global mouse and keyboard events via Win32 low-level hooks.
package recorder

// TODO: implement in Codex task
// SetWindowsHookEx(WH_KEYBOARD_LL), SetWindowsHookEx(WH_MOUSE_LL)
// Store events as []Event{Type, X, Y, Key, TimestampMS}
