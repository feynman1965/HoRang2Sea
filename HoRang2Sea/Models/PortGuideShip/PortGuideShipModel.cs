using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;

namespace HoRang2Sea.Models
{
    public class PortGuideShipMWDataModel
    {
        public PortGuideShipMWDataModel() { }
        public PortGuideShipMWDataModel(string name) { Name = name; }
        public PortGuideShipMWDataModel(string name, string unit, string parent) { Name = name; Unit = unit; Parent = parent; }

        public Subject<double> SubjectValue = new Subject<double>();
        private double _value;

        public string Name { get; set; }
        public double Value { get => _value; set { _value = value; SubjectValue.OnNext(value); } }
        public string Unit { get; set; }
        public string Parent { get; set; }
    }

    public class PortGuideShipMWModel : GenericPortDllModel { }

    public class PortGuideShipMW : PortGuideShipMWModel
    {
        public Thread CalculateThread { get; set; }
        public bool IsinitValue { get; set; }
        public int Step { get; set; }
        public bool IsPause { get; set; }
        public static ManualResetEvent manualEvent = new ManualResetEvent(true);
        private double[] _driveModes;
        private CancellationTokenSource _cancellationTokenSource;

        private const string DLL_FILE_NAME = "Electric_2MW_250703_hydro_win64.dll";
        private const string FUNCTION_PREFIX = "Electric_2MW_250703_hydro";
        private const int MAX_INPUT_PORT = 65;
        private const int MAX_OUTPUT_PORT = 55;

        // Input index -> port number mapping (sequential 1-65)
        private static readonly Dictionary<int, int> InputPortMap = new()
        {
            // XML 컬럼 순서 → 실제 DLL 포트 (FGS.xlsx 권위 매핑 + ctypes 직접 검증 기준).
            // idx0=Propellerpitch → In63 (이 DLL에서 무효 포트), idx1=Slip = 속도 프로파일 → In64 (DM_Speed_Profile, 매 step 주입).
            // idx2~63 = 물리 파라미터(Ambient~Intercooler Area) → In1~In62 (Excel 포트와 1:1).
            { 0, 63 }, { 1, 64 },
            { 2, 1 }, { 3, 2 }, { 4, 3 }, { 5, 4 }, { 6, 5 }, { 7, 6 }, { 8, 7 }, { 9, 8 }, { 10, 9 }, { 11, 10 },
            { 12, 11 }, { 13, 12 }, { 14, 13 }, { 15, 14 }, { 16, 15 }, { 17, 16 }, { 18, 17 }, { 19, 18 }, { 20, 19 }, { 21, 20 },
            { 22, 21 }, { 23, 22 }, { 24, 23 }, { 25, 24 }, { 26, 25 }, { 27, 26 }, { 28, 27 }, { 29, 28 }, { 30, 29 }, { 31, 30 },
            { 32, 31 }, { 33, 32 }, { 34, 33 }, { 35, 34 }, { 36, 35 }, { 37, 36 }, { 38, 37 }, { 39, 38 }, { 40, 39 }, { 41, 40 },
            { 42, 41 }, { 43, 42 }, { 44, 43 }, { 45, 44 }, { 46, 45 }, { 47, 46 }, { 48, 47 }, { 49, 48 }, { 50, 49 }, { 51, 50 },
            { 52, 51 }, { 53, 52 }, { 54, 53 }, { 55, 54 }, { 56, 55 }, { 57, 56 }, { 58, 57 }, { 59, 58 }, { 60, 59 }, { 61, 60 },
            { 62, 61 }, { 63, 62 },
            // Layout: Mode/Design는 이 DLL에 포트 없어 silent no-op, Control_Layout만 port 65(DM_Control_mode)에 실제 매핑.
            { 64, 482 }, // Mode (port 482 not in DLL — silent no-op)
            { 65, 483 }, // Design_Layout (port 483 not in DLL — silent no-op)
            { 66, 65 }   // Control_Layout (port 65 = DM_Control_mode)
        };

