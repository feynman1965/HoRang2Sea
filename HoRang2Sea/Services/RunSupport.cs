using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

// 실행 보조 — 프로파일 읽기, 입력 그리드 숫자 확인, 실행 입력 기록, 앱 데이터 경로 (2026-10-05).
// Ground · Air · Sea 세 앱이 같은 코드를 쓴다(네임스페이스만 다름). 한 앱에서 고치면 나머지도 같이 고칠 것.
namespace HoRang2Sea.Services
{
    /// <summary>프로파일 종류 — 읽은 뒤 값 범위를 확인할 때 쓴다.</summary>
    public enum ProfileKind { SpeedKmh, SpeedRatio, AltitudeNed, Rpm, Torque }

    /// <summary>앱 데이터 폴더(%LOCALAPPDATA%\HoRang2). 설정(Configs)과 같은 곳.</summary>
    public static class AppPaths
    {
        public static string DataRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoRang2");

        public static string ErrorLogPath(string appName)
        {
            string dir = Path.Combine(DataRoot, "Logs");
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, $"error_log_{appName}.txt");
        }

        public static string RunInputsDir(string model)
        {
            string dir = Path.Combine(DataRoot, "RunInputs", model);
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        public static string DefaultExportDir =>
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    /// <summary>
    /// 프로파일(.txt/.csv) 읽기. 한 줄 = 한 시점, 마지막 열 = 값. 열이 둘 이상이면 첫 열 = 시각(초).
    /// 시각 간격이 0.001초(모델 step)면 값을 그대로 쓰고, 다르면 0.001초로 선형 보간한다
    /// (이전에는 시각 열을 버리고 한 줄 = 1 ms로 써서 1초 간격 표가 1,000배 압축됐다).
    /// 숫자는 지역 설정과 상관없이 '.' 소수점으로 읽는다. 첫 숫자 줄 앞의 글자 줄은 머리글로 건너뛴다.
    /// </summary>
    public static class ProfileFile
    {
        public const double StepSeconds = 0.001;
        private static readonly char[] Sep = { '\t', ' ', ',', ';' };

        public static bool TryParseNumber(string s, out double v)
        {
            v = 0;
            if (s == null) return false;
            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                   && !double.IsNaN(v) && !double.IsInfinity(v);
        }

        public sealed class Result
        {
            public double[] Values;
            public string FileName;
            public int HeaderLines;
            public bool HadTimeColumn;
            public double SourceStep = StepSeconds;
            public bool Resampled;
            public string Error;   // null 이면 성공
            public double Min => Values == null || Values.Length == 0 ? 0 : Values.Min();
            public double Max => Values == null || Values.Length == 0 ? 0 : Values.Max();
            public double DurationSeconds => Values == null || Values.Length == 0 ? 0 : (Values.Length - 1) * StepSeconds;
        }

        private static string Trunc(string s) => s.Length <= 60 ? s : s.Substring(0, 60) + "…";

        public static Result Read(string path)
        {
            var r = new Result { FileName = Path.GetFileName(path) };
            string[] lines;
            try { lines = File.ReadAllLines(path); }
            catch (Exception ex) { r.Error = $"The file could not be read: {ex.Message}"; return r; }

            var times = new List<double>(lines.Length);
            var vals = new List<double>(lines.Length);
            bool started = false;
            int columns = 0;
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var tok = line.Split(Sep, StringSplitOptions.RemoveEmptyEntries);
                if (tok.Length == 0 || !TryParseNumber(tok[^1], out double v))
                {
                    if (!started) { r.HeaderLines++; continue; }
                    r.Error = $"Line {n + 1} is not a number: \"{Trunc(line.Trim())}\"";
                    return r;
                }
                double t = 0;
                bool hasTime = tok.Length >= 2 && TryParseNumber(tok[0], out t);
                if (!started) { started = true; columns = hasTime ? 2 : 1; }
                if (columns == 2)
                {
                    if (!hasTime) { r.Error = $"Line {n + 1} has no time value: \"{Trunc(line.Trim())}\""; return r; }
                    times.Add(t);
                }
                vals.Add(v);
            }
            if (vals.Count == 0) { r.Error = "No numeric data was found in the file."; return r; }
            if (columns == 1 || vals.Count == 1) { r.Values = vals.ToArray(); return r; }

            r.HadTimeColumn = true;
            double minDt = double.MaxValue, maxDt = 0;
            for (int i = 1; i < times.Count; i++)
            {
                double dt = times[i] - times[i - 1];
                if (dt <= 0)
                {
                    r.Error = $"Time does not increase at data row {i + 1} ({times[i - 1].ToString(CultureInfo.InvariantCulture)} → {times[i].ToString(CultureInfo.InvariantCulture)} s).";
                    return r;
                }
                if (dt < minDt) minDt = dt;
                if (dt > maxDt) maxDt = dt;
            }
            r.SourceStep = (times[^1] - times[0]) / (times.Count - 1);
            if (Math.Abs(minDt - StepSeconds) < 1e-6 && Math.Abs(maxDt - StepSeconds) < 1e-6)
            {
                r.Values = vals.ToArray();   // 0.001초 간격 — 이전과 똑같이 값만 쓴다
                return r;
            }

            // 0.001초 간격으로 선형 보간 (첫 시각부터 마지막 시각까지)
            int count = (int)Math.Floor((times[^1] - times[0]) / StepSeconds + 1e-9) + 1;
            var outv = new double[count];
            int k = 0;
            for (int i = 0; i < count; i++)
            {
                double tt = times[0] + i * StepSeconds;
                while (k < times.Count - 2 && times[k + 1] < tt) k++;
                double t0 = times[k], t1 = times[k + 1];
                double a = (tt - t0) / (t1 - t0);
                if (a < 0) a = 0;
                if (a > 1) a = 1;
                outv[i] = vals[k] + (vals[k + 1] - vals[k]) * a;
            }
            r.Values = outv;
            r.Resampled = true;
            return r;
        }

        private static readonly Dictionary<string, double[]> _defaultCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>앱과 함께 설치되는 DefaultProfiles\{fileName}. 없거나 못 읽으면 null.
        /// 한 번 읽은 파일은 기억해 두고 복사본을 돌려준다(수백만 줄이라 매번 읽으면 느리다).</summary>
        public static double[] ReadDefault(string fileName)
        {
            try
            {
                lock (_defaultCache)
                {
                    if (_defaultCache.TryGetValue(fileName, out var cached)) return (double[])cached.Clone();
                }
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultProfiles", fileName);
                if (!File.Exists(p)) return null;
                var r = Read(p);
                if (r.Error != null) return null;
                lock (_defaultCache) { _defaultCache[fileName] = r.Values; }
                return (double[])r.Values.Clone();
            }
            catch { return null; }
        }

        /// <summary>
        /// 사용자가 고른 파일 읽기(업로드 · 설정 불러오기). 실패하면 이유를 보여 주고 null.
        /// 보간했거나 값 범위가 그 입력에 맞지 않으면 알려 준다. 고도(NED)는 양수만 있으면 부호를 바꿀지 묻는다.
        /// </summary>
        public static double[] ReadForUi(string path, ProfileKind kind, bool quiet = false)
        {
            var r = Read(path);
            if (r.Error != null)
            {
                MessageBox.Show($"The profile could not be loaded.\n\nFile: {r.FileName}\n{r.Error}\n\n" +
                                "Expected: one value per line (time step 0.001 s), or two columns 'time(s) value'.",
                                "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            var notes = new List<string>();
            if (r.Resampled)
                notes.Add($"The time step in the file is {r.SourceStep.ToString("G4", CultureInfo.InvariantCulture)} s. " +
                          $"The values were linearly interpolated to the model step of 0.001 s ({r.Values.Length:N0} points, {r.DurationSeconds:N1} s).");

            if (kind == ProfileKind.AltitudeNed && r.Max > 1e-6 && r.Min > -1e-6 && !quiet)
            {
                var ans = MessageBox.Show(
                    $"This altitude profile has only positive values (0 to {r.Max.ToString("G4", CultureInfo.InvariantCulture)} m).\n\n" +
                    "The model uses NED altitude: up is negative, and values of 0 or more are treated as 'no altitude command'.\n\n" +
                    "Flip the sign so that the profile climbs (for example, 10 m becomes -10 m)?",
                    "Altitude profile", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (ans == MessageBoxResult.Yes)
                {
                    for (int i = 0; i < r.Values.Length; i++) r.Values[i] = -r.Values[i];
                    notes.Add("The sign of the altitude values was flipped (up is negative).");
                }
            }

            string range = RangeNote(r, kind);
            if (range != null) notes.Add(range);
            if (!quiet && notes.Count > 0)
                MessageBox.Show($"Profile: {r.FileName}\n\n" + string.Join("\n\n", notes), "Profile",
                                MessageBoxButton.OK, range != null ? MessageBoxImage.Warning : MessageBoxImage.Information);
            return r.Values;
        }

        private static string RangeNote(Result r, ProfileKind kind)
        {
            string lo = r.Min.ToString("G4", CultureInfo.InvariantCulture), hi = r.Max.ToString("G4", CultureInfo.InvariantCulture);
            switch (kind)
            {
                case ProfileKind.SpeedRatio:
                    if (r.Min < -1e-9 || r.Max > 1 + 1e-9)
                        return $"The speed profile of this model is a ratio between 0 and 1, but this file ranges from {lo} to {hi}. " +
                               "Values outside 0 to 1 give results the model was not built for.";
                    break;
                case ProfileKind.AltitudeNed:
                    if (r.Max > 1e-6 && r.Min < -1e-6)
                        return $"This altitude profile has both negative and positive values ({lo} to {hi} m). " +
                               "In NED, up is negative; values of 0 or more are treated as 'no altitude command'.";
                    break;
                case ProfileKind.SpeedKmh:
                    if (r.Min < -1)
                        return $"The speed profile has negative values (down to {lo} km/h).";
                    break;
            }
            return null;
        }
    }

    /// <summary>입력 그리드 값 읽기 — 지역 설정과 상관없이 '.' 소수점.</summary>
    public static class GridInput
    {
        public static bool TryParse(string s, out double v) => ProfileFile.TryParseNumber(s, out v);

        /// <summary>숫자가 아닌 칸이 있으면 Run 을 멈추고 어느 칸인지 알린다(그대로 돌리면 뒤 입력이 한 포트씩 밀린다).</summary>
        public static void ShowInvalid(IList<string> cells)
        {
            var shown = cells.Take(10).ToList();
            string more = cells.Count > shown.Count ? $"\n… and {cells.Count - shown.Count} more" : "";
            MessageBox.Show("The simulation was not started because some input values are not numbers:\n\n" +
                            string.Join("\n", shown) + more +
                            "\n\nEnter a number in each cell (use '.' as the decimal separator, e.g. 0.25).",
                            "Input check", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>실행 입력 기록 — Run 할 때 앱이 DLL 에 쓴 값을 initial_value.txt 형식(포트&lt;Tab&gt;값)으로 남긴다.
    /// 모델을 만든 쪽이 Simulink 에서 같은 조건을 재현할 때, 우리가 앱 동작을 확인할 때 쓴다.</summary>
    public static class RunInputLog
    {
        private const int Keep = 30;

        /// <summary>매 step 넣는 프로파일 하나(포트 · 값 · 출처 = 파일 이름 또는 "… (default)").</summary>
        public sealed class Profile
        {
            public int Port;
            public double[] Values;
            public string Source;
            public Profile(int port, double[] values, string source) { Port = port; Values = values; Source = source; }
        }

        public static string Write(string appName, string model, string dllFile, string prefix, string layout,
                                   IDictionary<int, double> written, IList<int> missingInputs, IList<int> missingOutputs,
                                   IList<Profile> profiles)
        {
            try
            {
                string dir = AppPaths.RunInputsDir(model);
                var inv = CultureInfo.InvariantCulture;
                var sb = new StringBuilder();
                sb.AppendLine($"# {appName} run inputs - the values the app wrote to the model DLL for this run");
                sb.AppendLine($"# time\t{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# model\t{model}");
                sb.AppendLine($"# dll\t{dllFile} (prefix {prefix})");
                sb.AppendLine($"# layout\t{layout}");
                // 첫 프로파일은 "profile", 그 밖(B그룹 rpm · torque)은 "profile (port N)"
                bool first = true;
                foreach (var p in profiles ?? new List<Profile>())
                {
                    if (p?.Values == null || p.Values.Length == 0) continue;
                    sb.AppendLine($"# {(first ? "profile" : $"profile (port {p.Port})")}\t{p.Source} | port {p.Port} every step | {p.Values.Length} points, " +
                                  $"{((p.Values.Length - 1) * ProfileFile.StepSeconds).ToString("F3", inv)} s | " +
                                  $"min {p.Values.Min().ToString("G6", inv)} | max {p.Values.Max().ToString("G6", inv)}");
                    first = false;
                }
                if (missingInputs != null && missingInputs.Count > 0)
                    sb.AppendLine($"# inputs not in DLL (value not applied)\t{string.Join(", ", missingInputs)}");
                if (missingOutputs != null && missingOutputs.Count > 0)
                    sb.AppendLine($"# outputs not in DLL (read as 0)\t{string.Join(", ", missingOutputs)}");
                sb.AppendLine("# port\tvalue");
                foreach (var kv in written.OrderBy(k => k.Key))
                    sb.AppendLine($"{kv.Key}\t{kv.Value.ToString("R", inv)}");
                string text = sb.ToString();
                string latest = Path.Combine(dir, $"{model}_latest.txt");
                File.WriteAllText(latest, text, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(dir, $"{model}_{DateTime.Now:yyyyMMdd_HHmmss}.txt"), text, new UTF8Encoding(false));
                foreach (var old in new DirectoryInfo(dir).GetFiles($"{model}_2*.txt").OrderByDescending(f => f.Name).Skip(Keep))
                {
                    try { old.Delete(); } catch { }
                }
                return latest;
            }
            catch { return null; }
        }
    }
}
