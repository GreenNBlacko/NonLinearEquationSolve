using CS_IMGUI;
using CS_IMGUI.GUI.Menus;
using ImGuiNET;
using MathNet.Numerics;
using MathNet.Symbolics;
using System.Drawing;
using System.Text;
using static CS_IMGUI.GUI.Renderer;

namespace NonLinearEquationSolve.GUI.Menus {
	public class Main(ContextManager ctx) : Menu(ctx) {
		public override int priority => (int)e_Menus.Main;

		string expr = "";
		double epsilon = 0.0001;
		int selection = 0;

		readonly string[] methods = ["Intervalo dalijimas pusiau", "kirstiniu", "liestiniu"];

		bool _resultExists = false;
		string _function;
		string _derivative;
		string _derivativeRoots = string.Empty;
		List<(double min, double max, bool flip)> intervals = new();
		List<string> _rootValues = new();
		List<char> _rootSigns = new();
		Action _RootsTable;

		List<string> _rootValues1 = new();
		List<char> _rootSigns1 = new();
		Action _RootsTable1;

		List<string> _rootValues2 = new();
		List<char> _rootSigns2 = new();
		Action _RootsTable2;

		List<string> _rootValues3 = new();
		List<char> _rootSigns3 = new();
		Action _RootsTable3;

		List<List<string>> _rootValuesList = new();
		List<List<char>> _rootSignsList = new();
		List<Action<int>> _RootsTables = new();

		List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> slices = new();
		List<(double root, List<(int iteration, double xn, double epsilon)> steps)> rf_iterations = new();
		List<(double root, List<(int iteration, double xn, double f_xn, double f_prime_xn)> steps)> n_iterations = new();
		List<Action<int>> EpsilonTables = new();

		public override void Render() {
			ImGui.InputText("Expression", ref expr, 256);
			ImGui.InputDouble("Epsilon", ref epsilon);

			if (ImGui.Combo("Method", ref selection, methods, methods.Length))
				_resultExists = false;

			if (GUI.FullWidthButton("Calculate")) {
				CalculateExpression();
			}

			if (_resultExists)
				RenderResults();
		}

		private void RenderResults() {
			ImGui.SeparatorText("Results");

			ImGui.Text($"f(x) = {_function}");
			ImGui.Text($"f'(x) = {_derivative}");
			ImGui.Text(_derivativeRoots);

			_RootsTable.Invoke();

			switch (selection) {
				case 0: // div by half
					_RootsTable1.Invoke();
					_RootsTable2.Invoke();
					_RootsTable3.Invoke();

					for (int i = 0; i < EpsilonTables.Count; i++) {
						Action<int>? table = EpsilonTables[i];
						table.Invoke(i);
					}
					break;

				case 1: // Regula falsi
					for (int i = 0; i < _RootsTables.Count; i++) {
						Action<int>? table = _RootsTables[i];
						table.Invoke(i);
					}

					for (int i = 0; i < EpsilonTables.Count; i++) {
						Action<int>? table = EpsilonTables[i];
						table.Invoke(i);
					}
					break;

				case 2: // Newton
					for (int i = 0; i < _RootsTables.Count; i++) {
						Action<int>? table = _RootsTables[i];
						table.Invoke(i);
					}

					for (int i = 0; i < EpsilonTables.Count; i++) {
						Action<int>? table = EpsilonTables[i];
						table.Invoke(i);
					}
					break;
			}
		}

