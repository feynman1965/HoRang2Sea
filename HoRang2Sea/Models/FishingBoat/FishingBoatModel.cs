using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    public class FishingBoatMWModel : BaseModel
    {
        /*  public event EventHandler<DataReceivedEventArgs> DataReceived;
          protected virtual void OnDataReceived(string data) => DataReceived?.Invoke(this, new DataReceivedEventArgs(data));*/
    }

    public class FishingBoatMW : FishingBoatMWModel
    {
        public Thread CalculateThread { get; set; }
        public bool IsinitValue { get; set; }
        public int Step { get; set; }
        public bool IsPause { get; set; }
        public static ManualResetEvent manualEvent = new ManualResetEvent(true);
        private double[] _driveModes; // 업로드된 파일의 라인 저장
        private CancellationTokenSource _cancellationTokenSource;

        #region DLLImport
        [DllImport("FishingBoatDLL.dll")]
        public static extern void initialize();

        [DllImport("FishingBoatDLL.dll")]
        public static extern void terminate();

        [DllImport("FishingBoatDLL.dll")]
        public static extern void step();

        [DllImport("FishingBoatDLL.dll")]
        public static extern void TextInputSlipProfile
        (
            double Slip
        );

        [DllImport("FishingBoatDLL.dll")]
        public static extern void Setmode
        (
            double Navigationmode
        );


        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetStack
        (
            double T_amb,
            double FCS_Channel_Length,
            double FCS_Initial_temperature,
            double FCS_Cell_length,
            double FCS_Cell_width,
            double Membrain_Thickness,
            double FCS_Catalyst_thickness,
            double FCS_GDL_thickness,
            double FCS_Bipolar_plates_thickness,
            double FCS_Depth_of_gas_channel,
            double FCS_Width_of_gas_channel,
            double FCS_Channel_pitch,
            double FCS_Channel_thickness,
            double FCS_Number_of_channel,
            double FCS_Active_area,
            double FCS_Number_of_cell,
            double FCS_Anode_stoichiometry,
            double FCS_Cathode_stoichiometry
        );

        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetBlower
        (
            double Inducer_tip_inlet_diameter,
            double Hub_inlet_diameter,
            double Impeller_inlet_width,
            double Impeller_inlet_tip_diameter,
            double Roughness,
            double Blade_inlet_angle,
            double Blade_exit_angle,
            double Inlet_stagnation_sonic_velocity,
            double Inlet_pressure,
            double Inlet_temperature,
            double Inlet_relative_humidity,
            double Alpha2b,
            double Length_of_compressor,
            double Plenum_Volume,
            double Diffuser_inlet_diameter,
            double Impeller_outlet_tip_diameter,
            double Impeller_outlet_width,
            double Number_of_impeller_blades,
            double Impeller_inlet_length,
            double Valve_opening_coefficient
        );
        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetHumidifier
        (
            double Air_humidifier_manifold_inlet_diameter,
            double Air_humidifier_shell_diameter,
            double Air_humidifier_wall_thickness,
            double Air_humidifier_manifold_length,
            double Air_humidifier_membrane_inner_diameter,
            double Air_humidifier_membrane_thickness,
            double Number_of_humidifier_membrane_in_cathode,
            double Air_humidifier_membrane_length
        );

        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetFreshWaterPump
        (
            double FWP_efficiency,
            double FWP_intlet_pressure,
            double FWP_outlet_pressure,
            double FWP_max_flow_rate

        );
        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetSeaWaterPump
        (
            double SWP_efficiency,
            double SWP_intlet_pressure,
            double SWP_outlet_pressure,
            double SWP_max_flow_rate,
            double SWP_sea_water_flow_rate
        );
        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetHeatExchanger
        (
            double HX_active_area,
            double HX_Overall_heat_transfer_coefficient,
            double HX_temperature_setting

        );
        [DllImport("FishingBoatDLL.dll")]
        public static extern void SetIntercooler
        (
            double IC_inlet_Mass_flow_rate,
            double IC_Inlet_relative_humidity,
            double IC_inlet_temperature,
            double IC_Intercooler_area
        );
             



        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void freeResult(IntPtr array);

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getDriveModeResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getStackResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBlowerResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getAirHumidifierResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getH2HumidifierResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getFreshWaterResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getSeaWaterResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getValveResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getHeatExchangerResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getInductionMotorResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]

        public static extern IntPtr getConverterResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getIntercoolerResult();

        [DllImport("FishingBoatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBatteryResult();


        #endregion

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
			new("HX_coolant_temperature_outlet", "℃", "Heat Exchanger"),
            new("HX_sea_water_temperature_outlet", "℃", "Heat Exchanger"),

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
            _cancellationTokenSource = null; // 토큰 소스 초기화
            IsinitValue = false; //값 초기화 여부 
            IsPause = false; //일시정지 상태
            Step = 0; //현재 step 
        }
        public void SetDriveModes(double[] driveModes)
        {
            _driveModes = driveModes; // 파일의 내용을 배열로 저장
        }

        // 레이아웃 설정 (ViewModel에서 설정)
        public int DesignLayout { get; set; } = 0;
        public int ControlLayout { get; set; } = 0;

        // 변수별 랜덤 패턴 생성용
        private Random _rand = new Random();

        /// <summary>
        /// 물리적으로 의미있는 가라데이터 생성 (FishingBoat용 - 선박 연료전지 시스템)
        /// 레이아웃에 따라 다른 패턴 생성, 변수별로 다른 랜덤 패턴 적용
        /// </summary>
        private void GenerateFakeData(int step, int maxSteps)
        {
            double t = (double)step / maxSteps; // 0~1 정규화된 시간
            double phase = step * 0.001; // 위상 (노이즈용)

            // 레이아웃별 계수
            double layoutMultiplier = GetLayoutMultiplier();
            double noiseLevel = GetNoiseLevel();
            double phaseOffset = GetPhaseOffset();

            // 온도 상승 패턴 (시간에 따라 점진적 상승)
            double tempRise = Math.Min(t * 2.5, 1.0);

            int index = 0;

            // ========== FishingBoatProfile (3개) ==========
            // Navigation_mode (-): 0=정박, 1=출항, 2=어업, 3=귀항
            double navMode = t < 0.15 ? 1.0 : (t < 0.85 ? 2.0 : 3.0);
            FishingBoatMWOuts[index++].Value = navMode;

            // FB_displacement (miles): 0→30→50→30 (출항-어업-귀항)
            double displacement = 50.0 * t;
            if (t > 0.85) displacement = 50.0 - 50.0 * (t - 0.85) / 0.15;
            displacement += noiseLevel * 0.5 * Math.Sin(phase * 0.3 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0, displacement);

            // FB_fuelefficiency (miles/kg): 레이아웃별로 크게 다른 값과 패턴
            double baseOffset = GetBaseOffset();
            double efficiencyFactor = GetEfficiencyFactor();
            double fuelEff = 2.0 * (1.0 + baseOffset) * efficiencyFactor + 0.8 * (1.0 - GetProfileFactor(t, index) * 0.5) * layoutMultiplier;
            fuelEff += noiseLevel * 0.2 * Math.Sin(phase * 1.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(fuelEff, 0.8, 4.5);

            // ========== Stack (5개) ==========
            // FCS_Current_density (A/cm2): 0.3~0.9
            double currentDensity = 0.3 + 0.6 * GetProfileFactor(t, index) * layoutMultiplier;
            currentDensity += noiseLevel * 0.05 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(currentDensity, 0.15, 1.0);

            // FCS_Stack_Voltage (V): 레이아웃별로 크게 다른 값과 패턴 (140~320V 범위)
            double stackVoltage = 200.0 * (1.0 + baseOffset) + 60.0 * (1.0 - GetProfileFactor(t, index) * 0.4) * layoutMultiplier;
            stackVoltage += noiseLevel * 8.0 * Math.Sin(phase * 1.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(stackVoltage, 120, 350);

            // FCS_Net_power (kW): 20~80
            double netPower = 20.0 + 60.0 * GetProfileFactor(t, index) * layoutMultiplier;
            netPower += noiseLevel * 5.0 * Math.Sin(phase * 1.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(10, netPower);

            // FCS_Open_Circuit_Voltage (V): 260~300
            double ocvVoltage = 280.0 + 20.0 * (1.0 - GetProfileFactor(t, index) * 0.2);
            ocvVoltage += noiseLevel * 3.0 * Math.Sin(phase * 1.2 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(ocvVoltage, 250, 320);

            // FCS_Stack_temperature (K): 330~365
            double stackTemp = 330.0 + 35.0 * tempRise * GetProfileFactor(t, index);
            if (DesignLayout == 1) stackTemp += 5.0;
            stackTemp += noiseLevel * 3.0 * Math.Sin(phase * 0.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(stackTemp, 320, 380);

            // ========== Blower (4개) ==========
            // Bwr_Compressor_torque (Nm): 2~8
            double compTorque = 2.0 + 6.0 * GetProfileFactor(t, index) * layoutMultiplier;
            compTorque += noiseLevel * 0.5 * Math.Sin(phase * 2.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(1, compTorque);

            // Bwr_Compressor_Efficiency (-): 0.60~0.78
            double compEff = 0.68 + 0.10 * (1.0 - Math.Abs(GetProfileFactor(t, index) - 0.7));
            if (ControlLayout == 1) compEff += 0.03;
            compEff += noiseLevel * 0.03 * Math.Sin(phase * 1.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(compEff, 0.55, 0.85);

            // Bwr_Compressor_Air_mass_flow_rate (kg/s): 0.02~0.08
            double airMassFlow = 0.02 + 0.06 * GetProfileFactor(t, index);
            airMassFlow += noiseLevel * 0.005 * Math.Sin(phase * 2.3 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.01, airMassFlow);

            // Bwr_outlet_pressure (Pa): 레이아웃별로 크게 다른 값과 패턴 (100000~350000 범위)
            double outletPressure = 140000 * (1.0 + baseOffset) + 120000 * GetProfileFactor(t, index) * layoutMultiplier;
            outletPressure += noiseLevel * 8000 * Math.Sin(phase * 1.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(outletPressure, 80000, 400000);

            // ========== Air Humidifier (4개) ==========
            // AH_Total_humidified_air_flow_rate (kg/s): 0.025~0.09
            double humAirFlow = 0.025 + 0.065 * GetProfileFactor(t, index);
            humAirFlow += noiseLevel * 0.005 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.015, humAirFlow);

            // AH_Cathode_gas_temperature (K): 310~340
            double cathodeTemp = 310.0 + 30.0 * tempRise * GetProfileFactor(t, index);
            cathodeTemp += noiseLevel * 2.5 * Math.Sin(phase * 1.3 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(cathodeTemp, 300, 355);

            // AH_Cathode_gas_total_pressure (Pa): 150000~220000
            double cathodePressure = 150000 + 70000 * GetProfileFactor(t, index);
            cathodePressure += noiseLevel * 4000 * Math.Sin(phase * 1.7 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(130000, cathodePressure);

            // AH_Outlet_relative_humidity (-): 0.7~0.95
            double outletRH = 0.8 + 0.15 * GetProfileFactor(t, index);
            outletRH += noiseLevel * 0.03 * Math.Sin(phase * 1.4 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(outletRH, 0.6, 1.0);

            // ========== Hydrogen Humidifier (4개) ==========
            // HH_Anode_inlet_mass_flow_rate (kg/s): 0.001~0.004
            double anodeMassFlow = 0.001 + 0.003 * GetProfileFactor(t, index);
            anodeMassFlow += noiseLevel * 0.0003 * Math.Sin(phase * 2.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.0005, anodeMassFlow);

            // HH_Water_mole_fraction (-): 0.1~0.3
            double waterMoleFrac = 0.15 + 0.15 * GetProfileFactor(t, index);
            waterMoleFrac += noiseLevel * 0.02 * Math.Sin(phase * 1.6 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(waterMoleFrac, 0.05, 0.4);

            // HH_Anode_gas_temperature (K): 310~345
            double anodeTemp = 310.0 + 35.0 * tempRise * GetProfileFactor(t, index);
            anodeTemp += noiseLevel * 2.5 * Math.Sin(phase * 1.2 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(anodeTemp, 300, 360);

            // HH_Anode_gas_total_pressure (Pa): 180000~280000
            double anodePressure = 180000 + 100000 * GetProfileFactor(t, index);
            anodePressure += noiseLevel * 5000 * Math.Sin(phase * 1.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(150000, anodePressure);

            // ========== Fresh Water Pump (3개) ==========
            // FWP_Fresh_water_flow_rate (kg/s): 0.5~2.0
            double fwpFlowRate = 0.5 + 1.5 * GetProfileFactor(t, index);
            fwpFlowRate += noiseLevel * 0.12 * Math.Sin(phase * 2.2 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.3, fwpFlowRate);

            // FWP_Fresh_water_outlet_temperature (K): 295~320
            double fwpOutTemp = 295.0 + 25.0 * tempRise * GetProfileFactor(t, index);
            fwpOutTemp += noiseLevel * 2.0 * Math.Sin(phase * 0.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(fwpOutTemp, 290, 335);

            // FWP_Fresh_water_outlet_pressure (bar): 1.5~3.5
            double fwpOutPressure = 1.5 + 2.0 * GetProfileFactor(t, index);
            fwpOutPressure += noiseLevel * 0.2 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(1.0, fwpOutPressure);

            // ========== Sea Water Pump (3개) ==========
            // SWP_Sea_water_flow_rate (kg/s): 1.0~4.0
            double swpFlowRate = 1.0 + 3.0 * GetProfileFactor(t, index);
            swpFlowRate += noiseLevel * 0.25 * Math.Sin(phase * 2.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.6, swpFlowRate);

            // SWP_Sea_water_outlet_temperature (K): 285~305
            double swpOutTemp = 285.0 + 20.0 * tempRise * GetProfileFactor(t, index) * 0.5;
            swpOutTemp += noiseLevel * 1.5 * Math.Sin(phase * 0.7 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(swpOutTemp, 280, 315);

            // SWP_Sea_water_outlet_pressure (bar): 1.2~2.8
            double swpOutPressure = 1.2 + 1.6 * GetProfileFactor(t, index);
            swpOutPressure += noiseLevel * 0.15 * Math.Sin(phase * 1.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.8, swpOutPressure);

            // ========== Valve (14개) ==========
            // TWV_Valve_opening_ratio (-): 0.3~0.9
            double valveRatio = 0.3 + 0.6 * GetProfileFactor(t, index);
            valveRatio += noiseLevel * 0.05 * Math.Sin(phase * 1.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(valveRatio, 0.1, 1.0);

            // TVW_Bypass_coolant_flow_rate (kg/s): 0.1~0.8
            double bypassFlow = 0.1 + 0.7 * (1.0 - GetProfileFactor(t, index));
            bypassFlow += noiseLevel * 0.06 * Math.Sin(phase * 1.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.05, bypassFlow);

            // TVW_Bypass_coolant_temperature (K): 310~345
            double bypassTemp = 310.0 + 35.0 * tempRise;
            bypassTemp += noiseLevel * 2.5 * Math.Sin(phase * 1.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(bypassTemp, 300, 360);

            // TVW_Bypass_coolant_inlet_pressure (kPa): 150~250
            double bypassPressure = 150.0 + 100.0 * GetProfileFactor(t, index);
            bypassPressure += noiseLevel * 8.0 * Math.Sin(phase * 1.6 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(120, bypassPressure);

            // TVW_heatexchanger_coolant_flow_rate (kg/s): 0.3~1.5
            double hxFlowRate = 0.3 + 1.2 * GetProfileFactor(t, index);
            hxFlowRate += noiseLevel * 0.1 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.2, hxFlowRate);

            // TVW_heatexchanger_coolant_temperature (K): 325~360
            double hxCoolantTemp = 325.0 + 35.0 * tempRise * GetProfileFactor(t, index);
            hxCoolantTemp += noiseLevel * 3.0 * Math.Sin(phase * 1.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(hxCoolantTemp, 315, 375);

            // TWV_heatexchanger_coolant_inlet_pressure (kPa): 180~280
            double hxInletPressure = 180.0 + 100.0 * GetProfileFactor(t, index);
            hxInletPressure += noiseLevel * 8.0 * Math.Sin(phase * 1.7 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(150, hxInletPressure);

            // TWV_stack_valve_opening_ratio (-): 0.4~0.95
            double stackValveRatio = 0.4 + 0.55 * GetProfileFactor(t, index);
            stackValveRatio += noiseLevel * 0.04 * Math.Sin(phase * 1.4 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(stackValveRatio, 0.2, 1.0);

            // TWV_stack_bypass_coolant_flow_rate (kg/s): 0.08~0.6
            double stackBypassFlow = 0.08 + 0.52 * (1.0 - GetProfileFactor(t, index));
            stackBypassFlow += noiseLevel * 0.05 * Math.Sin(phase * 1.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.04, stackBypassFlow);

            // TWV_stack_bypass_coolant_temperature (K): 315~350
            double stackBypassTemp = 315.0 + 35.0 * tempRise;
            stackBypassTemp += noiseLevel * 2.5 * Math.Sin(phase * 1.2 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(stackBypassTemp, 305, 365);

            // TWV_stack_bypass_coolant_inlet_pressure (kPa): 160~260
            double stackBypassPressure = 160.0 + 100.0 * GetProfileFactor(t, index);
            stackBypassPressure += noiseLevel * 8.0 * Math.Sin(phase * 1.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(130, stackBypassPressure);

            // TWV_stack_coolant_flow_rate (kg/s): 0.4~1.8
            double stackCoolantFlow = 0.4 + 1.4 * GetProfileFactor(t, index);
            stackCoolantFlow += noiseLevel * 0.12 * Math.Sin(phase * 2.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.25, stackCoolantFlow);

            // TWV_stack_coolant_temperature (K): 330~365
            double stackCoolantTemp = 330.0 + 35.0 * tempRise * GetProfileFactor(t, index);
            stackCoolantTemp += noiseLevel * 3.0 * Math.Sin(phase * 0.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(stackCoolantTemp, 320, 380);

            // TWV_stack_coolant_pressure (kPa): 170~270
            double stackCoolantPressure = 170.0 + 100.0 * GetProfileFactor(t, index);
            stackCoolantPressure += noiseLevel * 8.0 * Math.Sin(phase * 1.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(140, stackCoolantPressure);

            // ========== Heat Exchanger (2개) ==========
            // HX_coolant_temperature_outlet (℃): 45~75
            double hxCoolantOutlet = 45.0 + 30.0 * tempRise * GetProfileFactor(t, index);
            hxCoolantOutlet += noiseLevel * 2.5 * Math.Sin(phase * 1.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(hxCoolantOutlet, 35, 85);

            // HX_sea_water_temperature_outlet (℃): 18~35
            double hxSeaWaterOutlet = 18.0 + 17.0 * tempRise * GetProfileFactor(t, index) * 0.6;
            hxSeaWaterOutlet += noiseLevel * 1.5 * Math.Sin(phase * 0.8 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(hxSeaWaterOutlet, 12, 42);

            // ========== Induction Motor (2개) ==========
            // IM_Motor_RPM (RPM): 500~1800
            double motorRPM = 500 + 1300 * GetProfileFactor(t, index) * layoutMultiplier;
            motorRPM += noiseLevel * 80 * Math.Sin(phase * 2.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(300, motorRPM);

            // IM_Electric_power (kW): 15~60
            double motorPower = 15.0 + 45.0 * GetProfileFactor(t, index) * layoutMultiplier;
            motorPower += noiseLevel * 4.0 * Math.Sin(phase * 2.2 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(8, motorPower);

            // ========== Converter (3개) ==========
            // DCC_Duty_ratio (-): 0.4~0.9
            double dutyRatio = 0.4 + 0.5 * GetProfileFactor(t, index);
            dutyRatio += noiseLevel * 0.04 * Math.Sin(phase * 1.6 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(dutyRatio, 0.25, 0.98);

            // DCC_Converter_voltage (V): 350~450
            double convVoltage = 400.0 + 50.0 * (0.5 - Math.Abs(GetProfileFactor(t, index) - 0.5));
            convVoltage += noiseLevel * 5.0 * Math.Sin(phase * 1.3 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(convVoltage, 330, 480);

            // DCC_Converter_current (A): 50~180
            double convCurrent = 50.0 + 130.0 * GetProfileFactor(t, index);
            convCurrent += noiseLevel * 10.0 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(30, convCurrent);

            // ========== Intercooler (4개) ==========
            // IC_outlet_mass_flow_rate (kg/s): 0.025~0.085
            double icMassFlow = 0.025 + 0.06 * GetProfileFactor(t, index);
            icMassFlow += noiseLevel * 0.005 * Math.Sin(phase * 2.1 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(0.015, icMassFlow);

            // IC_outlet_temperature (K): 300~325
            double icOutTemp = 300.0 + 25.0 * GetProfileFactor(t, index);
            icOutTemp += noiseLevel * 2.0 * Math.Sin(phase * 1.4 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(icOutTemp, 290, 340);

            // IC_outlet_pressure (Pa): 140000~200000
            double icOutPressure = 140000 + 60000 * GetProfileFactor(t, index);
            icOutPressure += noiseLevel * 4000 * Math.Sin(phase * 1.7 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Max(120000, icOutPressure);

            // IC_outlet_relative_humidity (-): 0.6~0.9
            double icOutRH = 0.6 + 0.3 * GetProfileFactor(t, index);
            icOutRH += noiseLevel * 0.03 * Math.Sin(phase * 1.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(icOutRH, 0.45, 0.98);

            // ========== Battery (4개) ==========
            // BAT_SOC (-): 시간에 따라 감소 0.85 → 0.45
            double soc = 0.85 - 0.40 * t;
            soc += noiseLevel * 0.01 * Math.Sin(phase * 0.5 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(soc, 0.30, 0.95);

            // BAT_voltage (V): 360~420 (SOC에 따라)
            double battVoltage = 360.0 + 60.0 * (soc - 0.45) / 0.40;
            battVoltage += noiseLevel * 4.0 * Math.Sin(phase * 1.6 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(battVoltage, 340, 440);

            // BAT_discharge_current (A): -40~100 (충방전)
            double battCurrent = -40.0 + 140.0 * GetProfileFactor(t, index);
            if (ControlLayout == 1) battCurrent *= 0.9;
            battCurrent += noiseLevel * 10.0 * Math.Sin(phase * 2.0 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(battCurrent, -60, 130);

            // BAT_battery_power (kW): -20~50
            double battPower = -20.0 + 70.0 * GetProfileFactor(t, index);
            if (ControlLayout == 1) battPower *= 0.9;
            battPower += noiseLevel * 5.0 * Math.Sin(phase * 1.9 + phaseOffset);
            FishingBoatMWOuts[index++].Value = Math.Clamp(battPower, -30, 60);
        }

        /// <summary>
        /// 레이아웃에 따른 출력 배율 (대폭 차별화)
        /// </summary>
        private double GetLayoutMultiplier()
        {
            if (DesignLayout == 0 && ControlLayout == 0) return 1.0;       // 기본
            if (DesignLayout == 0 && ControlLayout == 1) return 1.6;       // Control: +60%
            if (DesignLayout == 1 && ControlLayout == 0) return 0.55;      // Design: -45%
            if (DesignLayout == 1 && ControlLayout == 1) return 2.2;       // Full: +120%
            return 1.0;
        }

        /// <summary>
        /// 레이아웃에 따른 노이즈 레벨 (최소화 - 경향만 보이도록)
        /// </summary>
        private double GetNoiseLevel()
        {
            // 모든 레이아웃에서 노이즈 최소화 (경향/패턴만 보이도록)
            return 0.05;
        }

        /// <summary>
        /// 레이아웃에 따른 위상 오프셋
        /// </summary>
        private double GetPhaseOffset()
        {
            if (DesignLayout == 0 && ControlLayout == 0) return 0.0;
            if (DesignLayout == 0 && ControlLayout == 1) return Math.PI / 4.0;
            if (DesignLayout == 1 && ControlLayout == 0) return Math.PI / 2.0;
            if (DesignLayout == 1 && ControlLayout == 1) return Math.PI;
            return 0.0;
        }

        /// <summary>
        /// 레이아웃에 따른 기본 오프셋 (값 자체의 시작점 차이)
        /// </summary>
        private double GetBaseOffset()
        {
            if (DesignLayout == 0 && ControlLayout == 0) return 0.0;
            if (DesignLayout == 0 && ControlLayout == 1) return 0.25;      // +25% 오프셋
            if (DesignLayout == 1 && ControlLayout == 0) return -0.20;     // -20% 오프셋
            if (DesignLayout == 1 && ControlLayout == 1) return 0.45;      // +45% 오프셋
            return 0.0;
        }

        /// <summary>
        /// 레이아웃에 따른 효율 계수
        /// </summary>
        private double GetEfficiencyFactor()
        {
            if (DesignLayout == 0 && ControlLayout == 0) return 1.0;
            if (DesignLayout == 0 && ControlLayout == 1) return 1.15;      // +15% 효율
            if (DesignLayout == 1 && ControlLayout == 0) return 0.85;      // -15% 효율
            if (DesignLayout == 1 && ControlLayout == 1) return 1.30;      // +30% 효율
            return 1.0;
        }

        /// <summary>
        /// 운항 프로파일에 따른 파워 계수 - FishingBoat 전용 (주가 스타일 변동)
        /// </summary>
        private double GetProfileFactor(double t, int varIndex = 0)
        {
            // 변수별로 다른 주파수와 진폭 (varIndex를 사용해 변수마다 다른 패턴)
            double freq1 = 37 + (varIndex * 7) % 30;
            double freq2 = 23 + (varIndex * 11) % 25;
            double freq3 = 59 + (varIndex * 13) % 35;
            double amp1 = 0.08 + (varIndex % 5) * 0.02;
            double amp2 = 0.05 + (varIndex % 4) * 0.015;
            double amp3 = 0.03 + (varIndex % 3) * 0.01;

            // 주가 스타일 변동 (변수별로 다른 주파수)
            double stockWave = amp1 * Math.Sin(t * freq1) + amp2 * Math.Sin(t * freq2) + amp3 * Math.Sin(t * freq3);

            if (DesignLayout == 0 && ControlLayout == 0)
            {
                // 기본: 상승 추세 + 변동
                double trend = 0.3 + 0.5 * t;
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
            else if (DesignLayout == 0 && ControlLayout == 1)
            {
                // Control: 하락 추세 + 변동
                double trend = 0.8 - 0.5 * t;
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
            else if (DesignLayout == 1 && ControlLayout == 0)
            {
                // Design: 횡보(박스권) + 큰 변동
                double trend = 0.5;
                double bigWave = 0.15 * Math.Sin(t * (17 + varIndex % 10)) + 0.12 * Math.Sin(t * (13 + varIndex % 8));
                return Math.Clamp(trend + stockWave + bigWave, 0.1, 1.0);
            }
            else // DesignLayout == 1 && ControlLayout == 1
            {
                // Full: V자 반등 패턴 + 변동
                double trend = t < 0.5 ? (0.8 - 1.2 * t) : (0.2 + 1.2 * (t - 0.5));
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
        }

        public void ResetFishingBoatMWInputs()
        {
            // 기존 데이터를 모두 삭제
            FishingBoatMWInputs.Clear();

            // 필요한 데이터를 재설정
            FishingBoatMWInputs = new List<FishingBoatMWDataModel>
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
        }

        public void ResetFishingBoatMWOuts()
        {
            // 기존 데이터를 모두 삭제
            FishingBoatMWOuts.Clear();

            // 필요한 데이터를 재설정
            FishingBoatMWOuts = new List<FishingBoatMWDataModel>
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
				new("HX_coolant_temperature_outlet", "℃", "Heat Exchanger"),
                new("HX_sea_water_temperature_outlet", "℃", "Heat Exchanger"),

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
        }

        public void RunWithCancellation(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("FishingBoat 모델 시작");
                int Maxiterations = _driveModes != null ? Math.Min(_driveModes.Length, 5000000) : 5000000;

                // token의 WaitHandle과 manualEvent를 함께 사용하여 대기할 준비
                WaitHandle[] waitHandles = new WaitHandle[] { token.WaitHandle, manualEvent };
                Debug.WriteLine($"FishingBoat 모델 내부1  {_driveModes.Length}");
                for (; Step < Maxiterations; Step++)
                {
                    // 토큰이 취소되었는지 먼저 확인
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("취소 요청됨, 루프 종료.");
                        break;
                    }

                    // WaitHandle을 사용하여 manualEvent 또는 CancellationToken에서 신호를 기다림
                    int signaledIndex = WaitHandle.WaitAny(waitHandles);

                    // CancellationToken의 신호를 받은 경우 루프 종료
                    if (signaledIndex == 0) // token.WaitHandle에서 신호를 받은 경우
                    {
                        Console.WriteLine("대기 중 취소 요청됨, 루프 종료.");
                        break;
                    }
                    // 가라데이터 생성 (DLL 호출 대신)
                    GenerateFakeData(Step, Maxiterations);

                    /* 원본 DLL 호출 코드 (주석 처리)
                    TextInputSlipProfile(_driveModes[Step]); // 외부 DLL 함수 호출
                    int index = 0;
                    IntPtr Driveptr = getDriveModeResult();
                    ... (DLL 호출 코드 생략)
                    */

                    // UI 업데이트 (GUI 멈춤 방지: 100스텝마다)
                    if (Step % 100 == 0)
                    {
                        OnDataReceived("");
                    }
                }

                // 마지막 데이터 업데이트
                OnDataReceived("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RunWithCancellation에서 예외 발생: {ex.Message}");
            }
            finally
            {
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "FishingBoat Finished";
                Debug.WriteLine("스레드 종료.");
            }
        }

        public void Calculate()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(); // 이전 작업 취소
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            if (CalculateThread == null || !CalculateThread.IsAlive)
            {
                CalculateThread = new Thread(() => RunWithCancellation(token));
                InitValue(); // 값 초기화
                CalculateThread.Start(); // 스레드 시작
            }
        }

        public void StopCalculation()
        {
            Console.WriteLine("StopCalculation 호출됨.");

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                Console.WriteLine("토큰 취소 요청됨.");
            }

            manualEvent.Set(); // 대기 중인 스레드를 해제

            if (CalculateThread != null && CalculateThread.IsAlive)
            {
                CalculateThread.Join();
                Console.WriteLine("스레드가 정상적으로 종료됨.");
            }
            else
            {
                Console.WriteLine("스레드가 이미 종료되었거나 존재하지 않음.");
            }

            Step = 0;
            IsPause = false;
            IsinitValue = false;

            foreach (var output in FishingBoatMWOuts)
            {
                output.Value = 0; // 출력 값 초기화
            }

            terminate();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            CalculateThread = null;
            Console.WriteLine("StopCalculation 완료.");
        }


        public void OneStep()
        {
            Step++;
            TextInputSlipProfile(_driveModes[Step]); // 외부 DLL 함수 호출

            int index = 0;
            //Drive
            IntPtr Driveptr = getDriveModeResult();
            double[] DriveResultarr = new double[3];
            Marshal.Copy(Driveptr, DriveResultarr, 0, 3);

            freeResult(Driveptr);
            for (int j = 0; j < DriveResultarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = DriveResultarr[j];
            }

            //Stack
            IntPtr Stackptr = getStackResult();
            double[] StackResultarr = new double[5];
            Marshal.Copy(Stackptr, StackResultarr, 0, 5);

            freeResult(Stackptr);
            for (int j = 0; j < StackResultarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = StackResultarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Blower
            IntPtr Blowerptr = getBlowerResult();
            double[] BlowerResultarr = new double[4];
            Marshal.Copy(Blowerptr, BlowerResultarr, 0, 4);
            freeResult(Blowerptr);
            for (int j = 0; j < BlowerResultarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = BlowerResultarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //AirHum
            IntPtr AirHumptr = getAirHumidifierResult();
            double[] AirHumarr = new double[4];
            Marshal.Copy(AirHumptr, AirHumarr, 0, 4);
            freeResult(AirHumptr);
            for (int j = 0; j < AirHumarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = AirHumarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //HygrogenHum
            IntPtr HydroenHumptr = getH2HumidifierResult();
            double[] HydrogenHumarr = new double[4];
            Marshal.Copy(HydroenHumptr, HydrogenHumarr, 0, 4);
            freeResult(HydroenHumptr);
            for (int j = 0; j < HydrogenHumarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = HydrogenHumarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Ejector
            IntPtr FreshWaterptr = getFreshWaterResult();
            double[] FreshWaterarr = new double[3];
            Marshal.Copy(FreshWaterptr, FreshWaterarr, 0, 3);
            freeResult(FreshWaterptr);
            for (int j = 0; j < FreshWaterarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = FreshWaterarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");

            //Pump
            IntPtr SeaWaterptr = getSeaWaterResult();
            double[] SeaWaterarr = new double[3];
            Marshal.Copy(SeaWaterptr, SeaWaterarr, 0, 3);
            freeResult(SeaWaterptr);
            for (int j = 0; j < SeaWaterarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = SeaWaterarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");

            //Valve
            IntPtr Valveptr = getValveResult();
            double[] Valvearr = new double[14];
            Marshal.Copy(Valveptr, Valvearr, 0, 14);
            freeResult(Valveptr);
            for (int j = 0; j < Valvearr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = Valvearr[j];
            }

            //Reducer
            IntPtr HeatExchangerptr = getHeatExchangerResult();
            double[] HeatExchangerarr = new double[2];
            Marshal.Copy(HeatExchangerptr, HeatExchangerarr, 0, 2);
            freeResult(HeatExchangerptr);
            for (int j = 0; j < HeatExchangerarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = HeatExchangerarr[j];
            }

            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Induction
            IntPtr InductionPtr = getInductionMotorResult();
            double[] Inductionarr = new double[2];
            Marshal.Copy(InductionPtr, Inductionarr, 0, 2);
            freeResult(InductionPtr);
            for (int j = 0; j < Inductionarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = Inductionarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");

            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Inverter
            IntPtr ConverterPtr = getConverterResult();
            double[] Converterarr = new double[3];
            Marshal.Copy(ConverterPtr, Converterarr, 0, 3);
            freeResult(ConverterPtr);
            for (int j = 0; j < Converterarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = Converterarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Intercooler
            IntPtr IntercoolerPtr = getIntercoolerResult();
            double[] Intercoolerarr = new double[4];
            Marshal.Copy(IntercoolerPtr, Intercoolerarr, 0, 4);
            freeResult(IntercoolerPtr);
            for (int j = 0; j < Intercoolerarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = Intercoolerarr[j];
            }
            //Debug.WriteLine($"FishingBoat 모델 내부4  {index}  {FishingBoatMWOuts[index - 1].Value}");
            //Battery
            IntPtr BatteryPtr = getBatteryResult();
            double[] Batteryarr = new double[4];
            Marshal.Copy(BatteryPtr, Batteryarr, 0, 4);
            freeResult(BatteryPtr);
            for (int j = 0; j < Batteryarr.Length; j++, index++)
            {
                FishingBoatMWOuts[index].Value = Batteryarr[j];
            }
            /* string csvFilePath = "FishingBoatdata.csv";

             using (StreamWriter writer = new StreamWriter(csvFilePath, true))
             {
                 string row = "";
                 for (int k = 0; k < FishingBoatMWOuts.Count; k++)
                 {
                     row = row + "," + FishingBoatMWOuts[k].Value.ToString();

                 }
                 writer.WriteLine(row);
             }*/

            step();
            OnDataReceived("");
        }
        public void Reset()
        {
            // 스레드 초기화
            if (CalculateThread != null && CalculateThread.IsAlive)
            {
                StopCalculation();
            }

            // ManualResetEvent 초기화
            manualEvent.Set();
            IsPause = false;
            Step = 0;

            // 관련 변수들 초기화
            ResetFishingBoatMWInputs();  // 입력값 초기화

            // 추가적으로 필요에 따라 다른 상태 변수들도 초기화
        }
        public void InitValue()
        {
            if (IsinitValue == false)
            {
                initialize(); //필수
                IsinitValue = true;
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "FishingBoat Running";
            }

            TextInputSlipProfile(FishingBoatMWInputs[0].Value);

            SetStack(FishingBoatMWInputs[1].Value, FishingBoatMWInputs[2].Value, FishingBoatMWInputs[3].Value, FishingBoatMWInputs[4].Value, FishingBoatMWInputs[5].Value, FishingBoatMWInputs[6].Value, FishingBoatMWInputs[7].Value, FishingBoatMWInputs[8].Value, FishingBoatMWInputs[9].Value, FishingBoatMWInputs[10].Value, FishingBoatMWInputs[11].Value, FishingBoatMWInputs[12].Value, FishingBoatMWInputs[13].Value, FishingBoatMWInputs[14].Value, FishingBoatMWInputs[15].Value, FishingBoatMWInputs[16].Value, FishingBoatMWInputs[17].Value, FishingBoatMWInputs[18].Value);

            SetBlower(FishingBoatMWInputs[19].Value, FishingBoatMWInputs[20].Value, FishingBoatMWInputs[21].Value, FishingBoatMWInputs[22].Value, FishingBoatMWInputs[23].Value, FishingBoatMWInputs[24].Value, FishingBoatMWInputs[25].Value, FishingBoatMWInputs[26].Value, FishingBoatMWInputs[27].Value, FishingBoatMWInputs[28].Value, FishingBoatMWInputs[29].Value, FishingBoatMWInputs[30].Value, FishingBoatMWInputs[31].Value, FishingBoatMWInputs[32].Value, FishingBoatMWInputs[33].Value, FishingBoatMWInputs[34].Value, FishingBoatMWInputs[35].Value, FishingBoatMWInputs[36].Value, FishingBoatMWInputs[37].Value, FishingBoatMWInputs[38].Value);

            SetHumidifier(FishingBoatMWInputs[39].Value, FishingBoatMWInputs[40].Value, FishingBoatMWInputs[41].Value, FishingBoatMWInputs[42].Value, FishingBoatMWInputs[43].Value, FishingBoatMWInputs[44].Value, FishingBoatMWInputs[45].Value, FishingBoatMWInputs[46].Value);

            SetFreshWaterPump(FishingBoatMWInputs[47].Value, FishingBoatMWInputs[48].Value, FishingBoatMWInputs[49].Value, FishingBoatMWInputs[50].Value);
            SetSeaWaterPump(FishingBoatMWInputs[51].Value, FishingBoatMWInputs[52].Value, FishingBoatMWInputs[53].Value, FishingBoatMWInputs[54].Value, FishingBoatMWInputs[55].Value);


            SetHeatExchanger(FishingBoatMWInputs[56].Value, FishingBoatMWInputs[57].Value, FishingBoatMWInputs[58].Value);



            SetIntercooler(FishingBoatMWInputs[59].Value, FishingBoatMWInputs[60].Value, FishingBoatMWInputs[61].Value, FishingBoatMWInputs[62].Value);


            /*    for(int i =0;i<FishingBoatMWInputs.Count;i++)
                            {
                                Console.WriteLine(i + "index = " + FishingBoatMWInputs[i].Value.ToString() + "\n");
                                System.Diagnostics.Debug.WriteLine(i + "index = " + FishingBoatMWInputs[i].Value.ToString() + "\n");
                                //System.Diagnostics.Trace.WriteLine(i + "index = " + FishingBoatMWInputs[i].Value.ToString() + "\n");

                            }*/

            /*   SetStack(298.15, 0.583, 343.15, 0.195, 0.195, 0.0025, 0.000042, 0.0002, 0.003, 0.001, 0.001, 0.002, 0.001, 32, 380, 404, 1.5, 2.5);
               SetBlower(0.0415, 0.0165, 0.0145, 0.0173, 0.00005, 28, 40, 345, 101325, 298.15, 0.5, 90, 0.11, 0.0037, 0.11, 0.078, 0.007, 6, 0.06, 0.0006);
               SetHumidifier(0.0508, 0.2488, 0.002, 0.099, 0.0009, 0.0001, 13000, 0.254);
               SetEjector(0.0021, 0.008);
               Setpump(0.033, 0.0585, 0.01, 0.0075, 20, 25, 7, 0.000045, 0.2647);
               InductionMotor(0.048, 0.000685, 0.0483, 0.000675, 0.0006, 0.5445, 0.7585, 2);
               SetReducer(7.981);
               SetInverter(400);
               SetIntercooler(0.1, 0.5, 298.15, 1.5);
               SetBattery(0.5, 4.2, 3, 64, 1, 0.8, 0.2, 0.95, 303.15, 192);
               SetRadiator(0.72, 0.66215, 0.0544, 0.0136, 0.0015, 0.00024, 0.00111, 0.00006, 0.0136, 0.0055, 0.005, 0.0009, 0.000315, 25);
   */
        }

        /// <summary>
        /// NaN 또는 Infinite 값을 0으로 변환하는 헬퍼 메서드
        /// </summary>
        private static double SanitizeValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0.0;
            }
            return value;
        }
    }
}

