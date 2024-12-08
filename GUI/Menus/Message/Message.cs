namespace CS_IMGUI.GUI.Menus.Message {
	public abstract class Message : Menu {
		public override int priority => -1;
		protected string _message;
		public bool acknowledged { get; protected set; } = false;

		public Message(string message) : base(null) {
			_message = message;
		}

		public override float GetMenuWidth() {
			return 300;
		}
	}
}
