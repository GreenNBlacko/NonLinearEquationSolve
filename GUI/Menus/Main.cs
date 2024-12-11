using ImGuiNET;
using MathNet.Numerics;
using MathNet.Symbolics;
using System.Drawing;
using System.Text;
using static NonLinearEquationSolve.GUI.Renderer;

namespace NonLinearEquationSolve.GUI.Menus;

public class Main(ContextManager ctx) : Menu(ctx) {
    public override int priority => (int)e_Menus.Main;

    private string expr      = "";
    private double epsilon   = 0.0001;
    private int    selection = 0;

    private readonly string[] methods = ["Intervalo dalijimas pusiau", "kirstiniu", "liestiniu"];

    private bool                                      _resultExists      = false;
    private bool                                      _useManualInterval = false;
    private (int start, int end)                      _manualInterval    = (0, 0);
    private string                                    _function          = string.Empty;
    private string                                    _derivative        = string.Empty;
    private string                                    _derivativeRoots   = string.Empty;
    private List<List<string>>                        intervalTests      = [];
    private Func<double, double>                      _doubleDerivative;
    private List<(double min, double max, bool flip)> intervals   = [];
    private List<string>                              _rootValues = [];
    private List<char>                                _rootSigns  = [];
    private Action                                    _RootsTable;

    private List<List<string>> _rootValuesList = [];
    private List<List<char>>   _rootSignsList  = [];
    private List<Action<int>>  _RootsTables    = [];

    private List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> slices        = [];
    private List<(double root, List<(int iteration, double xn, double epsilon)> steps)>          rf_iterations = [];
    private List<(double root, List<(int iteration, double xn, double epsilon)> steps)>          n_iterations  = [];
    private List<Action<int>>                                                                    EpsilonTables = [];

