using ElaraMacro.Models;
using ElaraMacro.Native;
using ElaraMacro.UI;

namespace ElaraMacro.Services;

public sealed class TrayApplicationContext : ApplicationContext
{
    private const int HotkeyRecordId = 1001;
    private const int HotkeyPlayId = 1002;
    private const int HotkeyPauseId = 1003;
    private const int HotkeyStopId = 1004;

    private readonly object _stateGate = new();
    private readonly StorageService _storage;
    private readonly HookManager _hookManager;
    private readonly RecorderService _recorder;
    private readonly InputSimulatorService _inputSimulator;
    private readonly PlayerService _player;
    private readonly NotifyIcon _notifyIcon;
    private readonly MainForm _mainForm;
    private readonly HotkeyWindow _hotkeyWindow;

    private AppSettings _settings;
    private List<Macro> _macros;
    private Guid? _selectedId;
    private Macro? _workingMacro;
    private AppState _state = AppState.Idle;
    private string _statusText = "Idle";
    private CancellationTokenSource? _playbackCts;

    public TrayApplicationContext()
    {
        _storage = new StorageService();
        _hookManager = new HookManager();
        _settings = _storage.LoadSettings();
        _macros = _storage.LoadMacros();
        _selectedId = _macros.FirstOrDefault()?.Id;

        _recorder = new RecorderService(_hookManager, () => _settings);
        _inputSimulator = new InputSimulatorService();
        _player = new PlayerService(_inputSimulator);

        _mainForm = new MainForm(this);
        _mainForm.Hide();

        _hotkeyWindow = new HotkeyWindow(this);
        RegisterAllHotkeys();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / Hide", null, (_, _) => ToggleWindow());
        menu.Items.Add("Quit", null, (_, _) => Quit());

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Elara Macro",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowWindow();

        _hookManager.Start();
        RefreshUi();
    }

    public void StartOrStopRecording()
    {
        var shouldRefresh = false;

        lock (_stateGate)
        {
            if (_state == AppState.Recording)
            {
                StopRecordingInternal();
                shouldRefresh = true;
            }
            else if (_state == AppState.Idle)
            {
                _workingMacro = null;
                _recorder.Start();
                SetStateStatus(AppState.Recording, "Recording...");
                shouldRefresh = true;
            }
        }

        if (shouldRefresh)
        {
            RefreshUi();
        }
    }

    public void PlaySelected()
    {
        Macro? macro = null;
        CancellationToken token = default;

        lock (_stateGate)
        {
            if (_state != AppState.Idle)
            {
                return;
            }

            macro = GetCurrentMacro();
            if (macro is null || macro.Events.Count == 0)
            {
                return;
            }

            _playbackCts?.Cancel();
            _playbackCts?.Dispose();
            _playbackCts = new CancellationTokenSource();
            token = _playbackCts.Token;
            SetStateStatus(AppState.Playing, "Playing...");
        }

        RefreshUi();
        _ = RunPlaybackAsync(macro.Events, token);
    }

    public void PauseOrResume()
    {
        bool pause;
        bool resume;

        lock (_stateGate)
        {
            pause = _state == AppState.Playing;
            resume = _state == AppState.Paused;
            if (pause) SetStateStatus(AppState.Paused, "Paused");
            else if (resume) SetStateStatus(AppState.Playing, "Playing...");
        }

        if (pause) _player.Pause();
        else if (resume) _player.Resume();

        if (pause || resume)
        {
            RefreshUi();
        }
    }

    public void StopPlaybackOrRecording()
    {
        bool shouldResume;
        bool shouldRefresh = false;

        lock (_stateGate)
        {
            if (_state == AppState.Recording)
            {
                StopRecordingInternal();
                shouldRefresh = true;
                shouldResume = false;
            }
            else
            {
                shouldResume = _state is AppState.Playing or AppState.Paused;
                if (shouldResume)
                {
                    _playbackCts?.Cancel();
                    SetStateStatus(AppState.Idle, "Idle");
                    shouldRefresh = true;
                }
            }
        }

        if (shouldResume)
        {
            _player.Resume();
        }

        if (shouldRefresh)
        {
            RefreshUi();
        }
    }

