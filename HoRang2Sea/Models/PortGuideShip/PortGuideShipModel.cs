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

    public class PortGuideShipMWModel : BaseModel
    {
        /*  public event EventHandler<DataReceivedEventArgs> DataReceived;
          protected virtual void OnDataReceived(string data) => DataReceived?.Invoke(this, new DataReceivedEventArgs(data));*/
    }

    public class PortGuideShipMW : PortGuideShipMWModel
    {
        public Thread CalculateThread { get; set; }
        public bool IsinitValue { get; set; }
        public int Step { get; set; }
        public bool IsPause { get; set; }
        public static ManualResetEvent manualEvent = new ManualResetEvent(true);
        private double[] _driveModes; // 업로드된 파일의 라인 저장
        private CancellationTokenSource _cancellationTokenSource;

        #region DLLImport
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void initialize();

        [DllImport("PortGuideShipDLL.dll")]
        public static extern void terminate();

        [DllImport("PortGuideShipDLL.dll")]
        public static extern void step();

        [DllImport("PortGuideShipDLL.dll")]
        public static extern void TextInputSlipProfile
        (
            double Slip
        );
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void SetProPellerPitch
        (
            double Propellerpitch
        );
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void Setmode
        (
            double Navigationmode
        );


        [DllImport("PortGuideShipDLL.dll")]
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

        [DllImport("PortGuideShipDLL.dll")]
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
        [DllImport("PortGuideShipDLL.dll")]
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

        [DllImport("PortGuideShipDLL.dll")]
        public static extern void SetFreshWaterPump
        (
            double FWP_efficiency,
            double FWP_intlet_pressure,
            double FWP_outlet_pressure,
            double FWP_max_flow_rate

        );
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void SetSeaWaterPump
        (
            double SWP_efficiency,
            double SWP_intlet_pressure,
            double SWP_outlet_pressure,
            double SWP_max_flow_rate,
            double SWP_sea_water_flow_rate
        );
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void SetHeatExchanger
        (
            double HX_active_area,
            double HX_Overall_heat_transfer_coefficient,
            double HX_temperature_setting

        );
        [DllImport("PortGuideShipDLL.dll")]
        public static extern void SetIntercooler
        (
            double IC_inlet_Mass_flow_rate,
            double IC_Inlet_relative_humidity,
            double IC_inlet_temperature,
            double IC_Intercooler_area
        );



        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void freeResult(IntPtr array);

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getDriveModeResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getStackResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBlowerResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getAirHumidifierResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getH2HumidifierResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getFreshWaterResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getSeaWaterResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getValveResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getHeatExchangerResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getInductionMotorResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]

        public static extern IntPtr getConverterResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getIntercoolerResult();

        [DllImport("PortGuideShipDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBatteryResult();


        #endregion

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

        };

        public List<PortGuideShipMWDataModel> PortGuideShipMWOuts = new()
        {
			//PortGuideShipMode  ( 개수 : 3 )
			new("Navigation_mode", "-", "PortGuideShipProfile"),
            new("FB_displacement", "miles", "PortGuideShipProfile"),
            new("FB_fuelefficiency", "miles/kg", "PortGuideShipProfile"),

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

        public PortGuideShipMW()
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
        /// 물리적으로 의미있는 가라데이터 생성 (PortGuideShip용 - 항만안내선 연료전지 시스템)
        /// 변수별로 다른 랜덤 패턴 적용
        /// </summary>
        private void GenerateFakeData(int step, int maxSteps)
        {
            double t = (double)step / maxSteps;
            double phase = step * 0.001;
            double layoutMultiplier = GetLayoutMultiplier();
            double noiseLevel = GetNoiseLevel();
            double phaseOffset = GetPhaseOffset();
            double tempRise = Math.Min(t * 2.5, 1.0);

            int index = 0;

            // PortGuideShipProfile (3개)
            double navMode = t < 0.2 ? 1.0 : (t < 0.8 ? 2.0 : 3.0);
            PortGuideShipMWOuts[index++].Value = navMode;
            double displacement = 30.0 * t;
            if (t > 0.8) displacement = 30.0 - 30.0 * (t - 0.8) / 0.2;
            PortGuideShipMWOuts[index++].Value = Math.Max(0, displacement + noiseLevel * 0.3 * Math.Sin(phase * 0.3 + phaseOffset));
            double fuelEff = 2.2 + 0.6 * (1.0 - GetProfileFactor(t, index) * 0.4);
            if (ControlLayout == 1) fuelEff += 0.25;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(fuelEff + noiseLevel * 0.12 * Math.Sin(phase * 1.0 + phaseOffset), 1.4, 3.0);

            // Stack (5개)
            double currentDensity = 0.25 + 0.5 * GetProfileFactor(t, index) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(currentDensity + noiseLevel * 0.04 * Math.Sin(phase * 2.0 + phaseOffset), 0.1, 0.85);
            double stackVoltage = 200.0 + 25.0 * (1.0 - GetProfileFactor(t, index) * 0.3) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(stackVoltage + noiseLevel * 4.0 * Math.Sin(phase * 1.5 + phaseOffset), 150, 240);
            double netPower = 15.0 + 45.0 * GetProfileFactor(t, index) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Max(8, netPower + noiseLevel * 4.0 * Math.Sin(phase * 1.8 + phaseOffset));
            double ocvVoltage = 260.0 + 18.0 * (1.0 - GetProfileFactor(t, index) * 0.2);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(ocvVoltage + noiseLevel * 2.5 * Math.Sin(phase * 1.2 + phaseOffset), 240, 290);
            double stackTemp = 325.0 + 30.0 * tempRise * GetProfileFactor(t, index);
            if (DesignLayout == 1) stackTemp += 4.0;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(stackTemp + noiseLevel * 2.5 * Math.Sin(phase * 0.8 + phaseOffset), 315, 365);

            // Blower (4개)
            double compTorque = 1.5 + 4.5 * GetProfileFactor(t, index) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Max(0.8, compTorque + noiseLevel * 0.4 * Math.Sin(phase * 2.5 + phaseOffset));
            double compEff = 0.65 + 0.12 * (1.0 - Math.Abs(GetProfileFactor(t, index) - 0.65));
            if (ControlLayout == 1) compEff += 0.025;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(compEff + noiseLevel * 0.025 * Math.Sin(phase * 1.1 + phaseOffset), 0.52, 0.82);
            double airMassFlow = 0.015 + 0.045 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.008, airMassFlow + noiseLevel * 0.004 * Math.Sin(phase * 2.3 + phaseOffset));
            double outletPressure = 140000 + 80000 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(110000, outletPressure + noiseLevel * 4000 * Math.Sin(phase * 1.9 + phaseOffset));

            // Air Humidifier (4개)
            double humAirFlow = 0.02 + 0.05 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.012, humAirFlow + noiseLevel * 0.004 * Math.Sin(phase * 2.0 + phaseOffset));
            double cathodeTemp = 305.0 + 25.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(cathodeTemp + noiseLevel * 2.0 * Math.Sin(phase * 1.3 + phaseOffset), 295, 345);
            double cathodePressure = 140000 + 60000 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(120000, cathodePressure + noiseLevel * 3500 * Math.Sin(phase * 1.7 + phaseOffset));
            double outletRH = 0.75 + 0.18 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(outletRH + noiseLevel * 0.025 * Math.Sin(phase * 1.4 + phaseOffset), 0.58, 0.98);

            // Hydrogen Humidifier (4개)
            double anodeMassFlow = 0.0008 + 0.0025 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.0004, anodeMassFlow + noiseLevel * 0.00025 * Math.Sin(phase * 2.1 + phaseOffset));
            double waterMoleFrac = 0.12 + 0.14 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(waterMoleFrac + noiseLevel * 0.018 * Math.Sin(phase * 1.6 + phaseOffset), 0.04, 0.35);
            double anodeTemp = 305.0 + 30.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(anodeTemp + noiseLevel * 2.0 * Math.Sin(phase * 1.2 + phaseOffset), 295, 350);
            double anodePressure = 160000 + 90000 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(140000, anodePressure + noiseLevel * 4500 * Math.Sin(phase * 1.8 + phaseOffset));

            // Fresh Water Pump (3개)
            double fwpFlowRate = 0.4 + 1.2 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.25, fwpFlowRate + noiseLevel * 0.1 * Math.Sin(phase * 2.2 + phaseOffset));
            double fwpOutTemp = 292.0 + 22.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(fwpOutTemp + noiseLevel * 1.8 * Math.Sin(phase * 0.9 + phaseOffset), 288, 325);
            double fwpOutPressure = 1.4 + 1.8 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.9, fwpOutPressure + noiseLevel * 0.18 * Math.Sin(phase * 2.0 + phaseOffset));

            // Sea Water Pump (3개)
            double swpFlowRate = 0.8 + 2.5 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.5, swpFlowRate + noiseLevel * 0.2 * Math.Sin(phase * 2.1 + phaseOffset));
            double swpOutTemp = 283.0 + 18.0 * tempRise * GetProfileFactor(t, index) * 0.4;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(swpOutTemp + noiseLevel * 1.2 * Math.Sin(phase * 0.7 + phaseOffset), 278, 310);
            double swpOutPressure = 1.1 + 1.4 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.7, swpOutPressure + noiseLevel * 0.12 * Math.Sin(phase * 1.9 + phaseOffset));

            // Valve (14개)
            double valveRatio = 0.28 + 0.55 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(valveRatio + noiseLevel * 0.045 * Math.Sin(phase * 1.5 + phaseOffset), 0.1, 0.95);
            double bypassFlow = 0.08 + 0.6 * (1.0 - GetProfileFactor(t, index));
            PortGuideShipMWOuts[index++].Value = Math.Max(0.04, bypassFlow + noiseLevel * 0.05 * Math.Sin(phase * 1.8 + phaseOffset));
            double bypassTemp = 305.0 + 32.0 * tempRise;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(bypassTemp + noiseLevel * 2.2 * Math.Sin(phase * 1.1 + phaseOffset), 295, 355);
            double bypassPressure = 140.0 + 90.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(110, bypassPressure + noiseLevel * 7.0 * Math.Sin(phase * 1.6 + phaseOffset));
            double hxFlowRate = 0.25 + 1.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.15, hxFlowRate + noiseLevel * 0.08 * Math.Sin(phase * 2.0 + phaseOffset));
            double hxCoolantTemp = 318.0 + 32.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(hxCoolantTemp + noiseLevel * 2.5 * Math.Sin(phase * 1.0 + phaseOffset), 308, 365);
            double hxInletPressure = 165.0 + 90.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(140, hxInletPressure + noiseLevel * 7.0 * Math.Sin(phase * 1.7 + phaseOffset));
            double stackValveRatio = 0.35 + 0.5 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(stackValveRatio + noiseLevel * 0.035 * Math.Sin(phase * 1.4 + phaseOffset), 0.18, 0.95);
            double stackBypassFlow = 0.06 + 0.45 * (1.0 - GetProfileFactor(t, index));
            PortGuideShipMWOuts[index++].Value = Math.Max(0.03, stackBypassFlow + noiseLevel * 0.04 * Math.Sin(phase * 1.9 + phaseOffset));
            double stackBypassTemp = 310.0 + 32.0 * tempRise;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(stackBypassTemp + noiseLevel * 2.2 * Math.Sin(phase * 1.2 + phaseOffset), 300, 358);
            double stackBypassPressure = 150.0 + 90.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(120, stackBypassPressure + noiseLevel * 7.0 * Math.Sin(phase * 1.5 + phaseOffset));
            double stackCoolantFlow = 0.35 + 1.2 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.2, stackCoolantFlow + noiseLevel * 0.1 * Math.Sin(phase * 2.1 + phaseOffset));
            double stackCoolantTemp = 322.0 + 32.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(stackCoolantTemp + noiseLevel * 2.5 * Math.Sin(phase * 0.9 + phaseOffset), 312, 370);
            double stackCoolantPressure = 160.0 + 90.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(130, stackCoolantPressure + noiseLevel * 7.0 * Math.Sin(phase * 1.8 + phaseOffset));

            // Heat Exchanger (2개)
            double hxCoolantOutlet = 42.0 + 28.0 * tempRise * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(hxCoolantOutlet + noiseLevel * 2.2 * Math.Sin(phase * 1.0 + phaseOffset), 32, 80);
            double hxSeaWaterOutlet = 16.0 + 15.0 * tempRise * GetProfileFactor(t, index) * 0.5;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(hxSeaWaterOutlet + noiseLevel * 1.2 * Math.Sin(phase * 0.8 + phaseOffset), 10, 38);

            // Induction Motor (2개)
            double motorRPM = 450 + 1100 * GetProfileFactor(t, index) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Max(280, motorRPM + noiseLevel * 70 * Math.Sin(phase * 2.5 + phaseOffset));
            double motorPower = 12.0 + 38.0 * GetProfileFactor(t, index) * layoutMultiplier;
            PortGuideShipMWOuts[index++].Value = Math.Max(6, motorPower + noiseLevel * 3.5 * Math.Sin(phase * 2.2 + phaseOffset));

            // Converter (3개)
            double dutyRatio = 0.38 + 0.48 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(dutyRatio + noiseLevel * 0.035 * Math.Sin(phase * 1.6 + phaseOffset), 0.22, 0.95);
            double convVoltage = 380.0 + 45.0 * (0.5 - Math.Abs(GetProfileFactor(t, index) - 0.5));
            PortGuideShipMWOuts[index++].Value = Math.Clamp(convVoltage + noiseLevel * 4.5 * Math.Sin(phase * 1.3 + phaseOffset), 320, 460);
            double convCurrent = 40.0 + 110.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(25, convCurrent + noiseLevel * 9.0 * Math.Sin(phase * 2.0 + phaseOffset));

            // Intercooler (4개)
            double icMassFlow = 0.02 + 0.05 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(0.012, icMassFlow + noiseLevel * 0.004 * Math.Sin(phase * 2.1 + phaseOffset));
            double icOutTemp = 295.0 + 22.0 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(icOutTemp + noiseLevel * 1.8 * Math.Sin(phase * 1.4 + phaseOffset), 285, 330);
            double icOutPressure = 130000 + 55000 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Max(110000, icOutPressure + noiseLevel * 3500 * Math.Sin(phase * 1.7 + phaseOffset));
            double icOutRH = 0.55 + 0.32 * GetProfileFactor(t, index);
            PortGuideShipMWOuts[index++].Value = Math.Clamp(icOutRH + noiseLevel * 0.028 * Math.Sin(phase * 1.5 + phaseOffset), 0.42, 0.95);

            // Battery (4개)
            double soc = 0.82 - 0.38 * t;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(soc + noiseLevel * 0.01 * Math.Sin(phase * 0.5 + phaseOffset), 0.32, 0.92);
            double battVoltage = 355.0 + 55.0 * (soc - 0.44) / 0.38;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(battVoltage + noiseLevel * 3.5 * Math.Sin(phase * 1.6 + phaseOffset), 335, 430);
            double battCurrent = -35.0 + 120.0 * GetProfileFactor(t, index);
            if (ControlLayout == 1) battCurrent *= 0.88;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(battCurrent + noiseLevel * 9.0 * Math.Sin(phase * 2.0 + phaseOffset), -55, 115);
            double battPower = -18.0 + 60.0 * GetProfileFactor(t, index);
            if (ControlLayout == 1) battPower *= 0.88;
            PortGuideShipMWOuts[index++].Value = Math.Clamp(battPower + noiseLevel * 4.5 * Math.Sin(phase * 1.9 + phaseOffset), -28, 55);
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
        /// 운항 프로파일에 따른 파워 계수 - PortGuideShip 전용 (주가 스타일 변동)
        /// varIndex로 변수별 다른 패턴 생성
        /// </summary>
        private double GetProfileFactor(double t, int varIndex = 0)
        {
            // 변수별로 다른 주파수와 진폭 (varIndex를 사용해 변수마다 다른 패턴)
            double freq1 = 41 + (varIndex * 7) % 30;
            double freq2 = 29 + (varIndex * 11) % 25;
            double freq3 = 61 + (varIndex * 13) % 35;
            double amp1 = 0.08 + (varIndex % 5) * 0.02;
            double amp2 = 0.06 + (varIndex % 4) * 0.015;
            double amp3 = 0.04 + (varIndex % 3) * 0.01;

            // 주가 스타일 변동 (변수별로 다른 주파수)
            double stockWave = amp1 * Math.Sin(t * freq1) + amp2 * Math.Sin(t * freq2) + amp3 * Math.Sin(t * freq3);

            if (DesignLayout == 0 && ControlLayout == 0)
            {
                // 기본: 완만한 상승 후 유지 + 변동
                double trend = t < 0.3 ? (0.3 + 1.5 * t) : 0.75;
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
            else if (DesignLayout == 0 && ControlLayout == 1)
            {
                // Control: 계단식 상승 + 변동
                double trend = 0.2 + 0.2 * Math.Floor(t * 4);
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
            else if (DesignLayout == 1 && ControlLayout == 0)
            {
                // Design: 급등 후 급락 + 변동
                double trend = t < 0.4 ? (0.2 + 1.8 * t) : (0.92 - 0.8 * (t - 0.4));
                double bigWave = 0.15 * Math.Sin(t * (19 + varIndex % 10)) + 0.12 * Math.Sin(t * (11 + varIndex % 8));
                return Math.Clamp(trend + stockWave + bigWave, 0.1, 1.0);
            }
            else // DesignLayout == 1 && ControlLayout == 1
            {
                // Full: 쌍봉 (M자) 패턴 + 변동
                double peak1 = 0.7 * Math.Exp(-Math.Pow((t - 0.25) * 6, 2));
                double peak2 = 0.7 * Math.Exp(-Math.Pow((t - 0.75) * 6, 2));
                double trend = 0.3 + peak1 + peak2;
                return Math.Clamp(trend + stockWave, 0.1, 1.0);
            }
        }

        public void ResetPortGuideShipMWInputs()
        {
            // 기존 데이터를 모두 삭제
            PortGuideShipMWInputs.Clear();

            // 필요한 데이터를 재설정
            PortGuideShipMWInputs = new List<PortGuideShipMWDataModel>
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

            };
        }

        public void ResetPortGuideShipMWOuts()
        {
            // 기존 데이터를 모두 삭제
            PortGuideShipMWOuts.Clear();

            // 필요한 데이터를 재설정
            PortGuideShipMWOuts = new List<PortGuideShipMWDataModel>
            {
				//PortGuideShipMode  ( 개수 : 3 )
				new("Navigation_mode", "-", "PortGuideShipProfile"),
                new("FB_displacement", "miles", "PortGuideShipProfile"),
                new("FB_fuelefficiency", "miles/kg", "PortGuideShipProfile"),

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
                Debug.WriteLine("PortGuideShip 모델 시작");
                int Maxiterations = _driveModes != null ? Math.Min(_driveModes.Length, 5000000) : 5000000;

                // token의 WaitHandle과 manualEvent를 함께 사용하여 대기할 준비
                WaitHandle[] waitHandles = new WaitHandle[] { token.WaitHandle, manualEvent };
                Debug.WriteLine($"PortGuideShip 모델 내부1  {_driveModes.Length}");
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
                mv.Status = "PortGuideShip Finished";
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

            foreach (var output in PortGuideShipMWOuts)
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
                PortGuideShipMWOuts[index].Value = DriveResultarr[j];
            }

            //Stack
            IntPtr Stackptr = getStackResult();
            double[] StackResultarr = new double[5];
            Marshal.Copy(Stackptr, StackResultarr, 0, 5);

            freeResult(Stackptr);
            for (int j = 0; j < StackResultarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = StackResultarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Blower
            IntPtr Blowerptr = getBlowerResult();
            double[] BlowerResultarr = new double[4];
            Marshal.Copy(Blowerptr, BlowerResultarr, 0, 4);
            freeResult(Blowerptr);
            for (int j = 0; j < BlowerResultarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = BlowerResultarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //AirHum
            IntPtr AirHumptr = getAirHumidifierResult();
            double[] AirHumarr = new double[4];
            Marshal.Copy(AirHumptr, AirHumarr, 0, 4);
            freeResult(AirHumptr);
            for (int j = 0; j < AirHumarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = AirHumarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //HygrogenHum
            IntPtr HydroenHumptr = getH2HumidifierResult();
            double[] HydrogenHumarr = new double[4];
            Marshal.Copy(HydroenHumptr, HydrogenHumarr, 0, 4);
            freeResult(HydroenHumptr);
            for (int j = 0; j < HydrogenHumarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = HydrogenHumarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Ejector
            IntPtr FreshWaterptr = getFreshWaterResult();
            double[] FreshWaterarr = new double[3];
            Marshal.Copy(FreshWaterptr, FreshWaterarr, 0, 3);
            freeResult(FreshWaterptr);
            for (int j = 0; j < FreshWaterarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = FreshWaterarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");

            //Pump
            IntPtr SeaWaterptr = getSeaWaterResult();
            double[] SeaWaterarr = new double[3];
            Marshal.Copy(SeaWaterptr, SeaWaterarr, 0, 3);
            freeResult(SeaWaterptr);
            for (int j = 0; j < SeaWaterarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = SeaWaterarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");

            //Valve
            IntPtr Valveptr = getValveResult();
            double[] Valvearr = new double[14];
            Marshal.Copy(Valveptr, Valvearr, 0, 14);
            freeResult(Valveptr);
            for (int j = 0; j < Valvearr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = Valvearr[j];
            }

            //Reducer
            IntPtr HeatExchangerptr = getHeatExchangerResult();
            double[] HeatExchangerarr = new double[2];
            Marshal.Copy(HeatExchangerptr, HeatExchangerarr, 0, 2);
            freeResult(HeatExchangerptr);
            for (int j = 0; j < HeatExchangerarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = HeatExchangerarr[j];
            }

            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Induction
            IntPtr InductionPtr = getInductionMotorResult();
            double[] Inductionarr = new double[2];
            Marshal.Copy(InductionPtr, Inductionarr, 0, 2);
            freeResult(InductionPtr);
            for (int j = 0; j < Inductionarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = Inductionarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");

            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Inverter
            IntPtr ConverterPtr = getConverterResult();
            double[] Converterarr = new double[3];
            Marshal.Copy(ConverterPtr, Converterarr, 0, 3);
            freeResult(ConverterPtr);
            for (int j = 0; j < Converterarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = Converterarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Intercooler
            IntPtr IntercoolerPtr = getIntercoolerResult();
            double[] Intercoolerarr = new double[4];
            Marshal.Copy(IntercoolerPtr, Intercoolerarr, 0, 4);
            freeResult(IntercoolerPtr);
            for (int j = 0; j < Intercoolerarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = Intercoolerarr[j];
            }
            //Debug.WriteLine($"PortGuideShip 모델 내부4  {index}  {PortGuideShipMWOuts[index - 1].Value}");
            //Battery
            IntPtr BatteryPtr = getBatteryResult();
            double[] Batteryarr = new double[4];
            Marshal.Copy(BatteryPtr, Batteryarr, 0, 4);
            freeResult(BatteryPtr);
            for (int j = 0; j < Batteryarr.Length; j++, index++)
            {
                PortGuideShipMWOuts[index].Value = Batteryarr[j];
            }
            /* string csvFilePath = "PortGuideShipdata.csv";

             using (StreamWriter writer = new StreamWriter(csvFilePath, true))
             {
                 string row = "";
                 for (int k = 0; k < PortGuideShipMWOuts.Count; k++)
                 {
                     row = row + "," + PortGuideShipMWOuts[k].Value.ToString();

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
            ResetPortGuideShipMWInputs();  // 입력값 초기화

            // 추가적으로 필요에 따라 다른 상태 변수들도 초기화
        }
        public void InitValue()
        {
            if (IsinitValue == false)
            {
                initialize(); //필수
                IsinitValue = true;
                var mv = App.Container.GetInstance<MainViewModel>();
                mv.Status = "PortGuideShip Running";
            }
            SetProPellerPitch(PortGuideShipMWInputs[0].Value);
            TextInputSlipProfile(PortGuideShipMWInputs[1].Value);

            SetStack(PortGuideShipMWInputs[2].Value, PortGuideShipMWInputs[3].Value, PortGuideShipMWInputs[4].Value, PortGuideShipMWInputs[5].Value, PortGuideShipMWInputs[6].Value, PortGuideShipMWInputs[7].Value, PortGuideShipMWInputs[8].Value, PortGuideShipMWInputs[9].Value, PortGuideShipMWInputs[10].Value, PortGuideShipMWInputs[11].Value, PortGuideShipMWInputs[12].Value, PortGuideShipMWInputs[13].Value, PortGuideShipMWInputs[14].Value, PortGuideShipMWInputs[15].Value, PortGuideShipMWInputs[16].Value, PortGuideShipMWInputs[17].Value, PortGuideShipMWInputs[18].Value, PortGuideShipMWInputs[19].Value);

            SetBlower(PortGuideShipMWInputs[20].Value, PortGuideShipMWInputs[21].Value, PortGuideShipMWInputs[22].Value, PortGuideShipMWInputs[23].Value, PortGuideShipMWInputs[24].Value, PortGuideShipMWInputs[25].Value, PortGuideShipMWInputs[26].Value, PortGuideShipMWInputs[27].Value, PortGuideShipMWInputs[28].Value, PortGuideShipMWInputs[29].Value, PortGuideShipMWInputs[30].Value, PortGuideShipMWInputs[31].Value, PortGuideShipMWInputs[32].Value, PortGuideShipMWInputs[33].Value, PortGuideShipMWInputs[34].Value, PortGuideShipMWInputs[35].Value, PortGuideShipMWInputs[36].Value, PortGuideShipMWInputs[37].Value, PortGuideShipMWInputs[38].Value, PortGuideShipMWInputs[39].Value);

            SetHumidifier(PortGuideShipMWInputs[40].Value, PortGuideShipMWInputs[41].Value, PortGuideShipMWInputs[42].Value, PortGuideShipMWInputs[43].Value, PortGuideShipMWInputs[44].Value, PortGuideShipMWInputs[45].Value, PortGuideShipMWInputs[46].Value, PortGuideShipMWInputs[47].Value);

            SetFreshWaterPump(PortGuideShipMWInputs[48].Value, PortGuideShipMWInputs[49].Value, PortGuideShipMWInputs[50].Value, PortGuideShipMWInputs[51].Value);
            SetSeaWaterPump(PortGuideShipMWInputs[52].Value, PortGuideShipMWInputs[53].Value, PortGuideShipMWInputs[54].Value, PortGuideShipMWInputs[55].Value, PortGuideShipMWInputs[56].Value);


            SetHeatExchanger(PortGuideShipMWInputs[57].Value, PortGuideShipMWInputs[58].Value, PortGuideShipMWInputs[59].Value);



            SetIntercooler(PortGuideShipMWInputs[60].Value, PortGuideShipMWInputs[61].Value, PortGuideShipMWInputs[62].Value, PortGuideShipMWInputs[63].Value);


            /*    for(int i =0;i<PortGuideShipMWInputs.Count;i++)
                            {
                                Console.WriteLine(i + "index = " + PortGuideShipMWInputs[i].Value.ToString() + "\n");
                                System.Diagnostics.Debug.WriteLine(i + "index = " + PortGuideShipMWInputs[i].Value.ToString() + "\n");
                                //System.Diagnostics.Trace.WriteLine(i + "index = " + PortGuideShipMWInputs[i].Value.ToString() + "\n");

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

