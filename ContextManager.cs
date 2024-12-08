using CS_IMGUI.GUI;

namespace CS_IMGUI {
	public class ContextManager {
		public Renderer renderer { get; private set; }

		public void SetRenderer(Renderer _renderer) {
			renderer = _renderer;
		}
	}
}
