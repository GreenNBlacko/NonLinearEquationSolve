using NonLinearEquationSolve.GUI;

namespace NonLinearEquationSolve;

public class ContextManager {
    public Renderer renderer { get; private set; }

    public void SetRenderer(Renderer _renderer) {
        renderer = _renderer;
    }
}