    public void SaveCurrentRecordingAs(string name, IWin32Window owner)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        lock (_stateGate)
        {
            if (_workingMacro is null || _workingMacro.Events.Count == 0)
            {
                MessageBox.Show(owner, "No unsaved recording is available.", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _workingMacro.Name = trimmed;
            _workingMacro.UpdatedUtc = DateTime.UtcNow;
            _macros.RemoveAll(m => m.Id == _workingMacro.Id);
            _macros.Add(_workingMacro);
            _selectedId = _workingMacro.Id;
            _workingMacro = null;
            _storage.SaveMacros(_macros);
            SetStateStatus(AppState.Idle, $"Saved '{trimmed}'");
        }

        RefreshUi();
    }

    public void DeleteSelected(IWin32Window owner)
    {
        lock (_stateGate)
        {
            var selected = _macros.FirstOrDefault(m => m.Id == _selectedId);
            if (selected is null)
            {
                return;
            }

            var result = MessageBox.Show(owner, $"Delete '{selected.Name}'?", "Elara Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            _macros.RemoveAll(m => m.Id == selected.Id);
            if (_selectedId == selected.Id)
            {
                _selectedId = _macros.FirstOrDefault()?.Id;
            }

            _storage.SaveMacros(_macros);
        }

        RefreshUi();
    }

    public void RenameSelected(IWin32Window owner)
    {
        Macro? selected;
        lock (_stateGate)
        {
            selected = _macros.FirstOrDefault(m => m.Id == _selectedId);
        }

        if (selected is null)
        {
            return;
        }

        var renamed = PromptDialog.Show(owner, "Rename Macro", "New name:", selected.Name);
        if (string.IsNullOrWhiteSpace(renamed))
        {
            return;
        }

        lock (_stateGate)
        {
            selected.Name = renamed.Trim();
            selected.UpdatedUtc = DateTime.UtcNow;
            _storage.SaveMacros(_macros);
        }

        RefreshUi();
    }

    public void SelectMacro(Guid id, IWin32Window owner)
    {
        var shouldRefresh = false;

        lock (_stateGate)
        {
            if (_workingMacro is not null && _state == AppState.Idle)
            {
                var result = MessageBox.Show(owner, "You have an unsaved recording. Discard it?", "Elara Macro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    shouldRefresh = true;
                }
                else
                {
                    _workingMacro = null;
                    _selectedId = id;
                    shouldRefresh = true;
                }
            }
            else
            {
                _selectedId = id;
                shouldRefresh = true;
            }
        }

        if (shouldRefresh)
        {
            RefreshUi();
        }
    }

    public void UpdateSettings(Action<AppSettings> mutate)
    {
        lock (_stateGate)
        {
            mutate(_settings);
            _storage.SaveSettings(_settings);
        }

        RefreshUi();
    }

    public void RebindHotkey(string action, Keys key, IWin32Window owner)
    {
        lock (_stateGate)
        {
            var map = new Dictionary<string, Keys>
            {
                ["Record"] = _settings.RecordHotkey,
                ["Play"] = _settings.PlayHotkey,
                ["Pause"] = _settings.PauseHotkey,
                ["Stop"] = _settings.StopHotkey
            };

            if (!map.ContainsKey(action))
            {
                return;
            }

            map[action] = key;
            if (map.Values.Distinct().Count() != map.Count)
            {
                MessageBox.Show(owner, "Hotkeys must be unique.", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.RecordHotkey = map["Record"];
            _settings.PlayHotkey = map["Play"];
            _settings.PauseHotkey = map["Pause"];
            _settings.StopHotkey = map["Stop"];

            _storage.SaveSettings(_settings);
            RegisterAllHotkeys();
        }

        RefreshUi();
    }

    internal void HandleHotkey(int id)
    {
        switch (id)
        {
            case HotkeyRecordId:
                StartOrStopRecording();
                break;
            case HotkeyPlayId:
                PlaySelected();
                break;
            case HotkeyPauseId:
                PauseOrResume();
                break;
            case HotkeyStopId:
                StopPlaybackOrRecording();
                break;
        }
    }

    private async Task RunPlaybackAsync(List<RecordedEvent> events, CancellationToken ct)
    {
        try
        {
            await _player.PlayAsync(events, _settings, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during stop.
        }
        finally
        {
            lock (_stateGate)
            {
                SetStateStatus(AppState.Idle, "Idle");
            }

            RefreshUi();
        }
    }

    private void StopRecordingInternal()
    {
        var events = _recorder.Stop();
        _workingMacro = new Macro
        {
            Name = "* New Recording",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            Events = events
        };

        SetStateStatus(AppState.Idle, $"Recorded {events.Count} events");
    }

    private Macro? GetCurrentMacro()
    {
        if (_workingMacro is not null)
        {
            return _workingMacro;
        }

        return _macros.FirstOrDefault(m => m.Id == _selectedId);
    }

    private void SetStateStatus(AppState state, string statusText)
    {
        _state = state;
        _statusText = statusText;
    }

    private void RefreshUi()
    {
        if (_mainForm.IsDisposed)
        {
            return;
        }

        void Bind()
        {
            var macros = _macros.OrderBy(m => m.Name).ToList();
            if (_workingMacro is not null)
            {
                macros.Insert(0, _workingMacro);
            }

            _mainForm.BindState(_settings, macros, _workingMacro?.Id ?? _selectedId, _statusText);
        }

        if (_mainForm.InvokeRequired)
        {
            _mainForm.BeginInvoke((Action)Bind);
            return;
        }

        Bind();
    }

    private void RegisterAllHotkeys()
    {
        var handle = _hotkeyWindow.Handle;
        NativeMethods.UnregisterHotKey(handle, HotkeyRecordId);
        NativeMethods.UnregisterHotKey(handle, HotkeyPlayId);
        NativeMethods.UnregisterHotKey(handle, HotkeyPauseId);
        NativeMethods.UnregisterHotKey(handle, HotkeyStopId);

        NativeMethods.RegisterHotKey(handle, HotkeyRecordId, 0, (uint)_settings.RecordHotkey);
        NativeMethods.RegisterHotKey(handle, HotkeyPlayId, 0, (uint)_settings.PlayHotkey);
        NativeMethods.RegisterHotKey(handle, HotkeyPauseId, 0, (uint)_settings.PauseHotkey);
        NativeMethods.RegisterHotKey(handle, HotkeyStopId, 0, (uint)_settings.StopHotkey);
    }

    private void ToggleWindow()
    {
        if (_mainForm.Visible)
        {
            _mainForm.Hide();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        _mainForm.Show();
        _mainForm.Activate();
    }

    private void Quit()
    {
        _playbackCts?.Cancel();
        _playbackCts?.Dispose();

        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyRecordId);
        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyPlayId);
        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyPauseId);
        NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyStopId);


        _storage.SaveSettings(_settings);
        _storage.SaveMacros(_macros);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        _mainForm.PrepareForExit();
        _mainForm.Close();

        _hotkeyWindow.DestroyHandle();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _hookManager.Dispose();
            _recorder.Dispose();
            _playbackCts?.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class HotkeyWindow : NativeWindow
    {
        private readonly TrayApplicationContext _owner;

        public HotkeyWindow(TrayApplicationContext owner)
        {
            _owner = owner;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                _owner.HandleHotkey(m.WParam.ToInt32());
                return;
            }

            base.WndProc(ref m);
        }
    }
}