        // Output index -> port number mapping (sequential 1-55)
        private static readonly Dictionary<int, int> OutputPortMap = new()
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 },
            { 10, 11 }, { 11, 12 }, { 12, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 }, { 16, 17 }, { 17, 18 }, { 18, 19 },
            { 19, 20 }, { 20, 21 }, { 21, 22 }, { 22, 23 }, { 23, 24 }, { 24, 25 }, { 25, 26 }, { 26, 27 }, { 27, 28 },
            { 28, 29 }, { 29, 30 }, { 30, 31 }, { 31, 32 }, { 32, 33 }, { 33, 34 }, { 34, 35 }, { 35, 36 }, { 36, 37 },
            { 37, 38 }, { 38, 39 }, { 39, 40 }, { 40, 41 }, { 41, 42 }, { 42, 43 }, { 43, 44 }, { 44, 45 }, { 45, 46 },
            { 46, 47 }, { 47, 48 }, { 48, 49 }, { 49, 50 }, { 50, 51 }, { 51, 52 }, { 52, 53 }, { 53, 54 }, { 54, 55 }
        };

        // Port initial values (from PGS_initial_value.txt: port_number -> value)
        private static readonly Dictionary<int, double> _defaultInputValues = new()
        {
            { 1, 298.15 }, { 2, 0.583 }, { 3, 343.15 }, { 4, 0.195 }, { 5, 0.195 },
            { 6, 0.0025 }, { 7, 0.000042 }, { 8, 0.0002 }, { 9, 0.003 }, { 10, 0.001 },
            { 11, 0.001 }, { 12, 0.002 }, { 13, 0.001 }, { 14, 32.0 }, { 15, 380.0 },
            { 16, 404.0 }, { 17, 1.5 }, { 18, 2.5 }, { 19, 0.0415 }, { 20, 0.0165 },
            { 21, 0.0145 }, { 22, 0.0173 }, { 23, 0.00005 }, { 24, 28.0 }, { 25, 40.0 },
            { 26, 345.0 }, { 27, 101325.0 }, { 28, 298.15 }, { 29, 0.5 }, { 30, 90.0 },
            { 31, 0.11 }, { 32, 0.0037 }, { 33, 0.11 }, { 34, 0.078 }, { 35, 0.007 },
            { 36, 6.0 }, { 37, 0.06 }, { 38, 0.0006 }, { 39, 0.0508 }, { 40, 0.2488 },
            { 41, 0.002 }, { 42, 0.099 }, { 43, 0.0009 }, { 44, 0.0001 }, { 45, 13000.0 },
            { 46, 0.254 }, { 47, 0.8 }, { 48, 1.0 }, { 49, 2.9 }, { 50, 16.0 },
            { 51, 0.61 }, { 52, 1.0 }, { 53, 2.9 }, { 54, 34.0 }, { 55, 25.0 },
            { 56, 45.0 }, { 57, 2100.0 }, { 58, 65.0 }, { 59, 0.1 }, { 60, 0.5 },
            { 61, 298.15 }, { 62, 1.5 }, { 63, 25.0 }, { 64, 0.01 }, { 65, 1.0 }
        };

        public List<PortGuideShipMWDataModel> PortGuideShipMWInputs = new()
        {
            //mode  ( 개수 : 2 )
            new("Propeller pitch"),
            new("Slip"),

            //Stack  ( 개수 : 18 )
            new("Ambient temperature"),
            new("Stack channel length"),
            new("Stack initial temperature"),
            new("Length of cell"),
            new("Width of cell"),
            new("Membrane thickness of cell"),
            new("Catalyst thickness of cell"),
            new("GDL thickness of cell"),
            new("Bipolar plates thickness of cell"),
            new("Gas channel depth of cell"),
            new("Gas channel width of cell"),
            new("Channel pitch of cell"),
            new("Channel thickness of cell"),
            new("Channel number of cell"),
            new("Active area of cell"),
            new("Number of cell"),
            new("Anode stoichiometry"),
            new("Cathode stoichiometry"),

            //Blower  ( 개수 : 20 )
            new("Inducer tip inlet diameter"),
            new("Hub inlet diameter"),
            new("Impeller inlet width"),
            new("Impeller inlet tip diameter"),
            new("Roughness"),
            new("Blade inlet angle"),
            new("Blade exit angle"),
            new("Inlet stagnation sonic velocity"),
            new("Inlet pressure"),
            new("Inlet temperature"),
            new("Inlet relative humidity"),
            new("Alpha2b"),
            new("Length of compressor"),
            new("Plenum volume"),
            new("Diffuser inlet diameter"),
            new("Impeller outlet tip diameter"),
            new("Impeller outlet width"),
            new("Number of impeller blades"),
            new("Impeller inlet length"),
            new("Valve opening coefficient"),

            //AirHumidifer  ( 개수 : 8 )
            new("Air humidifier manifold inlet diameter"),
            new("Air humidifier shell diameter"),
            new("Air humidifier wall thickness"),
            new("Air humidifier manifold length"),
            new("Air humidifier membrane inner diameter"),
            new("Air humidifier membrane thickness"),
            new("Number of humidifier membrane in cathode"),
            new("Air humidifier membrane length"),

            //CleanWater  ( 개수 : 4 )
            new("Fresh water pump efficiency"),
            new("Fresh water pump inlet pressure"),
            new("Fresh water pump outlet pressure"),
            new("Fresh water pump maximum flow rate"),

            //SeaWater  ( 개수 : 5 )
            new("Sea water pump efficiency"),
            new("Sea water pump inlet pressure"),
            new("Sea water pump outlet pressure"),
            new("Sea water pump maximum flow rate"),
            new("Sea water temperature"),

            //HeatExchanger  ( 개수 : 3 )
            new("Heat exchanger active area"),
            new("Heat exchanger overall heat transfer coefficient"),
            new("Heat exchanger temperature setting"),

            //Intercooler  ( 개수 : 4 )
            new("Intercooler inlet coolant mass flow rate"),
            new("Intercooler inlet relative humidity"),
            new("Intercooler inlet coolant temperature"),
            new("Intercooler area"),

            //Layout (Sea DLL은 mode2/port 65 단일 layout만 활성화. Mode/Design은 시각용)
            new("Mode"),
            new("Design_Layout"),
            new("Control_Layout"),
        };

        public List<PortGuideShipMWDataModel> PortGuideShipMWOuts = new()
        {
            //PortGuideShipMode  ( 개수 : 3 )

            //Stack  ( 개수 : 5 )
            new("Inlet current density", "A/cm2", "Stack"),
            new("Stack voltage", "V", "Stack"),
            new("Stack net power", "kW", "Stack"),
            new("Stack OCV", "V", "Stack"),
            new("Stack temperature", "K", "Stack"),

            //Blower  ( 개수 : 4 )
            new("Compressor torque", "Nm", "Blower"),
            new("Compressor efficiency", "-", "Blower"),
            new("Compressor air mass flow rate", "kg/s", "Blower"),
            new("Plenum downstream pressure", "Pa", "Blower"),

            //Air Humidifier  ( 개수 : 4 )
            new("Total humidified air mass flow rate", "kg/s", "Air Humidifier"),
            new("Cathode gas temperature", "K", "Air Humidifier"),
            new("Cathode gas total pressure", "Pa", "Air Humidifier"),
            new("Air humidifier outlet relative humidity", "-", "Air Humidifier"),

            //Hydrogen Humidifier  ( 개수 : 4 )
            new("Anode inlet mass flow rate", "kg/s", "Hydrogen Humidifier"),
            new("Hydrogen humidifier water mole fraction", "-", "Hydrogen Humidifier"),
            new("Anode gas temperature", "K", "Hydrogen Humidifier"),
            new("Anode gas total pressure", "Pa", "Hydrogen Humidifier"),

            //Fresh Water Pump  ( 개수 : 3 )
            new("Fresh water pump cooling water flow rate", "kg/s", "Fresh Water Pump"),
            new("Fresh water pump cooling water temperature", "K", "Fresh Water Pump"),
            new("Fresh water pump coolant outlet pressure", "bar", "Fresh Water Pump"),

            //Sea Water Pump  ( 개수 : 3 )
            new("Sea water pump sea water flow rate", "kg/s", "Sea Water Pump"),
            new("Sea water pump sea water temperature", "K", "Sea Water Pump"),
            new("Sea water pump sea water outlet pressure", "bar", "Sea Water Pump"),

            //Valve  ( 개수 : 14 )
            new("Valve opening ratio", "-", "Valve"),
            new("Bypass coolant mass flow rate", "kg/s", "Valve"),
            new("Bypass coolant temperature", "K", "Valve"),
            new("Bypass coolant inlet pressure", "kPa", "Valve"),
            new("heatexchanger coolant mass flow rate", "kg/s", "Valve"),
            new("heatexchanger coolant temperature", "K", "Valve"),
            new("heatexchanger coolant inlet pressure", "kPa", "Valve"),
            new("Stack valve opening ratio", "-", "Valve"),
            new("Stack bypass coolant flow rate", "kg/s", "Valve"),
            new("Stack bypass coolant temperature", "K", "Valve"),
            new("Stack bypass coolant inlet pressure", "kPa", "Valve"),
            new("Stack coolant flow rate", "kg/s", "Valve"),
            new("Stack coolant temperature", "K", "Valve"),
            new("Stack coolant pressure", "kPa", "Valve"),

            //Heat Exchanger  ( 개수 : 2 )
            new("Heat exchanger coolant outlet temperature", "\u2103", "Heat Exchanger"),
            new("Heat exchanger sea water outlet temperature", "\u2103", "Heat Exchanger"),

            //Induction Motor  ( 개수 : 2 )
            new("Motor RPM", "Nm", "Induction Motor"),
            new("Electric power", "kW", "Induction Motor"),

            //Converter  ( 개수 : 3 )
            new("Duty ratio", "-", "Converter"),
            new("Converter voltage", "V", "Converter"),
            new("Converter current", "I", "Converter"),

            //Intercooler  ( 개수 : 4 )
            new("Intercooler outlet air mass flow rate", "kg/s", "Intercooler"),
            new("Intercooler outlet air temperature", "K", "Intercooler"),
            new("Intercooler outlet pressure", "Pa", "Intercooler"),
            new("Intercooler outlet relative humidity", "-", "Intercooler"),

            //Battery  ( 개수 : 4 )
            new("Battery SOC(State Of Charge)", "-", "Battery"),
            new("Battery output voltage", "V", "Battery"),
            new("Battery output current", "A", "Battery"),
            new("Battery output power", "kW", "Battery"),
        };

        public PortGuideShipMW()
        {
            _cancellationTokenSource = null;
            IsinitValue = false;
            IsPause = false;
            Step = 0;
            ResetNaNWarning();
        }

        public void SetDriveModes(double[] driveModes) => _driveModes = driveModes;

        public int DesignLayout { get; set; } = 0;
        public int ControlLayout { get; set; } = 0;

        public void RunWithCancellation(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("PortGuideShip 모델 시작");
                int Maxiterations = _driveModes != null ? _driveModes.Length : 100000;
                WaitHandle[] waitHandles = new WaitHandle[] { token.WaitHandle, manualEvent };

                var outputNames = PortGuideShipMWOuts.Select(o => o.Name).ToList();
                var outputValues = new double[PortGuideShipMWOuts.Count];
                StartRecording(outputNames);

                for (; Step < Maxiterations; Step++)
                {
                    if (token.IsCancellationRequested) break;
                    int signaledIndex = WaitHandle.WaitAny(waitHandles);
                    if (signaledIndex == 0) break;

                    if (_driveModes != null && Step < _driveModes.Length)
                        SetInputPort(64, _driveModes[Step]);   // 속도 프로파일 = port 64 (DM_Speed_Profile, ctypes 검증)

                    CallStep();

                    for (int i = 0; i < PortGuideShipMWOuts.Count; i++)
                    {
                        if (OutputPortMap.TryGetValue(i, out int port))
                        {
                            double val = GetOutputPort(port);
                            PortGuideShipMWOuts[i].Value = val;
                            outputValues[i] = val;
                        }
                    }

                    if (CheckNaN(outputValues, outputNames, Step)) { Step = 0; break; }
                    RecordStep(Step, outputValues);

                    if (Step % 100 == 0)
                    {
                        OnDataReceived("");
                    }
                }

                OnDataReceived("");
            }
            catch (Exception ex) { Debug.WriteLine($"RunWithCancellation 예외: {ex.Message}"); }
            finally
            {
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "PortGuideShip Finished";
            }
        }

        public void Calculate()
        {
            ResetNaNWarning(); // 새 시뮬 시작 시 NaN 플래그 리셋 (이전 시뮬 NaN 잔존 방지)
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            if (CalculateThread == null || !CalculateThread.IsAlive)
            {
                Debug.WriteLine("PortGuideShip Calculate 시작");
                CalculateThread = new Thread(() => RunWithCancellation(token));
                InitValue();
                CalculateThread.Start();
            }
        }

        public void StopCalculation()
        {
            Debug.WriteLine("PortGuideShip StopCalculation 시작");
            _cancellationTokenSource?.Cancel();
            manualEvent.Set();

            if (CalculateThread != null && CalculateThread.IsAlive) CalculateThread.Join();

            Step = 0; IsPause = false; IsinitValue = false;
            foreach (var output in PortGuideShipMWOuts) output.Value = 0;

            CallTerminate();
            UnloadDll();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            CalculateThread = null;
        }

        public void OneStep()
        {
            Step++;
            if (_driveModes != null && Step < _driveModes.Length)
                SetInputPort(64, _driveModes[Step]);   // 속도 프로파일 = port 64 (DM_Speed_Profile, ctypes 검증)

            CallStep();

            for (int i = 0; i < PortGuideShipMWOuts.Count; i++)
            {
                if (OutputPortMap.TryGetValue(i, out int port))
                    PortGuideShipMWOuts[i].Value = GetOutputPort(port);
            }

            OnDataReceived("");
        }

        public void Reset()
        {
            if (CalculateThread != null && CalculateThread.IsAlive) StopCalculation();
            manualEvent.Set();
            IsPause = false;
            Step = 0;
        }

        public void InitValue()
        {
            if (!IsinitValue)
            {
                if (!LoadDll(DLL_FILE_NAME, FUNCTION_PREFIX, MAX_INPUT_PORT, MAX_OUTPUT_PORT))
                {
                    Debug.WriteLine("DLL 로드 실패");
                    return;
                }

                try { CallInitialize(); Debug.WriteLine("DLL initialize() 완료"); }
                catch (Exception ex) { Debug.WriteLine($"initialize() 실패: {ex.Message}"); return; }

                IsinitValue = true;
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "PortGuideShip Running";
            }

            // Set all input values from defaults (TXT)
            foreach (var kvp in _defaultInputValues)
            {
                SetInputPort(kvp.Key, kvp.Value);
            }

            // GUI 그리드의 모든 입력값을 해당 DLL 포트에 반영 (Ground와 동일 방식, Sea 고유 포트맵).
            // 속도 프로파일(port 64)은 매 step RunWithCancellation에서 채우므로 skip.
            // Mode(482)/Design(483)는 이 DLL에 포트 없어 silent no-op, Control(65)은 반영됨.
            for (int i = 0; i < PortGuideShipMWInputs.Count; i++)
            {
                if (InputPortMap.TryGetValue(i, out int port))
                {
                    if (port == 64) continue;   // 속도 프로파일 = 매 step 주입
                    SetInputPort(port, PortGuideShipMWInputs[i].Value);
                }
            }
        }
    }
}
