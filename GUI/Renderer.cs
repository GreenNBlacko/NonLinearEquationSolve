using System.Numerics;
using System.Reflection;
using ClickableTransparentOverlay;
using ImGuiNET;
using NonLinearEquationSolve.GUI.Menus;
using NonLinearEquationSolve.GUI.Menus.Message;

namespace NonLinearEquationSolve.GUI;

public class Renderer : Overlay {
    #region Theme

    private static void SetTheme() {
        var colors = ImGui.GetStyle().Colors;

        colors[(int)ImGuiCol.WindowBg]  = new Vector4(0.1f,  0.1f,  0.13f, 1.0f);
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

        // Border
        colors[(int)ImGuiCol.Border]       = new Vector4(0.44f, 0.37f, 0.61f, 0.29f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f,  0.0f,  0.0f,  0.24f);

        // Text
        colors[(int)ImGuiCol.Text]         = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        // Headers
        colors[(int)ImGuiCol.Header]        = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.19f, 0.2f,  0.25f, 1.0f);
        colors[(int)ImGuiCol.HeaderActive]  = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

        // Buttons
        colors[(int)ImGuiCol.Button]        = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.19f, 0.2f,  0.25f, 1.0f);
        colors[(int)ImGuiCol.ButtonActive]  = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.CheckMark]     = new Vector4(0.74f, 0.58f, 0.98f, 1.0f);

        // Popups
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.1f, 0.1f, 0.13f, 0.92f);

        // Slider
        colors[(int)ImGuiCol.SliderGrab]       = new Vector4(0.44f, 0.37f, 0.61f, 0.54f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.74f, 0.58f, 0.98f, 0.54f);

        // Frame BG
        colors[(int)ImGuiCol.FrameBg]        = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.19f, 0.2f,  0.25f, 1.0f);
        colors[(int)ImGuiCol.FrameBgActive]  = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

        // Tabs
        colors[(int)ImGuiCol.Tab]                = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.TabHovered]         = new Vector4(0.24f, 0.24f, 0.32f, 1.0f);
        colors[(int)ImGuiCol.TabActive]          = new Vector4(0.2f,  0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.TabUnfocused]       = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

        // Title
        colors[(int)ImGuiCol.TitleBg]          = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.TitleBgActive]    = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg]          = new Vector4(0.1f,  0.1f,  0.13f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrab]        = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.19f, 0.2f,  0.25f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabActive]  = new Vector4(0.24f, 0.24f, 0.32f, 1.0f);

        // Seperator
        colors[(int)ImGuiCol.Separator]        = new Vector4(0.44f, 0.37f, 0.61f, 1.0f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.74f, 0.58f, 0.98f, 1.0f);
        colors[(int)ImGuiCol.SeparatorActive]  = new Vector4(0.84f, 0.58f, 1.0f,  1.0f);

        // Resize Grip
        colors[(int)ImGuiCol.ResizeGrip]        = new Vector4(0.44f, 0.37f, 0.61f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.74f, 0.58f, 0.98f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripActive]  = new Vector4(0.84f, 0.58f, 1.0f,  0.29f);

        // Docking
        //colors[(int)ImGuiCol.DockingPreview] = new Vector4{ 0.44f, 0.37f, 0.61f, 1.0f };

        var style = ImGui.GetStyle();
        style.TabRounding       = 4;
        style.ScrollbarRounding = 9;
        style.WindowRounding    = 7;
        style.GrabRounding      = 3;
        style.FrameRounding     = 3;
        style.PopupRounding     = 4;
        style.ChildRounding     = 4;
    }

    #endregion

    public enum e_Menus {
        Main
    }

    public Message? message;

    private Menu menu;

    private List<Type> menus = new();

    private ContextManager ctx;

    public Renderer(ContextManager _ctx) {
        ctx = _ctx;
        ctx.SetRenderer(this);
    }

    protected override Task PostInitialized() {
        // Set up menus
        CrawlMenus();

        SetTheme();

        LoadMenu(0);

        message = null;

        return base.PostInitialized();
    }

    protected override void Render() {
        if (message != null) {
            menu.GetMenuHeight();
            ImGui.SetNextWindowSize(new Vector2(message.GetMenuWidth(), message.GetMenuHeight()));
            message.Render();

            if (message.acknowledged)
                message = null;
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(menu.GetMenuWidth(), menu.GetMenuHeight()));
        ImGui.Begin("Non-linear equation solver");

        ImGui.SetWindowFontScale(1.3f);

        menu.Render();

        ImGui.End();
    }

    public void LoadMenu(e_Menus menuID, params object?[] args) {
        if (args.Length > 0) {
            menu = Activator.CreateInstance(menus[(int)menuID], ctx, args) as Menu ??
                   throw new ArgumentNullException("Menu does not exist");
            return;
        }

        menu = Activator.CreateInstance(menus[(int)menuID], ctx) as Menu ??
               throw new ArgumentNullException("Menu does not exist");
    }

    private void CrawlMenus() {
        menus.Clear();

        var assembly = Assembly.GetExecutingAssembly();

        var menuTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Menu)) && !t.IsAbstract);

        foreach (var menuType in menuTypes)
            try {
                var instance = Activator.CreateInstance(menuType, ctx) as Menu;
                if (instance != null && instance.priority >= 0) menus.Add(menuType);
            }
            catch { }

        menus.Sort((a, b) => {
            return (Activator.CreateInstance(a, ctx) as Menu).priority >
                   (Activator.CreateInstance(b, ctx) as Menu).priority
                       ? 1
                       : -1;
        });
    }
}