using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HoRang2Sea.Models
{
    public class GenericPortDllModel : BaseModel
    {
        #region Run state (모델별로 구현, 2026-10-05)
        /// <summary>계산 스레드가 살아 있는지(실행 또는 일시정지). 탭 · 창을 닫을 때, 레이아웃을 바꿀 때 확인한다.</summary>
        public virtual bool IsSimulationActive => false;

        /// <summary>계산을 멈추고 DLL 을 내린다. keepResults = true 면 멈춘 위치(Step)와 마지막 출력값을 남긴다
        /// (Stop 버튼 · 탭 닫기 — 이전에는 Stop 이 차트 · 진행 · 출력값을 모두 0으로 지웠다). 새 Run 은 0 step 부터 다시 시작한다.</summary>
        public virtual void StopCalculation(bool keepResults = false) { }
        #endregion

        #region CSV Result Recording
        private readonly object _csvLock = new();
        private List<double[]> _csvResults = new();
        private List<string> _csvHeaders = new();
        private bool _isRecording = false;

        public void StartRecording(List<string> outputNames)
        {
            lock (_csvLock)
            {
                _csvHeaders = new List<string> { "Step" };
                _csvHeaders.AddRange(outputNames);
                _csvResults = new List<double[]>();
                _isRecording = true;
            }
        }

        /// <summary>
        /// 결과 기록 간격(시뮬 step). DLL step = 1 ms 이므로 100 = 0.1초.
        /// 차트는 이 간격으로만 그리고 CSV도 이 간격이 최소 해상도이므로 전 step을 보관할 필요가 없다.
        /// AppSettings 에서 변경 가능.
        /// </summary>
        public static int RecordStepInterval = 100;

        public void RecordStep(int step, double[] outputValues)
        {
            if (!_isRecording) return;
            int iv = RecordStepInterval < 1 ? 1 : RecordStepInterval;
            if (step % iv != 0) return;   // 기본 0.1초(100 step) 간격만 보관
            var row = new double[outputValues.Length + 1];
            row[0] = step;
            Array.Copy(outputValues, 0, row, 1, outputValues.Length);
            lock (_csvLock) { _csvResults.Add(row); }
        }

        public void StopRecording()
        {
            _isRecording = false;
        }

        public bool HasRecordedData
        {
            get { lock (_csvLock) { return _csvResults.Count > 0; } }
        }

        /// <summary>기록된 변수명 헤더 목록 (Step 컬럼 포함). CsvExportOptionsDialog의 선택 UI에서 사용.</summary>
        public IReadOnlyList<string> RecordedHeaders
        {
            get { lock (_csvLock) { return new List<string>(_csvHeaders); } }
        }

        /// <summary>기록 버퍼에서 변수의 시계열을 라이브 차트와 동일 cadence(매 100스텝, x=step*0.001)로 반환.
        /// 시뮬 후/중에 임의 변수를 차트에 backfill(되채움)하기 위함. 데이터 없거나 변수 없으면 빈 리스트.</summary>
        public List<(double x, double y)> GetRecordedSeries(string varName)
        {
            var result = new List<(double x, double y)>();
            if (string.IsNullOrEmpty(varName)) return result;
            lock (_csvLock)
            {
                int idx = _csvHeaders.IndexOf(varName);
                if (idx <= 0) return result;   // 0=Step, -1=없음
                foreach (var row in _csvResults)
                {
                    if (row.Length <= idx) continue;
                    int step = (int)row[0];
                    // 기록 자체가 RecordStepInterval 간격이라 추가 필터는 불필요하지만,
                    // 과거 저장분/설정 변경 직후를 대비해 한 번 더 거른다.
                    int ivc = RecordStepInterval < 1 ? 1 : RecordStepInterval;
                    if (step % ivc != 0) continue;
                    result.Add((step * 0.001, row[idx]));
                }
            }
            return result;
        }

        // CSV 저장 다이얼로그의 마지막 사용 폴더 (기본값 = 문서 폴더 — 설치 폴더(Program Files)는 쓰기가 막힐 수 있다). Export 성공 시 갱신.
        public static string LastExportDirectory = HoRang2Sea.Services.AppPaths.DefaultExportDir;

        public void ExportToCsv(string filePath)
        {
            ExportToCsv(filePath, 1, -1, -1, null);
        }

        public void ExportToCsv(string filePath, int stepInterval, int startStep, int endStep)
        {
            ExportToCsv(filePath, stepInterval, startStep, endStep, null);
        }

        /// <summary>
        /// Export with sampling, time range and optional variable selection.
        /// </summary>
        /// <param name="selectedVarNames">저장할 변수 이름 목록. null/empty면 전체 저장. Step 컬럼은 항상 포함.</param>
        public void ExportToCsv(string filePath, int stepInterval, int startStep, int endStep, IList<string> selectedVarNames)
        {
            if (stepInterval < 1) stepInterval = 1;

            List<double[]> snapshot;
            List<string> headers;
            lock (_csvLock)
            {
                snapshot = new List<double[]>(_csvResults);
                headers = new List<string>(_csvHeaders);
            }

            var selIndices = new List<int> { 0 };
            var selHeaders = new List<string> { headers[0] };
            if (selectedVarNames != null && selectedVarNames.Count > 0)
            {
                for (int i = 1; i < headers.Count; i++)
                {
                    if (selectedVarNames.Contains(headers[i]))
                    {
                        selIndices.Add(i);
                        selHeaders.Add(headers[i]);
                    }
                }
            }
            else
            {
                for (int i = 1; i < headers.Count; i++)
                {
                    selIndices.Add(i);
                    selHeaders.Add(headers[i]);
                }
            }

            // 전체 CSV를 메모리에 쌓지 않고 파일로 한 줄씩 스트리밍 기록 (대용량 기록 시 OutOfMemory 방지)
            // 열: Step, Time_s(= Step × 0.001, DLL step 1 ms), 선택한 출력들
            // 간격은 시작 이후 첫 기록 행부터 센다(시작 step 이 기록 간격의 배수가 아니면 이전에는 빈 CSV 가 나왔다)
            int baseStep = -1;
            var outHeaders = new List<string>(selHeaders);
            outHeaders.Insert(1, "Time_s");
            var fields = new string[selIndices.Count + 1];
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8, 1 << 20))
            {
                writer.WriteLine(string.Join(",", outHeaders));
                for (int i = 0; i < snapshot.Count; i++)
                {
                    int step = (int)snapshot[i][0];
                    if (startStep >= 0 && step < startStep) continue;
                    if (endStep >= 0 && step > endStep) break;
                    if (baseStep < 0) baseStep = step;
                    if ((step - baseStep) % stepInterval != 0) continue;
                    var row = snapshot[i];
                    fields[0] = row[0].ToString("G", CultureInfo.InvariantCulture);
                    fields[1] = (row[0] * 0.001).ToString("0.###", CultureInfo.InvariantCulture);
                    for (int j = 1; j < selIndices.Count; j++)
                        fields[j + 1] = row[selIndices[j]].ToString("G", CultureInfo.InvariantCulture);
                    writer.WriteLine(string.Join(",", fields));
                }
            }
        }

        public int RecordedStepCount
        {
            get { lock (_csvLock) { return _csvResults.Count; } }
        }
        #endregion

        #region NaN Detection
        private bool _nanWarningShown = false;
        public bool NaNDetected { get; private set; } = false;
        /// <summary>NaN/Inf 가 처음 나온 step(멈춘 위치). 없으면 -1.</summary>
        public int NaNStep { get; private set; } = -1;

        protected bool CheckNaN(double[] outputValues, List<string> outputNames, int step)
        {
            if (_nanWarningShown) return true;
            List<string> bad = null;
            for (int i = 0; i < outputValues.Length; i++)
            {
                if (double.IsNaN(outputValues[i]) || double.IsInfinity(outputValues[i]))
                    (bad ??= new List<string>()).Add(i < outputNames.Count ? outputNames[i] : $"Out[{i}]");
            }
            if (bad == null) return false;

            _nanWarningShown = true;
            NaNDetected = true;
            NaNStep = step;
            string names = string.Join(", ", bad.Take(8)) + (bad.Count > 8 ? $" … (+{bad.Count - 8} more)" : "");
            string file = LastRunInputFile;
            System.Windows.Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                System.Windows.MessageBox.Show(
                    System.Windows.Application.Current.MainWindow,
                    $"The model output became NaN/Inf at t = {step * 0.001:F3} s (step {step}).\n\nOutputs: {names}\n\n" +
                    "The simulation stopped here. Results up to this point are kept - you can view them in the charts and export them to CSV.\n\n" +
                    "If the inputs are within the workbook ranges and the profile is reasonable, please report this time to the model provider" +
                    (file != null ? $" together with the run-input file:\n{file}" : "."),
                    "Simulation stopped (NaN/Inf)",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }));
            return true;
        }

        protected void ResetNaNWarning()
        {
            _nanWarningShown = false;
            NaNDetected = false;
            NaNStep = -1;
        }
        #endregion

        #region Run input log · missing ports (2026-10-05)
        /// <summary>이번 Run 의 프로파일 출처(파일 이름 또는 "… (default)"). VM 이 Calculate 전에 넣는다.</summary>
        public string ProfileSource { get; set; } = "";
        /// <summary>마지막 Run 의 실행 입력 기록 파일(%LOCALAPPDATA%\HoRang2\RunInputs\{model}\{model}_latest.txt).</summary>
        public string LastRunInputFile { get; private set; }
        private Dictionary<int, double> _logInputs;
        private static readonly HashSet<string> _missingPortWarned = new();

        /// <summary>InitValue 에서 입력을 쓰기 시작할 때 부른다. 이후 SetInputPort 로 쓴 값이 기록된다.</summary>
        protected void BeginInputLog() => _logInputs = new Dictionary<int, double>();

        /// <summary>B그룹 작업 rpm · torque 처럼 속도 외에 매 step 넣는 프로파일의 출처(포트 → 파일 이름). VM 이 Calculate 전에 넣는다.</summary>
        public Dictionary<int, string> ExtraProfileSources { get; } = new();

        /// <summary>InitValue 끝에서 부른다. 앱이 DLL 에 쓴 값과 DLL 에 없는 포트를 파일로 남기고,
        /// 없는 포트(0이 아닌 값을 쓰려던 입력, 읽으려는 출력)가 있으면 DLL 마다 한 번 알린다.
        /// extraProfiles: 속도 말고도 매 step 넣는 프로파일(포트, 값) — 출처는 ExtraProfileSources.</summary>
        protected void EndInputLog(string appName, string model, string layout, IEnumerable<int> outputPortsUsed, int profilePort, double[] profile,
                                   params (int Port, double[] Values)[] extraProfiles)
        {
            var written = _logInputs ?? new Dictionary<int, double>();
            _logInputs = null;
            var profiles = new List<HoRang2Sea.Services.RunInputLog.Profile> { new(profilePort, profile, ProfileSource) };
            foreach (var x in extraProfiles)
                profiles.Add(new(x.Port, x.Values, ExtraProfileSources.TryGetValue(x.Port, out var src) ? src : "-"));
            var missIn = written.Where(kv => !_inputPorts.ContainsKey(kv.Key) && kv.Value != 0).Select(kv => kv.Key).ToList();
            foreach (var p in profiles)
                if (p.Port > 0 && p.Values != null && p.Values.Any(v => v != 0) && !_inputPorts.ContainsKey(p.Port)) missIn.Add(p.Port);
            missIn = missIn.Distinct().OrderBy(p => p).ToList();
            var missOut = outputPortsUsed.Distinct().Where(p => !_outputPorts.ContainsKey(p)).OrderBy(p => p).ToList();
            LastRunInputFile = HoRang2Sea.Services.RunInputLog.Write(appName, model, _dllFileName, _functionPrefix, layout,
                                                                     written, missIn, missOut, profiles);
            if ((missIn.Count > 0 || missOut.Count > 0) && _missingPortWarned.Add(_dllFileName ?? model))
            {
                System.Windows.MessageBox.Show(
                    $"The model DLL '{_dllFileName}' does not have some ports the app uses.\n\n" +
                    (missIn.Count > 0 ? $"Inputs (values not applied): {string.Join(", ", missIn)}\n" : "") +
                    (missOut.Count > 0 ? $"Outputs (shown as 0): {string.Join(", ", missOut)}\n" : "") +
                    "\nThe run continues. Please send this to the model provider" +
                    (LastRunInputFile != null ? $" together with the run-input file:\n{LastRunInputFile}" : "."),
                    "Model ports", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        /// <summary>DLL 을 못 열었을 때 — 상태줄만으로는 지나치기 쉬워 창으로 알린다.</summary>
        protected static void ShowDllLoadFailed(string dllFile, string model)
        {
            System.Windows.MessageBox.Show(
                $"The model DLL could not be loaded, so the simulation was not started.\n\nModel: {model}\nFile: ModelDLLs\\{dllFile}\n\n" +
                "Reinstall the program, or check that the file exists and is the 64-bit Release build from the model provider.",
                "Model DLL", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        #endregion

        #region Kernel32 Imports
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        #endregion

        #region Delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidDelegate();

        private VoidDelegate _initializeFunc;
        private VoidDelegate _stepFunc;
        private VoidDelegate _terminateFunc;
        #endregion

        #region DLL State
        private IntPtr _hDll = IntPtr.Zero;
        private string _dllFileName;
        private string _functionPrefix;
        private Dictionary<int, IntPtr> _inputPorts = new();
        private Dictionary<int, IntPtr> _outputPorts = new();
        private int _maxInputPort;
        private int _maxOutputPort;
        private string _tempDllPath;   // 인스턴스별 유니크 복사본 경로(null이면 원본 직접 로드)
        private static readonly object _tempDirLock = new();
        private static string _dllTempDir;
        private static bool _tempCleaned;
        #endregion

        // 유니크 DLL 복사가 들어갈 temp 폴더. 프로그램 폴더 안(dll_temp), 쓰기 불가 시 시스템 temp로 폴백.
        private static string GetDllTempDir()
        {
            lock (_tempDirLock)
            {
                if (_dllTempDir != null) return _dllTempDir;
                string dir;
                try
                {
                    dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dll_temp");
                    System.IO.Directory.CreateDirectory(dir);
                    string test = System.IO.Path.Combine(dir, ".w");
                    System.IO.File.WriteAllText(test, "x");
                    System.IO.File.Delete(test);
                }
                catch
                {
                    dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoRang2_dll_temp");
                    try { System.IO.Directory.CreateDirectory(dir); } catch { }
                }
                if (!_tempCleaned)   // 이전 비정상 종료 잔여물 청소
                {
                    _tempCleaned = true;
                    try { foreach (var f in System.IO.Directory.GetFiles(dir, "*.dll")) { try { System.IO.File.Delete(f); } catch { } } }
                    catch { }
                }
                _dllTempDir = dir;
                return dir;
            }
        }

        // 원본(ModelDLLs/{dll})을 유니크 이름으로 복사해 로드 → In/Out 전역변수 격리(같은 모델 2개 동시 실행 충돌 방지).
        // 실패하면 _hDll=Zero 유지 → 호출부가 기존 방식으로 폴백(무해).
        private void TryLoadUniqueCopy(string dllFileName, string functionPrefix)
        {
            try
            {
                string original = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModelDLLs", dllFileName);
                if (!System.IO.File.Exists(original)) return;
                string tempPath = System.IO.Path.Combine(GetDllTempDir(), $"{functionPrefix}_{System.Guid.NewGuid():N}.dll");
                System.IO.File.Copy(original, tempPath, true);
                var h = LoadLibrary(tempPath);
                if (h != IntPtr.Zero) { _hDll = h; _tempDllPath = tempPath; Debug.WriteLine($"DLL 유니크 복사 로드: {tempPath}"); }
                else { try { System.IO.File.Delete(tempPath); } catch { } }
            }
            catch (Exception ex) { Debug.WriteLine($"유니크 DLL 복사 실패(폴백): {ex.Message}"); }
        }

        protected bool LoadDll(string dllFileName, string functionPrefix, int maxInputPort, int maxOutputPort)
        {
            _dllFileName = dllFileName;
            _functionPrefix = functionPrefix;
            _maxInputPort = maxInputPort;
            _maxOutputPort = maxOutputPort;
            _tempDllPath = null;

            TryLoadUniqueCopy(dllFileName, functionPrefix);
            if (_hDll == IntPtr.Zero)
                _hDll = LoadLibrary(dllFileName);
            if (_hDll == IntPtr.Zero)
            {
                // Fallback: ModelDLLs/ 하위 폴더에서 시도 (작업 디렉터리에 없을 때)
                var subPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModelDLLs", dllFileName);
                if (System.IO.File.Exists(subPath))
                {
                    _hDll = LoadLibrary(subPath);
                    if (_hDll != IntPtr.Zero)
                    {
                        Debug.WriteLine($"DLL LoadLibrary 완료 (ModelDLLs/): {subPath} -> {_hDll}");
                    }
                }
            }
            if (_hDll == IntPtr.Zero)
            {
                Debug.WriteLine($"DLL LoadLibrary 실패: {dllFileName} (Error: {Marshal.GetLastWin32Error()})");
                return false;
            }
            Debug.WriteLine($"DLL LoadLibrary 완료: {dllFileName} -> {_hDll}");

            // Resolve functions
            var initPtr = GetProcAddress(_hDll, $"{functionPrefix}_initialize");
            var stepPtr = GetProcAddress(_hDll, $"{functionPrefix}_step");
            var termPtr = GetProcAddress(_hDll, $"{functionPrefix}_terminate");

            if (initPtr == IntPtr.Zero || stepPtr == IntPtr.Zero || termPtr == IntPtr.Zero)
            {
                Debug.WriteLine($"DLL 함수 탐색 실패: init={initPtr}, step={stepPtr}, term={termPtr}");
                return false;
            }

            _initializeFunc = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(initPtr);
            _stepFunc = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(stepPtr);
            _terminateFunc = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(termPtr);

            // Resolve input ports
            // 포트 심볼은 보통 In1/Out1 이지만, 충남대 2026-09-15 재빌드(시외버스·트랙터)처럼
            // 접두어가 붙어 Tractor_DLL_In1 로 나오는 DLL 도 있다 → 둘 다 찾는다(세 앱 공통, 2026-10-05).
            _inputPorts.Clear();
            for (int i = 1; i <= maxInputPort; i++)
            {
                var ptr = GetProcAddress(_hDll, $"In{i}");
                if (ptr == IntPtr.Zero)
                    ptr = GetProcAddress(_hDll, $"{functionPrefix}_In{i}");
                if (ptr != IntPtr.Zero)
                    _inputPorts[i] = ptr;
            }

            // Resolve output ports
            _outputPorts.Clear();
            for (int i = 1; i <= maxOutputPort; i++)
            {
                var ptr = GetProcAddress(_hDll, $"Out{i}");
                if (ptr == IntPtr.Zero)
                    ptr = GetProcAddress(_hDll, $"{functionPrefix}_Out{i}");
                if (ptr != IntPtr.Zero)
                    _outputPorts[i] = ptr;
            }

            Debug.WriteLine($"포트 탐색 완료: In={_inputPorts.Count}, Out={_outputPorts.Count}");
            return true;
        }

        protected void CallInitialize()
        {
            _initializeFunc?.Invoke();
        }

        protected void CallStep()
        {
            _stepFunc?.Invoke();
        }

        protected void CallTerminate()
        {
            try
            {
                _terminateFunc?.Invoke();
                Debug.WriteLine("DLL terminate() 완료");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DLL terminate() 예외: {ex.Message}");
            }
        }

        protected void SetInputPort(int port, double value)
        {
            if (_logInputs != null) _logInputs[port] = value;   // 실행 입력 기록(InitValue 동안만)
            if (_inputPorts.TryGetValue(port, out var ptr))
            {
                var bytes = BitConverter.GetBytes(value);
                Marshal.Copy(bytes, 0, ptr, 8);
            }
        }

        protected double GetOutputPort(int port)
        {
            if (_outputPorts.TryGetValue(port, out var ptr))
            {
                var bytes = new byte[8];
                Marshal.Copy(ptr, bytes, 0, 8);
                return BitConverter.ToDouble(bytes, 0);
            }
            return 0.0;
        }

        protected double GetInputPort(int port)
        {
            if (_inputPorts.TryGetValue(port, out var ptr))
            {
                var bytes = new byte[8];
                Marshal.Copy(ptr, bytes, 0, 8);
                return BitConverter.ToDouble(bytes, 0);
            }
            return 0.0;
        }

        protected void UnloadDll()
        {
            if (_dllFileName == null) return;
            try
            {
                Thread.Sleep(50);
                if (_tempDllPath != null)
                {
                    // 유니크 복사본: 핸들로 직접 FreeLibrary (한 번 로드라 1회면 충분)
                    if (_hDll != IntPtr.Zero) FreeLibrary(_hDll);
                    Debug.WriteLine($"DLL(유니크 복사) FreeLibrary 완료");
                }
                else
                {
                    int freeCount = 0;
                    IntPtr hModule = GetModuleHandle(_dllFileName);
                    while (hModule != IntPtr.Zero && freeCount < 10)
                    {
                        FreeLibrary(hModule);
                        freeCount++;
                        Thread.Sleep(10);
                        hModule = GetModuleHandle(_dllFileName);
                    }
                    Debug.WriteLine($"DLL FreeLibrary 완료 ({freeCount}회)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FreeLibrary 예외: {ex.Message}");
            }
            // 임시 복사본 삭제 (best-effort; 실패해도 다음 시작 시 청소됨)
            if (_tempDllPath != null)
            {
                try { Thread.Sleep(20); System.IO.File.Delete(_tempDllPath); } catch { }
                _tempDllPath = null;
            }
            _hDll = IntPtr.Zero;
            _initializeFunc = null;
            _stepFunc = null;
            _terminateFunc = null;
            _inputPorts.Clear();
            _outputPorts.Clear();
        }

        protected bool IsDllLoaded => _hDll != IntPtr.Zero;

        /// <summary>
        /// Set a specific input port value directly. Used by ViewModel to pass GUI-edited values.
        /// </summary>
        public void SetInputPortPublic(int port, double value)
        {
            SetInputPort(port, value);
        }
    }
}
