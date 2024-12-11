using NonLinearEquationSolve.GUI;

namespace NonLinearEquationSolve;

public class Program {
    private Renderer       gui;
    private ContextManager ctx;

    public static void Main() { // Entry point
        new Program().Start().Wait();
    }

    private async Task Start() {
        ctx = new ContextManager();

        gui = new Renderer(ctx);

        await gui.Start();
    }
}