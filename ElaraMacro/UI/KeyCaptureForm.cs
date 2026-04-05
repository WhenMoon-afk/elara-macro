namespace ElaraMacro.UI;

public sealed class KeyCaptureForm : Form
{
    public Keys? CapturedKey { get; private set; }

    public KeyCaptureForm()
    {
        Text = "Press a key";
        Width = 260;
        Height = 110;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Press the key you want to bind.",
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey)
            return;
        CapturedKey = e.KeyCode;
        DialogResult = DialogResult.OK;
        Close();
    }
}
