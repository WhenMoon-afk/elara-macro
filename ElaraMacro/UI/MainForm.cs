using ElaraMacro.Models;
using ElaraMacro.Services;

namespace ElaraMacro.UI;

public sealed class MainForm : Form
{
    private readonly TrayApplicationContext _app;
    private readonly Label _status     = new() { AutoSize = true, Text = "Idle" };
    private readonly ComboBox _macroBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
    private readonly Button _record    = new() { Text = "Record",  Width = 70 };
    private readonly Button _play      = new() { Text = "Play",    Width = 70 };
    private readonly Button _pause     = new() { Text = "Pause",   Width = 70 };
    private readonly Button _stop      = new() { Text = "Stop",    Width = 70 };
    private readonly Button _save      = new() { Text = "Save As", Width = 70 };
    private readonly Button _rename    = new() { Text = "Rename",  Width = 70 };
    private readonly Button _delete    = new() { Text = "Delete",  Width = 70 };
    private readonly NumericUpDown _loops     = new() { Minimum = 0,  Maximum = 9999, Width = 70 };
    private readonly NumericUpDown _delay     = new() { Minimum = 1,  Maximum = 5000, Width = 70 };
    private readonly NumericUpDown _threshold = new() { Minimum = 1,  Maximum = 100,  Width = 70 };
    private readonly CheckBox _normalize = new() { Text = "Normalize timing" };
    private readonly CheckBox _topMost   = new() { Text = "Always on top" };
    private readonly LinkLabel _recordKey = new() { AutoSize = true };
    private readonly LinkLabel _playKey   = new() { AutoSize = true };
    private readonly LinkLabel _pauseKey  = new() { AutoSize = true };
    private readonly LinkLabel _stopKey   = new() { AutoSize = true };
    private bool _closingForExit;

    public MainForm(TrayApplicationContext app)
    {
        _app = app;
        Text = "Elara Macro";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        Width = 330; Height = 280;
        ShowInTaskbar = false;
        MaximizeBox = false; MinimizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(8),
            ColumnCount = 2, RowCount = 8, AutoSize = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        var topRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        topRow.Controls.Add(_status);

        var macroRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        macroRow.Controls.Add(_macroBox);
        macroRow.Controls.Add(_save);

        var buttons1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons1.Controls.AddRange(new Control[] { _record, _play, _pause, _stop });

        var buttons2 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons2.Controls.AddRange(new Control[] { _rename, _delete });

        var hotkeys = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false
        };
        hotkeys.Controls.AddRange(new Control[] { _recordKey, _playKey, _pauseKey, _stopKey });

        root.Controls.Add(new Label { Text = "Status",        AutoSize = true }, 0, 0); root.Controls.Add(topRow,    1, 0);
        root.Controls.Add(new Label { Text = "Macro",         AutoSize = true }, 0, 1); root.Controls.Add(macroRow,  1, 1);
        root.Controls.Add(new Label { Text = "Controls",      AutoSize = true }, 0, 2); root.Controls.Add(buttons1,  1, 2);
        root.Controls.Add(new Label { Text = "Library",       AutoSize = true }, 0, 3); root.Controls.Add(buttons2,  1, 3);
        root.Controls.Add(new Label { Text = "Loop count",    AutoSize = true }, 0, 4); root.Controls.Add(_loops,    1, 4);
        root.Controls.Add(_normalize, 0, 5);                                            root.Controls.Add(_delay,    1, 5);
        root.Controls.Add(new Label { Text = "Mouse move px", AutoSize = true }, 0, 6); root.Controls.Add(_threshold,1, 6);
        root.Controls.Add(_topMost,   0, 7);                                            root.Controls.Add(hotkeys,   1, 7);
        Controls.Add(root);

