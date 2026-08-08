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

        // CSV 저장 다이얼로그의 마지막 사용 폴더 (기본값 = exe 폴더). Export 성공 시 갱신.
        public static string LastExportDirectory = AppDomain.CurrentDomain.BaseDirectory;

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
            int baseStep = startStep < 0 ? 0 : startStep;
            var fields = new string[selIndices.Count];
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8, 1 << 20))
            {
                writer.WriteLine(string.Join(",", selHeaders));
                for (int i = 0; i < snapshot.Count; i++)
                {
                    int step = (int)snapshot[i][0];
                    if (startStep >= 0 && step < startStep) continue;
                    if (endStep >= 0 && step > endStep) break;
                    if ((step - baseStep) % stepInterval != 0) continue;
                    var row = snapshot[i];
                    for (int j = 0; j < selIndices.Count; j++)
                        fields[j] = row[selIndices[j]].ToString("G", CultureInfo.InvariantCulture);
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

        protected bool CheckNaN(double[] outputValues, List<string> outputNames, int step)
        {
            if (_nanWarningShown) return true;
            for (int i = 0; i < outputValues.Length; i++)
            {
                if (double.IsNaN(outputValues[i]) || double.IsInfinity(outputValues[i]))
                {
                    _nanWarningShown = true;
                    NaNDetected = true;
                    string varName = (i < outputNames.Count) ? outputNames[i] : $"Out[{i}]";
                    System.Windows.Application.Current?.Dispatcher.Invoke(new Action(() =>
                    {
                        System.Windows.MessageBox.Show(
                            System.Windows.Application.Current.MainWindow,
                            $"Output '{varName}' is NaN/Inf at step {step}.\n\nPlease check the input variable settings.\n(The parameter values may not be appropriate for this model.)\n\nThe simulation will be stopped.",
                            "Input Variable Warning",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }));
                    return true;
                }
            }
            return false;
        }

        protected void ResetNaNWarning()
        {
            _nanWarningShown = false;
            NaNDetected = false;
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
            _inputPorts.Clear();
            for (int i = 1; i <= maxInputPort; i++)
            {
                var ptr = GetProcAddress(_hDll, $"In{i}");
                if (ptr != IntPtr.Zero)
                    _inputPorts[i] = ptr;
            }

            // Resolve output ports
            _outputPorts.Clear();
            for (int i = 1; i <= maxOutputPort; i++)
            {
                var ptr = GetProcAddress(_hDll, $"Out{i}");
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
    }
}