    public override void Render() {
        ImGui.InputText("Expression", ref expr, 256);
        ImGui.InputDouble("Epsilon", ref epsilon);

        ImGui.Checkbox("Use graphing method", ref _useManualInterval);

        if (_useManualInterval) {
            ImGui.InputInt("Start", ref _manualInterval.start);
            ImGui.InputInt("End",   ref _manualInterval.end);
        }

        if (ImGui.Combo("Method", ref selection, methods, methods.Length))
            _resultExists = false;

        if (GUI.FullWidthButton("Calculate")) CalculateExpression();

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
                for (var i = 0; i < _RootsTables.Count; i++) {
                    var table = _RootsTables[i];
                    table.Invoke(i);
                }

                for (var i = 0; i < EpsilonTables.Count; i++) {
                    var table = EpsilonTables[i];
                    table.Invoke(i);
                }

                break;

            case 1: // Regula falsi
                for (var i = 0; i < _RootsTables.Count; i++) {
                    var table = _RootsTables[i];
                    table.Invoke(i);
                }

                for (var i = 0; i < EpsilonTables.Count; i++) {
                    foreach (var test in intervalTests[i])
                        ImGui.Text(test);

                    var table = EpsilonTables[i];
                    table.Invoke(i);
                }

                break;

            case 2: // Newton
                for (var i = 0; i < _RootsTables.Count; i++) {
                    var table = _RootsTables[i];
                    table.Invoke(i);
                }

                for (var i = 0; i < EpsilonTables.Count; i++) {
                    foreach (var test in intervalTests[i])
                        ImGui.Text(test);

                    var table = EpsilonTables[i];
                    table.Invoke(i);
                }

                break;
        }
    }

    private void CalculateExpression() {
        try {
            expr = expr.Replace('X', 'x');

            intervalTests = [];

            var x        = SymbolicExpression.Variable("x");
            var function = SymbolicExpression.Parse(expr);
            var solver   = function.Compile("x");

            _function = function.ToString();

            var derivative = function.Differentiate(x);

            _derivative = derivative.ToString();

            var polynomial = derivative.Coefficients(x);

            List<double> coeffs = [];

            foreach (var coefficient in polynomial) coeffs.Add(double.Parse(coefficient.ToString()));

            var roots = FindRoots.Polynomial(coeffs.ToArray());

            _RootsTables = [];

            var lowestVal = 0d;
            var highVal   = 0d;

            if (_useManualInterval) {
                lowestVal = _manualInterval.start;
                highVal   = _manualInterval.end;

                _rootValues = [];
                _rootSigns  = [];

                _rootValues.Add("-inf");
                _rootSigns.Add('-');

                _derivativeRoots = string.Empty;

                if (selection == 0) {
                    _derivativeRoots += $"x = {lowestVal}  or  ";
                    _derivativeRoots += $"x = {highVal}  or  ";
                }
                else {
                    var doubleDerivative = derivative.Differentiate(x);
                    _doubleDerivative = doubleDerivative.Compile("x");
                    _derivativeRoots  = $"f''(x) = {doubleDerivative.ToString}";
                }

                _rootValues.Add(lowestVal.ToString());
                _rootSigns.Add(solver.Invoke(lowestVal) < 0 ? '-' : '+');

                _rootValues.Add(highVal.ToString());
                _rootSigns.Add(solver.Invoke(highVal) < 0 ? '-' : '+');

                _rootValues.Add("inf");
                _rootSigns.Add('+');

                if (selection == 0)
                    _derivative = "-";
            }
            else {
                lowestVal = roots[0].Real - 1;
                highVal   = roots[1].Real + 1;

                _rootValues = [];
                _rootSigns  = [];

                _rootValues.Add("-inf");
                _rootSigns.Add('-');

                _derivativeRoots = string.Empty;

                if (selection != 0) {
                    var doubleDerivative = derivative.Differentiate(x);
                    _doubleDerivative = doubleDerivative.Compile("x");
                    _derivativeRoots  = $"f''(x) = {doubleDerivative.ToString()}\n";
                }

                foreach (var root in roots) {
                    _derivativeRoots += $"x = {root.Real}  or  ";
                    _rootValues.Add(root.Real.ToString());
                    _rootSigns.Add(solver.Invoke(root.Real) < 0 ? '-' : '+');
                }

                _rootValues.Add("inf");
                _rootSigns.Add('+');
            }

            intervals = [];

            _derivativeRoots = _derivativeRoots.Substring(0, _derivativeRoots.Length - "  or  ".Length);

            _RootsTable = delegate { DisplayRootsTable(_rootValues, _rootSigns); };

            EpsilonTables = [];

            switch (selection) {
                case 0: // div by 2
                    if (_useManualInterval) {
                        intervals.Add((_manualInterval.start, _manualInterval.end,
                                       solver.Invoke(_manualInterval.start) > 0));
                    }
                    else {
                        var bs_roots = FindRootIntervals(solver, lowestVal, highVal, 1);

                        foreach (var root in bs_roots) intervals.Add((root.min, root.max, solver.Invoke(root.min) > 0));
                    }

                    slices = [];

                    slices = Bisection(intervals, epsilon);

                    for (var i = 0; i < intervals.Count; i++) {
                        var idx     = int.Parse(i.ToString());
                        var bs_root = intervals[idx];

                        List<string> bs_intervals = [];
                        List<char>   bs_signs     = [];

                        bs_intervals.Add(bs_root.min.ToString());
                        bs_intervals.Add(bs_root.max.ToString());

                        bs_signs.Add(solver.Invoke(bs_root.min) < 0 ? '-' : '+');
                        bs_signs.Add(solver.Invoke(bs_root.max) < 0 ? '-' : '+');

                        _rootValuesList.Add(bs_intervals);
                        _rootSignsList.Add(bs_signs);

                        _RootsTables.Add((int index) => {
                            var intervals = _rootValuesList[index];
                            var signs     = _rootSignsList[index];

                            DisplayRootsTable(intervals, signs);
                        });

                        EpsilonTables.Add((int index) => {
                            var slice = slices[index];

                            var lastSign = '*';

                            GUI.Table("EpsilonTable", ["n", "a", "b", "epsilon", "xn", "fxn", "sign"], delegate {
                                for (var i = 0; i < slice.Count; i++) {
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

                            var precision = epsilon.ToString("0.################").Split('.')[1].Length;
                            var sb        = new StringBuilder();
                            sb.Append("##0");
                            sb.Append('.');
                            sb.Append('0', precision);
                            ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
                        });
                    }

                    break;

                case 1: // Regula Falsi, f'(x), M max, m min, M <= 2m, f''(start) f''(end)
                    if (_useManualInterval) {
                        intervals.Add((_manualInterval.start, _manualInterval.end,
                                       solver.Invoke(_manualInterval.start) > 0));
                    }
                    else {
                        var rf_roots = FindRootIntervals(solver, lowestVal, highVal, 1);

                        foreach (var root in rf_roots) intervals.Add((root.min, root.max, solver.Invoke(root.min) > 0));
                    }

                    _rootSignsList.Clear();
                    _rootValuesList.Clear();
                    _RootsTables.Clear();
                    rf_iterations = [];
                    EpsilonTables = [];

                    for (var i = 0; i < intervals.Count; i++) {
                        var tests   = new List<string>();
                        var idx     = int.Parse(i.ToString());
                        var rf_root = intervals[idx];

                        List<string> rf_intervals = [];
                        List<char>   rf_signs     = [];

                        rf_intervals.Add(rf_root.min.ToString());
                        rf_intervals.Add(rf_root.max.ToString());

                        rf_signs.Add(solver.Invoke(rf_root.min) < 0 ? '-' : '+');
                        rf_signs.Add(solver.Invoke(rf_root.max) < 0 ? '-' : '+');

                        _rootValuesList.Add(rf_intervals);
                        _rootSignsList.Add(rf_signs);

                        _RootsTables.Add((int index) => {
                            var intervals = _rootValuesList[index];
                            var signs     = _rootSignsList[index];

                            DisplayRootsTable(intervals, signs);
                        });

                        var derivativeSolver = derivative.Compile("x");

                        var sb = new StringBuilder();

                        var rootMin = rf_root.min;
                        var rootMax = rf_root.max;

                        var a = Math.Abs(derivativeSolver.Invoke(rootMin));
                        var b = Math.Abs(derivativeSolver.Invoke(rootMax));

                        var max = Math.Max(a, b);
                        var min = Math.Min(a, b);

                        var maxVar = max == a ? rootMin : rootMax;
                        var minVar = min == a ? rootMin : rootMax;

                        sb.AppendLine($"[{rootMin}; {rootMax}]");
                        sb.AppendLine($"|f'({maxVar})| = {max} = max");
                        sb.AppendLine($"|f'({minVar})| = {min} = min");
                        sb.Append($"{max} <= {min * 2} - ");

                        while (max > min * 2) {
                            sb.AppendLine("invalid");
                            tests.Add(sb.ToString());
                            sb.Clear();

                            var signA    = solver.Invoke(rootMin) < 0 ? '-' : '+';
                            var signB    = solver.Invoke(rootMax) < 0 ? '-' : '+';
                            var midPoint = (rootMin + rootMax) / 2;

                            Console.WriteLine(midPoint);

                            var midPointSign = solver.Invoke(midPoint) < 0 ? '-' : '+';
                            Console.WriteLine(signA);
                            Console.WriteLine(signB);
                            Console.WriteLine(midPointSign);
                            Console.WriteLine();

                            if ((midPointSign == '+' && rf_root.flip) || (midPointSign == '-' && !rf_root.flip))
                                rootMin = midPoint;
                            else
                                rootMax = midPoint;

                            a = Math.Abs(derivativeSolver.Invoke(rootMin));
                            b = Math.Abs(derivativeSolver.Invoke(rootMax));

                            max = Math.Max(a, b);
                            min = Math.Min(a, b);

                            maxVar = max == a ? rootMin : rootMax;
                            minVar = min == a ? rootMin : rootMax;

                            sb.AppendLine($"[{rootMin}; {rootMax}]");
                            sb.AppendLine($"|f'({maxVar})| = {max} = max");
                            sb.AppendLine($"|f'({minVar})| = {min} = min");
                            sb.Append($"{max} <= {min * 2} - ");
                        }

                        sb.AppendLine("valid");
                        tests.Add(sb.ToString());
                        sb.Clear();

                        var da = _doubleDerivative.Invoke(rootMin);
                        var db = _doubleDerivative.Invoke(rootMax);

                        var aSign = solver.Invoke(rootMin) < 0 ? '-' : '+';
                        var bSign = solver.Invoke(rootMax) < 0 ? '-' : '+';

                        var dfx     = Math.Max(da, db);
                        var dfxSign = dfx < 0 ? '-' : '+';

                        var xn   = 0d;
                        var side = 0;

                        if (aSign == dfxSign) {
                            xn   = rootMax;
                            side = 0;
                        }
                        else if (bSign == dfxSign) {
                            xn   = rootMin;
                            side = 1;
                        }
                        else {
                            _RootsTables.RemoveAt(i);
                            continue;
                        }

                        rf_iterations.Add(RegulaFalsi(solver, xn, rootMin, rootMax, epsilon, side));

                        EpsilonTables.Add((int index) => {
                            var iteration = rf_iterations[index];

                            GUI.Table("EpsilonTable", ["n", "xn", "epsilon"], delegate {
                                for (var i = 0; i < iteration.steps.Count; i++) {
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

                            var precision = epsilon.ToString("0.################").Split('.')[1].Length;
                            var sb        = new StringBuilder();
                            sb.Append("##0");
                            sb.Append('.');
                            sb.Append('0', precision);
                            ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
                        });

                        intervalTests.Add(tests);
                    }

                    break;

                case 2: // Newton
                    if (_useManualInterval) {
                        intervals.Add((_manualInterval.start, _manualInterval.end,
                                       solver.Invoke(_manualInterval.start) > 0));
                    }
                    else {
                        var rf_roots = FindRootIntervals(solver, lowestVal, highVal, 1);

                        foreach (var root in rf_roots) intervals.Add((root.min, root.max, solver.Invoke(root.min) > 0));
                    }

                    _rootSignsList.Clear();
                    _rootValuesList.Clear();
                    _RootsTables.Clear();
                    rf_iterations = [];
                    EpsilonTables = [];

                    for (var i = 0; i < intervals.Count; i++) {
                        var tests  = new List<string>();
                        var idx    = int.Parse(i.ToString());
                        var n_root = intervals[idx];

                        List<string> n_intervals = [];
                        List<char>   n_signs     = [];

                        n_intervals.Add(n_root.min.ToString());
                        n_intervals.Add(n_root.max.ToString());

                        n_signs.Add(solver.Invoke(n_root.min) < 0 ? '-' : '+');
                        n_signs.Add(solver.Invoke(n_root.max) < 0 ? '-' : '+');

                        _rootValuesList.Add(n_intervals);
                        _rootSignsList.Add(n_signs);

                        _RootsTables.Add((int index) => {
                            var intervals = _rootValuesList[index];
                            var signs     = _rootSignsList[index];

                            DisplayRootsTable(intervals, signs);
                        });

                        var derivativeSolver = derivative.Compile("x");

                        var sb = new StringBuilder();

                        var rootMin = n_root.min;
                        var rootMax = n_root.max;

                        var a = Math.Abs(derivativeSolver.Invoke(rootMin));
                        var b = Math.Abs(derivativeSolver.Invoke(rootMax));

                        var max = Math.Max(a, b);
                        var min = Math.Min(a, b);

                        var maxVar = max == a ? rootMin : rootMax;
                        var minVar = min == a ? rootMin : rootMax;

                        sb.AppendLine($"[{rootMin}; {rootMax}]");
                        sb.AppendLine($"|f'({maxVar})| = {max} = max");
                        sb.AppendLine($"|f'({minVar})| = {min} = min");
                        sb.Append($"{max} <= {min * 2} - ");

                        while (max > min * 2) {
                            sb.AppendLine("invalid");
                            tests.Add(sb.ToString());
                            sb.Clear();

                            var signA    = solver.Invoke(rootMin) < 0 ? '-' : '+';
                            var signB    = solver.Invoke(rootMax) < 0 ? '-' : '+';
                            var midPoint = (rootMin + rootMax) / 2;

                            Console.WriteLine(midPoint);

                            var midPointSign = solver.Invoke(midPoint) < 0 ? '-' : '+';
                            Console.WriteLine(signA);
                            Console.WriteLine(signB);
                            Console.WriteLine(midPointSign);
                            Console.WriteLine();

                            if ((midPointSign == '+' && n_root.flip) || (midPointSign == '-' && !n_root.flip))
                                rootMin = midPoint;
                            else
                                rootMax = midPoint;

                            a = Math.Abs(derivativeSolver.Invoke(rootMin));
                            b = Math.Abs(derivativeSolver.Invoke(rootMax));

                            max = Math.Max(a, b);
                            min = Math.Min(a, b);

                            maxVar = max == a ? rootMin : rootMax;
                            minVar = min == a ? rootMin : rootMax;

                            sb.AppendLine($"[{rootMin}; {rootMax}]");
                            sb.AppendLine($"|f'({maxVar})| = {max} = max");
                            sb.AppendLine($"|f'({minVar})| = {min} = min");
                            sb.Append($"{max} <= {min * 2} - ");
                        }

                        sb.AppendLine("valid");
                        tests.Add(sb.ToString());
                        sb.Clear();

                        var da = derivativeSolver.Invoke(rootMin);
                        var db = derivativeSolver.Invoke(rootMax);

                        var dda = _doubleDerivative.Invoke(rootMin);
                        var ddb = _doubleDerivative.Invoke(rootMax);

                        var aSign = solver.Invoke(rootMin) < 0 ? '-' : '+';
                        var bSign = solver.Invoke(rootMax) < 0 ? '-' : '+';

                        var dfx     = Math.Max(da, db);
                        var dfxSign = dfx < 0 ? '-' : '+';

                        var ddfx     = Math.Max(dda, ddb);
                        var ddfxSign = ddfx < 0 ? '-' : '+';

                        var xn   = 0d;
                        var side = 0;

                        if (aSign == bSign) {
                            _RootsTables.RemoveAt(i);
                            continue;
                        }

                        if ((aSign == '-' && bSign == '+' && dfxSign == '+' && ddfxSign == '+') ||
                            (aSign == '+' && bSign == '-' && dfxSign == '-' && ddfxSign == '-')) {
                            xn   = rootMax;
                            side = 0;
                        }
                        else if ((aSign == '-' && bSign == '+' && dfxSign == '+' && ddfxSign == '-') ||
                                 (aSign == '+' && bSign == '-' && dfxSign == '-' && ddfxSign == '+')) {
                            xn   = rootMin;
                            side = 1;
                        }
                        else {
                            _RootsTables.RemoveAt(i);
                            continue;
                        }

                        n_iterations.Add(NewtonsMethod(solver, derivativeSolver, xn, rootMin, rootMax, epsilon, side));

                        EpsilonTables.Add((int index) => {
                            var iteration = n_iterations[index];

                            GUI.Table("EpsilonTable", ["n", "xn", "epsilon"], delegate {
                                for (var i = 0; i < iteration.steps.Count; i++) {
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

                            var precision = epsilon.ToString("0.################").Split('.')[1].Length;
                            var sb        = new StringBuilder();
                            sb.Append("##0");
                            sb.Append('.');
                            sb.Append('0', precision);
                            ImGui.Text($"x{idx + 1} = {Math.Round(answer, precision).ToString(sb.ToString())}");
                        });

                        intervalTests.Add(tests);
                    }

                    break;
            }

            _resultExists = true;
        }
        catch {
            _resultExists      = false;
            _useManualInterval = true;
        }
    }

    private List<(double min, double max)> FindRootIntervals(Func<double, double> f,
                                                             double               start,
                                                             double               end,
                                                             double               step) {
        var intervals = new List<(double min, double max)>();

        var prevX = start;
        var prevY = f(prevX);

        for (var x = start + step; x <= end; x += step) {
            var y = f(x);

            // Check for a sign change
            if (prevY * y < 0) // Root is bracketed
                intervals.Add((prevX, x));

            // Update previous values
            prevX = x;
            prevY = y;
        }

        return intervals;
    }

    private (double root, List<(int iteration, double xn, double epsilon)> steps) NewtonsMethod(
        Func<double, double> f,
        Func<double, double> fd,
        double               xn,
        double               a,
        double               b,
        double               epsilon,
        int                  side) {
        var steps = new List<(int iteration, double xn, double epsilon)>();

        double xn_old = 0;
        steps.Add((0, xn, Math.Abs(xn - xn_old)));
        for (var iteration = 1;; iteration++) {
            xn_old = xn;

            if (side == 0) {
                if (iteration == 0)
                    xn = b;
                else
                    xn -= f(xn) / fd(xn);
            }
            else {
                if (iteration == 0)
                    xn = a;
                else
                    xn -= f(xn) / fd(xn);
            }

            steps.Add((iteration, xn, Math.Abs(xn - xn_old)));

            // Check stopping criteria
            if (Math.Abs(xn - xn_old) <= epsilon)
                break;
        }

        return (xn_old, steps);
    }

    private (double root, List<(int iteration, double xn, double epsilon)> steps) RegulaFalsi(
        Func<double, double> f,
        double               xn,
        double               a,
        double               b,
        double               epsilon,
        int                  side) {
        var steps = new List<(int iteration, double xn, double epsilon)>();

        double xn_old = 0;
        steps.Add((0, xn, Math.Abs(xn - xn_old)));
        for (var iteration = 1;; iteration++) {
            xn_old = xn;

            if (side == 0) {
                if (iteration == 0)
                    xn = b - f(b) * (b - a) / (f(b) - f(a));
                else
                    xn -= f(xn) * (xn - a) / (f(xn) - f(a));
            }
            else {
                if (iteration == 0)
                    xn = a - f(a) * (b - a) / (f(b) - f(a));
                else
                    xn -= f(xn) * (b - xn) / (f(b) - f(xn));
            }

            steps.Add((iteration, xn, Math.Abs(xn - xn_old)));

            // Check stopping criteria
            if (Math.Abs(xn - xn_old) <= epsilon)
                break;
        }

        return (xn_old, steps);
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

    private List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> Bisection(
        List<(double min, double max, bool flip)> intervals,
        double                                    tolerance) {
        List<List<(double a, double b, double epsilon, double mid, double fMid, char sign)>> output = [];

        var x        = SymbolicExpression.Variable("x");
        var function = SymbolicExpression.Parse(expr);
        var solver = function.Compile("x");

        foreach (var interval in intervals) {
            List<(double a, double b, double epsilon, double mid, double fMid, char sign)>
                iterations = []; // List to hold iteration data

            var a       = interval.min;
            var b       = interval.max;
            var mid     = (a + b) / 2.0;
            var epsilon = b - a;
            var flip    = interval.flip;

            while (epsilon > tolerance) {
                var f_a   = solver.Invoke(a);
                var f_b   = solver.Invoke(b);
                var f_mid = solver.Invoke(mid);

                // Record iteration data
                iterations.Add((a, b, epsilon, mid, fMid: f_mid, sign: f_mid < 0 ? '-' : '+'));
                if (flip) {
                    if (f_mid < 0) // root is in [a, mid]
                        b = mid;
                    else
                        a = mid;
                }
                else {
                    if (f_mid >= 0) // root is in [a, mid]
                        b = mid;
                    else
                        a = mid;
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