		private void CalculateExpression() {
			try {
				var x = SymbolicExpression.Variable("x");
				var function = SymbolicExpression.Parse(expr);
				var solver = function.Compile("x");

				_function = function.ToString();

				var derivative = function.Differentiate(x);

				_derivative = derivative.ToString();

				var polynomial = derivative.Coefficients(x);

				List<double> coeffs = new();

				foreach (var coefficient in polynomial) {
					coeffs.Add(double.Parse(coefficient.ToString()));
				}

				var roots = FindRoots.Polynomial(coeffs.ToArray());

				intervals = new();

				_rootValues = new();
				_rootSigns = new();

				_rootValues.Add("-inf");
				_rootSigns.Add('-');

				_derivativeRoots = string.Empty;

				foreach (var root in roots) {
					_derivativeRoots += $"x = {root.Real}  or  ";
					_rootValues.Add(root.Real.ToString());
					_rootSigns.Add(solver.Invoke(root.Real) < 0 ? '-' : '+');
				}

				_rootValues.Add("inf");
				_rootSigns.Add('+');

				_derivativeRoots = _derivativeRoots.Substring(0, _derivativeRoots.Length - "  or  ".Length);

				_RootsTable = delegate { DisplayRootsTable(_rootValues, _rootSigns); };

				switch (selection) {
					case 0: // div by 2
						_rootValues1 = new();
						_rootValues2 = new();
						_rootValues3 = new();

						_rootSigns1 = new();
						_rootSigns2 = new();
						_rootSigns3 = new();

						var lowestVal = roots[0].Real - 1;
						var lowestSign = solver.Invoke(lowestVal) < 0 ? '-' : '+';
						_rootValues1.Add(lowestVal.ToString());
						_rootSigns1.Add(lowestSign);

						_rootValues1.Add(roots[0].Real.ToString());
						_rootSigns1.Add(_rootSigns[1]);

						_RootsTable1 = delegate { DisplayRootsTable(_rootValues1, _rootSigns1); };

						if (lowestSign != _rootSigns[1])
							intervals.Add((lowestVal, roots[0].Real, _rootSigns1[0] == '+'));

						var midVal = roots[0].Real + 1;
						var midSign = solver.Invoke(midVal) < 0 ? '-' : '+';

						_rootValues2.Add(roots[0].Real.ToString());
						_rootSigns2.Add(_rootSigns[1]);

						_rootValues2.Add(midVal.ToString());
						_rootSigns2.Add(midSign);

						_RootsTable2 = delegate { DisplayRootsTable(_rootValues2, _rootSigns2); };

						if (midSign != _rootSigns[1])
							intervals.Add((roots[0].Real, midVal, _rootSigns2[0] == '+'));

						var highVal = roots[1].Real + 1;
						var highSign = solver.Invoke(highVal) < 0 ? '-' : '+';

						_rootValues3.Add(roots[1].Real.ToString());
						_rootSigns3.Add(_rootSigns[2]);

						_rootValues3.Add(highVal.ToString());
						_rootSigns3.Add(highSign);

						_RootsTable3 = delegate { DisplayRootsTable(_rootValues3, _rootSigns3); };

						if (highSign != _rootSigns[2])
							intervals.Add((roots[1].Real, highVal, _rootSigns3[0] == '+'));

						slices = new();

						slices = Bisection(intervals, epsilon);

						EpsilonTables = new();

						for (int o = 0; o < slices.Count; o++) {
							EpsilonTables.Add((int idx) => {
								List<(double a, double b, double epsilon, double mid, double fMid, char sign)> slice = slices[idx];

								var lastSign = '*';
								GUI.Table("EpsilonTable", ["n", "a", "b", "epsilon", "mid", "f(xn)", "sign f(x)"], delegate {
									for (int i = 0; i < slice.Count; i++) {
										var row = slice[i];

										ImGui.TableNextRow();

										ImGui.TableNextColumn();
										ImGui.Text(i.ToString());

										ImGui.TableNextColumn();
										if (lastSign == '-')
											ImGui.TextColored(Color.Red.ToVector(), row.a.ToString());
										else
											ImGui.Text(row.a.ToString());

										ImGui.TableNextColumn();
										if (lastSign == '+')
											ImGui.TextColored(Color.Red.ToVector(), row.b.ToString());
										else
											ImGui.Text(row.b.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(row.epsilon.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(row.mid.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(row.fMid.ToString());

										ImGui.TableNextColumn();
										ImGui.TextColored(Color.Red.ToVector(), row.sign.ToString());

										if (i != slice.Count - 1)
											lastSign = row.sign;
									}
								}, ImGuiTableFlags.Borders);

								var answer = lastSign == '-' ? slice[slice.Count - 1].a : slice[slice.Count - 1].b;

								int precision = epsilon.ToString("0.################").Split('.')[1].Length;
								var sb = new StringBuilder();
								sb.Append("##0");
								sb.Append('.');
								sb.Append('0', precision);
								ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
							});
						}
						break;

					case 1: // Regula Falsi
						lowestVal = roots[0].Real - 1;
						highVal = roots[1].Real + 1;

						var rf_roots = FindRootIntervals(solver, lowestVal, highVal, 1);

						_rootSignsList.Clear();
						_rootValuesList.Clear();
						_RootsTables.Clear();
						rf_iterations = new();
						EpsilonTables = new();

						for (int i = 0; i < rf_roots.Count; i++) {
							var idx = int.Parse(i.ToString());
							var rf_root = rf_roots[idx];

							List<string> rf_intervals = new();
							List<char> rf_signs = new();

							rf_intervals.Add(rf_root.min.ToString());
							rf_intervals.Add(rf_root.max.ToString());

							rf_signs.Add(solver.Invoke(rf_root.min) < 0 ? '-' : '+');
							rf_signs.Add(solver.Invoke(rf_root.max) < 0 ? '-' : '+');

							_rootValuesList.Add(rf_intervals);
							_rootSignsList.Add(rf_signs);

							_RootsTables.Add((int index) => {
								var intervals = _rootValuesList[index];
								var signs = _rootSignsList[index];

								DisplayRootsTable(intervals, signs);
							});

							rf_iterations.Add(RegulaFalsi(solver, rf_root.min, rf_root.max, epsilon));

							EpsilonTables.Add((int index) => {
								var iteration = rf_iterations[index];

								GUI.Table("EpsilonTable", ["n", "xn", "epsilon"], delegate {
									for (int i = 0; i < iteration.steps.Count; i++) {
										var row = iteration.steps[i];

										ImGui.TableNextRow();

										ImGui.TableNextColumn();
										ImGui.Text(row.iteration.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(row.xn.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(i == 0 ? "-" : row.epsilon.ToString());
									}
								}, ImGuiTableFlags.Borders);
								var answer = iteration.root;

								int precision = epsilon.ToString("0.################").Split('.')[1].Length;
								var sb = new StringBuilder();
								sb.Append("##0");
								sb.Append('.');
								sb.Append('0', precision);
								ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
							});
						}
						break;

					case 2: // Newton
						lowestVal = roots[0].Real - 1;
						highVal = roots[1].Real + 1;

						var dsolver = derivative.Compile("x");

						var n_roots = FindRootIntervals(solver, lowestVal, highVal, 1);

						_rootSignsList.Clear();
						_rootValuesList.Clear();
						_RootsTables.Clear();
						n_iterations = new();
						EpsilonTables = new();

						for (int i = 0; i < n_roots.Count; i++) {
							var idx = int.Parse(i.ToString());
							var n_root = n_roots[idx];

							List<string> n_intervals = new();
							List<char> n_signs = new();

							n_intervals.Add(n_root.min.ToString());
							n_intervals.Add(n_root.max.ToString());

							n_signs.Add(solver.Invoke(n_root.min) < 0 ? '-' : '+');
							n_signs.Add(solver.Invoke(n_root.max) < 0 ? '-' : '+');

							_rootValuesList.Add(n_intervals);
							_rootSignsList.Add(n_signs);

							_RootsTables.Add((int index) => {
								var intervals = _rootValuesList[index];
								var signs = _rootSignsList[index];

								DisplayRootsTable(intervals, signs);
							});

							n_iterations.Add(NewtonsMethod(solver, dsolver, (n_root.min + n_root.max) / 2, epsilon));

							EpsilonTables.Add((int index) => {
								var iteration = rf_iterations[index];

								GUI.Table("EpsilonTable", ["n", "xn", "epsilon"], delegate {
									for (int i = 0; i < iteration.steps.Count; i++) {
										var row = iteration.steps[i];

										ImGui.TableNextRow();

										ImGui.TableNextColumn();
										ImGui.Text(row.iteration.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(row.xn.ToString());

										ImGui.TableNextColumn();
										ImGui.Text(i == 0 ? "-" : row.epsilon.ToString());
									}
								}, ImGuiTableFlags.Borders);
								var answer = iteration.root;

								int precision = epsilon.ToString("0.################").Split('.')[1].Length;
								var sb = new StringBuilder();
								sb.Append("##0");
								sb.Append('.');
								sb.Append('0', precision);
								ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
							});

						}
						break;
				}

				_resultExists = true;

			} catch {
				_resultExists = false;

				GUI.ThrowError("The values provided are invalid. Try again");
			}
		}

		List<(double min, double max)> FindRootIntervals(Func<double, double> f, double start, double end, double step) {
			var intervals = new List<(double min, double max)>();

			double prevX = start;
			double prevY = f(prevX);

			for (double x = start + step; x <= end; x += step) {
				double y = f(x);

				// Check for a sign change
				if (prevY * y < 0) // Root is bracketed
				{
					intervals.Add((prevX, x));
				}

				// Update previous values
				prevX = x;
				prevY = y;
			}

			return intervals;
		}

		(double root, List<(int iteration, double xn, double f_xn, double f_prime_xn)> steps) NewtonsMethod(Func<double, double> f, Func<double, double> fPrime, double initialGuess, double epsilon) {
			var steps = new List<(int iteration, double xn, double f_xn, double f_prime_xn)>();
			double xn = initialGuess;

			for (int iteration = 0; ; iteration++) {
				double f_xn = f(xn);
				double f_prime_xn = fPrime(xn);

				// Record the current step
				steps.Add((iteration, xn, f_xn, f_prime_xn));

				// Check if derivative is zero to avoid division by zero
				if (Math.Abs(f_prime_xn) < 1e-12) {
					throw new InvalidOperationException("Derivative is zero. Newton's Method fails.");
				}

				// Calculate the next approximation
				double nextXn = xn - f_xn / f_prime_xn;

				// Check stopping criteria
				if (Math.Abs(f_xn) < epsilon || Math.Abs(nextXn - xn) < epsilon) {
					xn = nextXn;
					break;
				}

				xn = nextXn;
			}

			return (xn, steps);
		}

		(double root, List<(int iteration, double xn, double epsilon)> steps) RegulaFalsi(Func<double, double> f, double a, double b, double epsilon) {
			var steps = new List<(int iteration, double xn, double epsilon)>();

			double xn_old = 0;
			double xn = a; // Initial guess
			for (int iteration = 0; ; iteration++) {
				double fa = f(a);
				double fb = f(b);

				// Calculate the new approximation
				xn = b - fb * (b - a) / (fb - fa);
				double fxn = f(xn);

				// Record the current step
				steps.Add((iteration, xn, Math.Abs(xn - xn_old)));

				// Check stopping criteria
				if (Math.Abs(fxn) < epsilon || Math.Abs(b - a) < epsilon)
					break;

				// Update the interval
				if (fa * fxn < 0)
					b = xn; // Root is in [a, xn]
				else
					a = xn; // Root is in [xn, b]

				xn_old = xn;
			}

			return (xn, steps);
		}

		private void DisplayRootsTable(List<string> rootValues, List<char> rootSigns) {
			if (ImGui.BeginTable("SignTable", rootValues.Count + 1, ImGuiTableFlags.Borders)) {
				ImGui.TableNextRow();

				ImGui.TableNextColumn();
				ImGui.Text("x");

				foreach (var root in rootValues) {
					ImGui.TableNextColumn();
					ImGui.Text(root);
				}

				ImGui.TableNextRow();

				ImGui.TableNextColumn();
				ImGui.Text("sign f(x)");

				foreach (var root in rootSigns) {
					ImGui.TableNextColumn();
					ImGui.Text(root.ToString());
				}

				ImGui.EndTable();
			}
		}

		private List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> Bisection(List<(double min, double max, bool flip)> intervals, double tolerance) {
			List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> output = new();

			var x = SymbolicExpression.Variable("x");
			var function = SymbolicExpression.Parse(expr);
			var solver = function.Compile("x");

			foreach (var interval in intervals) {
				List<(double a, double b, double epsilon, double mid, double fMid, char sign)> iterations = new(); // List to hold iteration data

				double a = interval.min;
				double b = interval.max;
				double mid = (a + b) / 2.0;
				double epsilon = b - a;
				bool flip = interval.flip;

				while (epsilon > tolerance) {
					double f_a = solver.Invoke(a);
					double f_b = solver.Invoke(b);
					double f_mid = solver.Invoke(mid);

					// Record iteration data
					iterations.Add((a, b, epsilon, mid, fMid: f_mid, sign: f_mid < 0 ? '-' : '+'));
					if (flip) {
						if (f_mid < 0) { // root is in [a, mid]
							b = mid;
						} else {
							a = mid;
						}
					} else {
						if (f_mid >= 0) { // root is in [a, mid]
							b = mid;
						} else {
							a = mid;
						}
					}

					mid = (a + b) / 2.0;

					epsilon = b - a;
				}

				var fmid = solver.Invoke(mid);

				iterations.Add((a, b, epsilon, mid, fMid: fmid, sign: fmid < 0 ? '-' : '+'));

				output.Add(iterations);
			}

			return output;
		}

		public override float GetMenuHeight() {
			return 600;
		}

		public override float GetMenuWidth() {
			return 700;
		}
	}
}
