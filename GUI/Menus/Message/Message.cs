using ImGuiNET;

namespace NonLinearEquationSolve.GUI.Menus.Message;

public abstract class Message : Menu {
    public override int    priority => -1;
    protected       string _message;
    public          bool   acknowledged { get; protected set; } = false;

    protected readonly ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar |
                                                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

    public Message(string message) : base(null) {
        _message = message;
    }

    public override float GetMenuWidth() {
        return 300;
    }
}