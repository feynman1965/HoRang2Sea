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
    public class FishingBoatMWDataModel
    {
        public FishingBoatMWDataModel() { }
        public FishingBoatMWDataModel(string name) { Name = name; }
        public FishingBoatMWDataModel(string name, string unit, string parent) { Name = name; Unit = unit; Parent = parent; }

        public Subject<double> SubjectValue = new Subject<double>();
        private double _value;

        public string Name { get; set; }
        public double Value { get => _value; set { _value = value; SubjectValue.OnNext(value); } }
        public string Unit { get; set; }
        public string Parent { get; set; }
    }

    public class FishingBoatMWModel : GenericPortDllModel { }

    public class FishingBoatMW : FishingBoatMWModel
    {
        public Thread CalculateThread { get; set; }
        public bool IsinitValue { get; set; }
        public int Step { get; set; }
        public bool IsPause { get; set; }
        public static ManualResetEvent manualEvent = new ManualResetEvent(true);
        private double[] _driveModes;
        private CancellationTokenSource _cancellationTokenSource;

        private const string DLL_FILE_NAME = "Electric_072MW_250703_hydro_win64.dll";
        private const string FUNCTION_PREFIX = "Electric_072MW_250703_hydro";
        private const int MAX_INPUT_PORT = 65;
        private const int MAX_OUTPUT_PORT = 55;

        // Input index -> port number mapping (sequential 1-65)
        private static readonly Dictionary<int, int> InputPortMap = new()
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 },
            { 10, 11 }, { 11, 12 }, { 12, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 }, { 16, 17 }, { 17, 18 }, { 18, 19 },
            { 19, 20 }, { 20, 21 }, { 21, 22 }, { 22, 23 }, { 23, 24 }, { 24, 25 }, { 25, 26 }, { 26, 27 }, { 27, 28 },
            { 28, 29 }, { 29, 30 }, { 30, 31 }, { 31, 32 }, { 32, 33 }, { 33, 34 }, { 34, 35 }, { 35, 36 }, { 36, 37 },
            { 37, 38 }, { 38, 39 }, { 39, 40 }, { 40, 41 }, { 41, 42 }, { 42, 43 }, { 43, 44 }, { 44, 45 }, { 45, 46 },
            { 46, 47 }, { 47, 48 }, { 48, 49 }, { 49, 50 }, { 50, 51 }, { 51, 52 }, { 52, 53 }, { 53, 54 }, { 54, 55 },
            { 55, 56 }, { 56, 57 }, { 57, 58 }, { 58, 59 }, { 59, 60 }, { 60, 61 }, { 61, 62 }, { 62, 63 }
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

        // Port initial values (from FS_initial_value.txt: port_number -> value)
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
            { 46, 0.254 }, { 47, 0.8 }, { 48, 1.0 }, { 49, 2.9 }, { 50, 8.5 },
            { 51, 0.61 }, { 52, 1.0 }, { 53, 2.9 }, { 54, 18.5 }, { 55, 25.0 },
            { 56, 20.0 }, { 57, 2100.0 }, { 58, 65.0 }, { 59, 0.1 }, { 60, 0.5 },
            { 61, 298.15 }, { 62, 1.5 }, { 63, 25.0 }, { 64, 0.01 }, { 65, 1.0 }
        };

        public List<FishingBoatMWDataModel> FishingBoatMWInputs = new()
        {
            //Slip  ( 개수 : 1 )
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
        };

        public List<FishingBoatMWDataModel> FishingBoatMWOuts = new()
        {
            //FishingBoatMode  ( 개수 : 3 )
            new("Navigation_mode", "-", "FishingBoatProfile"),
            new("FB_displacement", "miles", "FishingBoatProfile"),
            new("FB_fuelefficiency", "miles/kg", "FishingBoatProfile"),

            //Stack  ( 개수 : 5 )
            new("FCS_Current_density", "A/cm2", "Stack"),
            new("FCS_Stack_Voltage", "V", "Stack"),
            new("FCS_Net_power", "kW", "Stack"),
            new("FCS_Open_Circuit_Voltage", "V", "Stack"),
            new("FCS_Stack_temperature", "K", "Stack"),

            //Blower  ( 개수 : 4 )
            new("Bwr_Compressor_torque", "Nm", "Blower"),
            new("Bwr_Compressor_Efficiency", "-", "Blower"),
            new("Bwr_Compressor_Air_mass_flow_rate", "kg/s", "Blower"),
            new("Bwr_outlet_pressure", "Pa", "Blower"),

            //Air Humidifier  ( 개수 : 4 )
            new("AH_Total_humidified_air_flow_rate", "kg/s", "Air Humidifier"),
            new("AH_Cathode_gas_temperature", "K", "Air Humidifier"),
            new("AH_Cathode_gas_total_pressure", "Pa", "Air Humidifier"),
            new("AH_Outlet_relative_humidity", "-", "Air Humidifier"),

            //Hydrogen Humidifier  ( 개수 : 4 )
            new("HH_Anode_inlet_mass_flow_rate", "kg/s", "Hydrogen Humidifier"),
            new("HH_Water_mole_fraction", "-", "Hydrogen Humidifier"),
            new("HH_Anode_gas_temperature", "K", "Hydrogen Humidifier"),
            new("HH_Anode_gas_total_pressure", "Pa", "Hydrogen Humidifier"),

            //Fresh Water Pump  ( 개수 : 3 )
            new("FWP_Fresh_water_flow_rate", "kg/s", "Fresh Water Pump"),
            new("FWP_Fresh_water_outlet_temperature", "K", "Fresh Water Pump"),
            new("FWP_Fresh_water_outlet_pressure", "bar", "Fresh Water Pump"),

            //Sea Water Pump  ( 개수 : 3 )
            new("SWP_Sea_water_flow_rate", "kg/s", "Sea Water Pump"),
            new("SWP_Sea_water_outlet_temperature", "K", "Sea Water Pump"),
            new("SWP_Sea_water_outlet_pressure", "bar", "Sea Water Pump"),

            //Valve  ( 개수 : 14 )
            new("TWV_Valve_opening_ratio", "-", "Valve"),
            new("TVW_Bypass_coolant_flow_rate", "kg/s", "Valve"),
            new("TVW_Bypass_coolant_temperature", "K", "Valve"),
            new("TVW_Bypass_coolant_inlet_pressure", "kPa", "Valve"),
            new("TVW_heatexchanger_coolant_flow_rate", "kg/s", "Valve"),
            new("TVW_heatexchanger_coolant_temperature", "K", "Valve"),
            new("TWV_heatexchanger_coolant_inlet_pressure", "kPa", "Valve"),
            new("TWV_stack_valve_opening_ratio", "-", "Valve"),
            new("TWV_stack_bypass_coolant_flow_rate", "kg/s", "Valve"),
            new("TWV_stack_bypass_coolant_temperature ", "K", "Valve"),
            new("TWV_stack_bypass_coolant_inlet_pressure ", "kPa", "Valve"),
            new("TWV_stack_coolant_flow_rate ", "kg/s", "Valve"),
            new("TWV_stack_coolant_temperature ", "K", "Valve"),
            new("TWV_stack_coolant_pressure ", "kPa", "Valve"),

            //Heat Exchanger  ( 개수 : 2 )
            new("HX_coolant_temperature_outlet", "\u2103", "Heat Exchanger"),
            new("HX_sea_water_temperature_outlet", "\u2103", "Heat Exchanger"),

            //Induction Motor  ( 개수 : 2 )
            new("IM_Motor_RPM", "Nm", "Induction Motor"),
            new("IM_Electric_power", "kW", "Induction Motor"),

            //Converter  ( 개수 : 3 )
            new("DCC_Duty_ratio", "-", "Converter"),
            new("DCC_Converter_voltage", "V", "Converter"),
            new("DCC_Converter_current", "I", "Converter"),

            //Intercooler  ( 개수 : 4 )
            new("IC_outlet_mass_flow_rate", "kg/s", "Intercooler"),
            new("IC_outlet_temperature", "K", "Intercooler"),
            new("IC_outlet_pressure", "Pa", "Intercooler"),
            new("IC_outlet_relative_humidity ", "-", "Intercooler"),

            //Battery  ( 개수 : 4 )
            new("BAT_SOC", "-", "Battery"),
            new("BAT_voltage", "V", "Battery"),
            new("BAT_discharge_current", "A", "Battery"),
            new("BAT_battery_power ", "kW", "Battery"),
        };

        public FishingBoatMW()
        {
            _cancellationTokenSource = null;
            IsinitValue = false;
            IsPause = false;
            Step = 0;
            ResetNaNWarning();
        }

        public void SetDriveModes(double[] driveModes) => _driveModes = driveModes;

        // 레이아웃 설정 (ViewModel에서 설정)
        public int DesignLayout { get; set; } = 0;
        public int ControlLayout { get; set; } = 0;

        public void RunWithCancellation(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("FishingBoat 모델 시작");
                int Maxiterations = _driveModes != null ? Math.Min(_driveModes.Length, 5000000) : 100000;
                WaitHandle[] waitHandles = new WaitHandle[] { token.WaitHandle, manualEvent };

                // Start CSV recording
                StartRecording(FishingBoatMWOuts.Select(o => o.Name).ToList());

                for (; Step < Maxiterations; Step++)
                {
                    if (token.IsCancellationRequested) break;
                    int signaledIndex = WaitHandle.WaitAny(waitHandles);
                    if (signaledIndex == 0) break;

                    // Set drive mode input (port 1)
                    if (_driveModes != null && Step < _driveModes.Length)
                        SetInputPort(1, _driveModes[Step]);

                    // Execute one step
                    CallStep();

                    // Read all outputs
                    var outputValues = new double[FishingBoatMWOuts.Count];
                    for (int i = 0; i < FishingBoatMWOuts.Count; i++)
                    {
                        if (OutputPortMap.TryGetValue(i, out int port))
                        {
                            double val = GetOutputPort(port);
                            FishingBoatMWOuts[i].Value = val;
                            outputValues[i] = val;
                        }
                    }

                    CheckNaN(outputValues, FishingBoatMWOuts.Select(o => o.Name).ToList(), Step);
                    RecordStep(Step, outputValues);

                    // UI 업데이트 (GUI 멈춤 방지: 100스텝마다)
                    if (Step % 100 == 0)
                    {
                        OnDataReceived("");
                    }
                }

                // 마지막 데이터 업데이트
                OnDataReceived("");
            }
            catch (Exception ex) { Debug.WriteLine($"RunWithCancellation 예외: {ex.Message}"); }
            finally
            {
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "FishingBoat Finished";
            }
        }

        public void Calculate()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            if (CalculateThread == null || !CalculateThread.IsAlive)
            {
                Debug.WriteLine("FishingBoat Calculate 시작");
                CalculateThread = new Thread(() => RunWithCancellation(token));
                InitValue();
                CalculateThread.Start();
            }
        }

        public void StopCalculation()
        {
            Debug.WriteLine("FishingBoat StopCalculation 시작");
            _cancellationTokenSource?.Cancel();
            manualEvent.Set();

            if (CalculateThread != null && CalculateThread.IsAlive) CalculateThread.Join();

            Step = 0; IsPause = false; IsinitValue = false;
            foreach (var output in FishingBoatMWOuts) output.Value = 0;

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
                SetInputPort(1, _driveModes[Step]);

            CallStep();

            for (int i = 0; i < FishingBoatMWOuts.Count; i++)
            {
                if (OutputPortMap.TryGetValue(i, out int port))
                    FishingBoatMWOuts[i].Value = GetOutputPort(port);
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
                mv.Status = "FishingBoat Running";
            }

            // Set all input values from the input list using port mapping
            for (int i = 0; i < FishingBoatMWInputs.Count; i++)
            {
                if (InputPortMap.TryGetValue(i, out int port))
                {
                    double val = FishingBoatMWInputs[i].Value;
                    // Use initial value if input is 0 and we have a default
                    if (val == 0.0 && _defaultInputValues.TryGetValue(port, out double initVal))
                        val = initVal;
                    SetInputPort(port, val);
                }
            }
        }
    }
}
