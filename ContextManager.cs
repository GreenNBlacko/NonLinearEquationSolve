using NonLinearEquationSolve.GUI;
using Veldrid.Sdl2;

namespace NonLinearEquationSolve;

public class ContextManager {
    public Renderer renderer { get; private set; }
    public Sdl2Window window { get; private set; }

    public void SetRenderer(Renderer _renderer) {
        renderer = _renderer;
    }

    public void SetWindow(Sdl2Window _window)
    {
        window = _window;
    }
}