# elara-macro

A lightweight Windows 11 macro recorder/player — single portable `.exe`, no installer.

**Tech:** C# .NET 8 · WinForms · Win32 low-level hooks · `SendInput`

## Project layout

```
elara-macro/
├── build.ps1              # Build / publish script
└── ElaraMacro/
    ├── ElaraMacro.csproj
    ├── Program.cs
    ├── Models/            # AppSettings, Macro, RecordedEvent, enums
    ├── Native/            # P/Invoke declarations (NativeMethods.cs)
    ├── Services/          # HookManager, RecorderService, PlayerService,
    │                      #   InputSimulatorService, StorageService,
    │                      #   TrayApplicationContext
    └── UI/                # MainForm, PromptDialog, KeyCaptureForm
```

See [ElaraMacro/README.md](ElaraMacro/README.md) for full feature docs and build instructions.
