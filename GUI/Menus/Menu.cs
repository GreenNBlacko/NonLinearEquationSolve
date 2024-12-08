using CS_IMGUI.GUI.Menus.Message;
using ImGuiNET;
using System.Numerics;

namespace CS_IMGUI.GUI.Menus {
	public abstract class Menu {
		public virtual int priority => 0;

		protected ContextManager ctx { get; private set; }

		public Menu(ContextManager _ctx) {
			ctx = _ctx;

			GUI.ctx = ctx;
		}

		public abstract void Render();

		public virtual float GetMenuHeight() {
			return 0;
		}

		public virtual float GetMenuWidth() {
			return 500;
		}


		protected static class GUI {
			public static ContextManager ctx;

			public static void FoldoutHeader(string label, ref bool foldout, Action content) {
				ImGui.SetNextItemOpen(foldout);
				foldout = ImGui.CollapsingHeader(label);

				if (foldout) {
					ImGui.Indent();
					content.Invoke();
					ImGui.Unindent();
				}
			}

			public static void CenteredWrappedText(string text, float padding = 0) {
				float contentWidth = ImGui.GetContentRegionAvail().X - padding * 2;

				ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + contentWidth);

				string[] lines = SplitTextIntoWrappedLines(text, contentWidth);

				foreach (var line in lines) {
					Vector2 textSize = ImGui.CalcTextSize(line);

					float offsetX = (contentWidth - textSize.X) / 2;

					if (offsetX > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

					ImGui.Text(line);
				}

				ImGui.PopTextWrapPos();
			}

			public static void Table(string name, List<string> columns, Action tableContents, ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchSame) {
				Vector2 originalSpacing = ImGui.GetStyle().CellPadding;
				Vector2 newSpacing = new Vector2(originalSpacing.X, originalSpacing.Y + 3);

				if (ImGui.BeginTable(name, columns.Count, flags)) {
					ImGui.TableNextRow();

					foreach (var column in columns) {
						ImGui.TableNextColumn();
						ImGui.Text(column);
					}

					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);

					var drawList = ImGui.GetWindowDrawList();
					var start = ImGui.GetCursorScreenPos();

					start.X -= 1;
					start.Y += originalSpacing.Y;

					ImGui.TableSetColumnIndex(columns.Count - 1);

					var end = new Vector2(ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X + 1, start.Y + 1);
					drawList.AddRectFilled(start, end, ImGui.GetColorU32(ImGuiCol.Separator));

					ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, newSpacing);

					tableContents.Invoke();

					ImGui.EndTable();
				}

				ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, originalSpacing);
			}

			public static void SpaceX(float width) {
				ImGui.Dummy(new Vector2(width, 0));
			}

			public static void SpaceY(float height) {
				ImGui.Dummy(new Vector2(0, height));
			}

			public static bool FullWidthButton(string label) {
				return ImGui.Button(label, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.CalcTextSize("L").Y * 1.5f));
			}

			public static int? ButtonList(ICollection<string> labels) {
				List<string> list = [.. labels];

				ImGui.BeginTable("Buttons", labels.Count, ImGuiTableFlags.SizingStretchSame);
				ImGui.TableNextRow();
				for (int i = 0; i < labels.Count; i++) {
					ImGui.TableNextColumn();
					ImGui.SetNextItemWidth(-1);
					if (FullWidthButton(list[i]))
						return i;
				}
				ImGui.EndTable();

				return null;
			}

			public static void ThrowError(string e_message) {
				ctx.renderer.message = new Error(e_message);
			}

			public static void ShowInfo(string i_message) {
				ctx.renderer.message = new Info(i_message);
			}

			public static void ShowDialog(string i_message, ICollection<string> options, Action<int> callback) {
				ctx.renderer.message = new Dialog(i_message, options, callback);
			}

			private static string[] SplitTextIntoWrappedLines(string text, float wrapWidth) {
				List<string> wrappedLines = new List<string>();

				string[] paragraphs = text.Split('\n');

				foreach (var paragraph in paragraphs) {
					string[] words = paragraph.Split(' ');

					string currentLine = "";
					foreach (var word in words) {
						string testLine = (currentLine.Length > 0 ? currentLine + " " : "") + word;
						Vector2 testSize = ImGui.CalcTextSize(testLine);

						if (testSize.X > wrapWidth) {
							if (!string.IsNullOrEmpty(currentLine))
								wrappedLines.Add(currentLine);

							currentLine = word;
						} else {
							// Add the word to the current line.
							currentLine = testLine;
						}
					}

					if (!string.IsNullOrEmpty(currentLine))
						wrappedLines.Add(currentLine);
				}

				return wrappedLines.ToArray();
			}
		}
	}
}
