namespace ElaraMacro.UI;

public sealed class PromptDialog : Form
{
    private readonly TextBox _textBox = new() { Dock = DockStyle.Top };
    public string Value => _textBox.Text.Trim();

    private PromptDialog(string title, string label, string initial)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        Width = 320; Height = 150;

        var lbl = new Label { Text = label, Dock = DockStyle.Top, Height = 24 };
        _textBox.Text = initial;

        var ok     = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        Controls.Add(buttons);
        Controls.Add(_textBox);
        Controls.Add(lbl);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public static string? Show(IWin32Window owner, string title, string label, string initial = "")
    {
        using var dlg = new PromptDialog(title, label, initial);
        return dlg.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.Value) ? dlg.Value : null;
    }
}
