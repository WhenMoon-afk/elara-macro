using ElaraMacro.Models;
using ElaraMacro.UI;

namespace ElaraMacro.Services;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly StorageService _storage = new();
    private readonly HookManager _hooks = new();
    private readonly RecorderService _recorder = new();
    private readonly PlayerService _player = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly MainForm _form;

    private AppSettings _settings;
    private List<Macro> _macros;
    private Guid? _selectedMacroId;
    private Macro? _workingMacro;
    private AppState _state = AppState.Idle;
    private string _status = "Idle";

    public TrayApplicationContext()
    {
        _settings = _storage.LoadSettings();
        _macros = _storage.LoadMacros();
        _selectedMacroId = _macros.FirstOrDefault()?.Id;

        _form = new MainForm(this);
        _form.Hide();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / Hide", null, (_, _) => ToggleWindow());
        menu.Items.Add("Quit", null, (_, _) => ExitApp());

        _notifyIcon = new NotifyIcon
        {
            Text = "Elara Macro",
            Visible = true,
            ContextMenuStrip = menu,
            Icon = SystemIcons.Application
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleWindow();

        _hooks.InputCaptured += OnInputCaptured;
        _player.StatusChanged += (state, text) => SetStatus(state, text);
        _player.PlaybackFinished += () => SetStatus(AppState.Idle, "Idle");
        _hooks.Start();
        RefreshUi();
    }

    public void ToggleWindow()
    {
        if (_form.Visible) _form.Hide();
        else { _form.Show(); _form.Activate(); }
    }

    public void StartOrStopRecording()
    {
        if (_state == AppState.Recording) { StopRecordingCore(); return; }
        if (_state == AppState.Playing || _state == AppState.Paused) return;
        _workingMacro = null;
        _recorder.Start();
        SetStatus(AppState.Recording, "Recording - capturing global input");
    }

    public void StopPlaybackOrRecording()
    {
        if (_state == AppState.Recording) { StopRecordingCore(); return; }
        if (_state == AppState.Playing || _state == AppState.Paused) _player.Stop();
    }

    public void PlaySelected()
    {
        if (_state != AppState.Idle) return;
        var macro = CurrentMacro();
        if (macro is null || macro.Events.Count == 0) return;
        _player.Start(macro, _settings);
    }

    public void PauseOrResume()
    {
        if (_state == AppState.Playing || _state == AppState.Paused) _player.TogglePause();
    }

    public void SaveCurrentRecordingAs(string name, IWin32Window owner)
    {
        if (_workingMacro is null || _workingMacro.Events.Count == 0)
        {
            MessageBox.Show(owner, "No unsaved recording is available.", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _workingMacro.Name = name.Trim();
        _workingMacro.UpdatedUtc = DateTime.UtcNow;
        _macros.RemoveAll(m => m.Id == _workingMacro.Id);
        _macros.Add(_workingMacro);
        _selectedMacroId = _workingMacro.Id;
        _workingMacro = null;
        _storage.SaveMacros(_macros);
        SetStatus(AppState.Idle, $"Saved '{name.Trim()}'");
        RefreshUi();
    }

    public void DeleteSelected(IWin32Window owner)
    {
        var macro = CurrentMacro();
        if (macro is null) return;
        if (MessageBox.Show(owner, $"Delete '{macro.Name}'?", "Elara Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _macros.RemoveAll(m => m.Id == macro.Id);
        if (_selectedMacroId == macro.Id) _selectedMacroId = _macros.FirstOrDefault()?.Id;
        _storage.SaveMacros(_macros);
        RefreshUi();
    }

    public void RenameSelected(IWin32Window owner)
    {
        var macro = CurrentMacro();
        if (macro is null) return;
        var name = PromptDialog.Show(owner, "Rename Macro", "New name:", macro.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        macro.Name = name.Trim();
        macro.UpdatedUtc = DateTime.UtcNow;
        _storage.SaveMacros(_macros);
        RefreshUi();
    }

    public void SelectMacro(Guid id, IWin32Window owner)
    {
        if (_workingMacro is not null && _state == AppState.Idle)
        {
            if (MessageBox.Show(owner, "You have an unsaved recording. Discard it?", "Elara Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            { RefreshUi(); return; }
            _workingMacro = null;
        }
        _selectedMacroId = id;
        RefreshUi();
    }

    public void RebindHotkey(string action, Keys key, IWin32Window owner)
    {
        var values = new Dictionary<string, Keys>
        {
            ["Record"] = _settings.RecordHotkey,
            ["Play"]   = _settings.PlayHotkey,
            ["Pause"]  = _settings.PauseHotkey,
            ["Stop"]   = _settings.StopHotkey
        };
        values[action] = key;
        if (values.Values.Distinct().Count() != values.Count)
        { MessageBox.Show(owner, "Hotkeys must be unique.", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        switch (action)
        {
            case "Record": _settings.RecordHotkey = key; break;
            case "Play":   _settings.PlayHotkey   = key; break;
            case "Pause":  _settings.PauseHotkey  = key; break;
            case "Stop":   _settings.StopHotkey   = key; break;
        }
        _storage.SaveSettings(_settings);
        RefreshUi();
    }

    public void UpdateSettings(Action<AppSettings> update)
    {
        update(_settings);
        _storage.SaveSettings(_settings);
        RefreshUi();
    }

    private void StopRecordingCore()
    {
        var events = _recorder.Stop();
        _workingMacro = new Macro { Name = "* New Recording", Events = events, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
        SetStatus(AppState.Idle, $"Recorded {events.Count} events");
        RefreshUi();
    }

    private Macro? CurrentMacro()
    {
        if (_workingMacro is not null) return _workingMacro;
        return _macros.FirstOrDefault(m => m.Id == _selectedMacroId);
    }

    private void OnInputCaptured(object? sender, RecordedEvent e)
    {
        if (e.Kind == EventKind.KeyDown)
        {
            if (e.KeyCode == _settings.RecordHotkey) { StartOrStopRecording(); return; }
            if (e.KeyCode == _settings.PlayHotkey)   { PlaySelected();          return; }
            if (e.KeyCode == _settings.PauseHotkey)  { PauseOrResume();         return; }
            if (e.KeyCode == _settings.StopHotkey)   { StopPlaybackOrRecording(); return; }
        }
        if (_state == AppState.Recording) _recorder.Process(e, _settings);
    }

    private void SetStatus(AppState state, string text)
    {
        _state = state; _status = text;
        if (_form.IsHandleCreated)
            _form.BeginInvoke(RefreshUi);
        else
            RefreshUi();
    }

    private void RefreshUi()
    {
        var viewMacros = _macros.OrderBy(m => m.Name).ToList();
        if (_workingMacro is not null) viewMacros.Insert(0, _workingMacro);
        var selectedId = _workingMacro?.Id ?? _selectedMacroId;
        _form.BindState(_settings, viewMacros, selectedId, _status);
    }

    private void ExitApp()
    {
        _player.Stop();
        _hooks.Stop();
        _storage.SaveSettings(_settings);
        _storage.SaveMacros(_macros);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _form.PrepareForExit();
        _form.Close();
        ExitThread();
    }
}