        _record.Click  += (_, _) => _app.StartOrStopRecording();
        _play.Click    += (_, _) => _app.PlaySelected();
        _pause.Click   += (_, _) => _app.PauseOrResume();
        _stop.Click    += (_, _) => _app.StopPlaybackOrRecording();
        _save.Click    += (_, _) => SaveCurrent();
        _delete.Click  += (_, _) => _app.DeleteSelected(this);
        _rename.Click  += (_, _) => _app.RenameSelected(this);
        _macroBox.SelectedIndexChanged += (_, _) =>
        {
            if (_macroBox.SelectedItem is Macro m) _app.SelectMacro(m.Id, this);
        };
        _loops.ValueChanged     += (_, _) => _app.UpdateSettings(s => s.LoopCount            = (int)_loops.Value);
        _delay.ValueChanged     += (_, _) => _app.UpdateSettings(s => s.NormalizedDelayMs    = (int)_delay.Value);
        _threshold.ValueChanged += (_, _) => _app.UpdateSettings(s => s.MouseMoveThresholdPx = (int)_threshold.Value);
        _normalize.CheckedChanged += (_, _) =>
        {
            _delay.Enabled = _normalize.Checked;
            _app.UpdateSettings(s => s.NormalizeTiming = _normalize.Checked);
        };
        _topMost.CheckedChanged += (_, _) =>
        {
            TopMost = _topMost.Checked;
            _app.UpdateSettings(s => s.AlwaysOnTop = _topMost.Checked);
        };
        _recordKey.LinkClicked += (_, _) => BindHotkey("Record");
        _playKey.LinkClicked   += (_, _) => BindHotkey("Play");
        _pauseKey.LinkClicked  += (_, _) => BindHotkey("Pause");
        _stopKey.LinkClicked   += (_, _) => BindHotkey("Stop");
        Move += (_, _) => _app.UpdateSettings(s => { s.WindowX = Left; s.WindowY = Top; });
    }

    public void BindState(AppSettings settings, IReadOnlyList<Macro> macros, Guid? selectedId, string status)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => BindState(settings, macros, selectedId, status))); return; }

        _status.Text     = status;
        _loops.Value     = Math.Max(_loops.Minimum,     Math.Min(_loops.Maximum,     settings.LoopCount));
        _delay.Value     = Math.Max(_delay.Minimum,     Math.Min(_delay.Maximum,     settings.NormalizedDelayMs));
        _threshold.Value = Math.Max(_threshold.Minimum, Math.Min(_threshold.Maximum, settings.MouseMoveThresholdPx));
        _normalize.Checked = settings.NormalizeTiming;
        _delay.Enabled     = settings.NormalizeTiming;
        _topMost.Checked   = settings.AlwaysOnTop;
        TopMost            = settings.AlwaysOnTop;
        Location           = new Point(settings.WindowX, settings.WindowY);

        _recordKey.Text = $"Record: {settings.RecordHotkey}";
        _playKey.Text   = $"Play: {settings.PlayHotkey}";
        _pauseKey.Text  = $"Pause: {settings.PauseHotkey}";
        _stopKey.Text   = $"Stop: {settings.StopHotkey}";

        _macroBox.BeginUpdate();
        _macroBox.DataSource = null;
        _macroBox.DataSource = macros.ToList();
        if (selectedId is Guid id)
        {
            var match = macros.FirstOrDefault(x => x.Id == id);
            if (match is not null) _macroBox.SelectedItem = match;
        }
        _macroBox.EndUpdate();
    }

    public void PrepareForExit() => _closingForExit = true;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_closingForExit) { e.Cancel = true; Hide(); return; }
        base.OnFormClosing(e);
    }

    private void SaveCurrent()
    {
        var name = PromptDialog.Show(this, "Save Macro", "Macro name:");
        if (!string.IsNullOrWhiteSpace(name)) _app.SaveCurrentRecordingAs(name, this);
    }

    private void BindHotkey(string action)
    {
        using var dlg = new KeyCaptureForm();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.CapturedKey is Keys key)
            _app.RebindHotkey(action, key, this);
    }
}
