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

        public void RecordStep(int step, double[] outputValues)
        {
            if (!_isRecording) return;
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

        public void ExportToCsv(string filePath)
        {
            List<double[]> snapshot;
            List<string> headers;
            lock (_csvLock)
            {
                snapshot = new List<double[]>(_csvResults);
                headers = new List<string>(_csvHeaders);
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));
            foreach (var row in snapshot)
            {
                sb.AppendLine(string.Join(",", row.Select(v => v.ToString("G", CultureInfo.InvariantCulture))));
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
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
                            $"Step {step}에서 '{varName}' 출력이 NaN/Inf입니다.\n\n입력 변수 설정을 확인하세요.\n(파라미터 값이 해당 모델에 맞지 않을 수 있습니다)\n\n시뮬레이션을 중지합니다.",
                            "입력 변수 경고",
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
        #endregion

        protected bool LoadDll(string dllFileName, string functionPrefix, int maxInputPort, int maxOutputPort)
        {
            _dllFileName = dllFileName;
            _functionPrefix = functionPrefix;
            _maxInputPort = maxInputPort;
            _maxOutputPort = maxOutputPort;

            _hDll = LoadLibrary(dllFileName);
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
            catch (Exception ex)
            {
                Debug.WriteLine($"FreeLibrary 예외: {ex.Message}");
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
