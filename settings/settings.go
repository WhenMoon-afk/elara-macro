// Package settings manages user configuration with JSON persistence.
package settings

import (
	"encoding/json"
	"os"
	"path/filepath"
)

// Settings holds all user-configurable options.
type Settings struct {
	HotkeyRecord       string `json:"hotkey_record"`
	HotkeyPlay         string `json:"hotkey_play"`
	HotkeyPause        string `json:"hotkey_pause"`
	HotkeyStop         string `json:"hotkey_stop"`
	LoopCount          int    `json:"loop_count"`
	NormalizeTiming    bool   `json:"normalize_timing"`
	NormalizedDelayMS  int    `json:"normalized_delay_ms"`
	LastMacroPath      string `json:"last_macro_path"`
}

func Defaults() *Settings {
	return &Settings{
		HotkeyRecord:      "F9",
		HotkeyPlay:        "F10",
		HotkeyPause:       "F11",
		HotkeyStop:        "F12",
		LoopCount:         1,
		NormalizeTiming:   false,
		NormalizedDelayMS: 50,
	}
}

func configPath() string {
	appdata := os.Getenv("APPDATA")
	return filepath.Join(appdata, "elara-macro", "settings.json")
}

func Load() (*Settings, error) {
	p := configPath()
	data, err := os.ReadFile(p)
	if err != nil {
		// Missing file — return defaults and persist them
		s := Defaults()
		_ = Save(s)
		return s, nil
	}
	var s Settings
	if err := json.Unmarshal(data, &s); err != nil {
		return Defaults(), nil
	}
	return &s, nil
}

func Save(s *Settings) error {
	p := configPath()
	_ = os.MkdirAll(filepath.Dir(p), 0755)
	data, err := json.MarshalIndent(s, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(p, data, 0644)
}
