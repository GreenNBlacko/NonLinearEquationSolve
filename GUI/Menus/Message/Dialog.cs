using ImGuiNET;

namespace NonLinearEquationSolve.GUI.Menus.Message;

public class Dialog : Message {
    private List<string> _options;
    private Action<int>  _callback;

    public Dialog(string message, ICollection<string> options, Action<int> callback) : base(message) {
        _options  = [.. options];
        _callback = callback;
    }

    public override void Render() {
        ImGui.Begin("Notice", flags);
        
        GUI.ctx.window.Title = "Notice";

        ImGui.SetWindowFontScale(1.3f);

        GUI.CenteredWrappedText(_message, 5);

        GUI.SpaceY(5);

        ImGui.Separator();
        var selection = GUI.ButtonList(_options);

        if (selection.HasValue) {
            _callback.Invoke(selection.Value);
            acknowledged = true;
        }

        ImGui.End();
    }
}