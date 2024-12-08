using CS_IMGUI.GUI;

namespace CS_IMGUI {
	public class Program {
		private Renderer gui;
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
}