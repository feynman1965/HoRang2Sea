using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HoRang2Sea.Common
{
    public class NexoDllImport
    {
        #region DLLImport
        [DllImport("NexoDLL.dll")]
        public static extern void initialize();

        [DllImport("NexoDLL.dll")]
        public static extern void step();

        [DllImport("NexoDLL.dll")]
        public static extern void SetStack(double AmbientTemp, double Channellength, double OperationTemp, double Celllength, double CellWidth,
    double MembThick, double CatalystThick, double GDLThick, double BiopolarThick, double DepthChannel, double WidthChannel,
    double ChannelPitch, double ChannelThick, double NumberofChannel, double Activearea, double NumberofCell, double Anodestoi,
    double Cathodestoi);

        [DllImport("NexoDLL.dll")]
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
        [DllImport("NexoDLL.dll")]
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
        [DllImport("NexoDLL.dll")]
        public static extern void SetEjector
    (
    double Ejector_throat_diameter,
    double Ejector_mixing_chamber_diameter
    );
        [DllImport("NexoDLL.dll")]
        public static extern void Setpump
    (
    double Pump_impeller_inlet_diameter,
    double Pump_impeller_outlet_diameter,
    double Pump_blade_inlet_height,
    double Pump_blade_outlet_height,
    double Pump_blade_inlet_angle,
    double Pump_blade_outlet_angle,
    double Pump_blade_numbers,
    double Pump_impeller_roughness,
    double Pump_impeller_volute_diameter
    );
        [DllImport("NexoDLL.dll")]
        public static extern void InductionMotor
    (
    double Stator_resistance_Rs,
    double Stator_inductance_Ls,
    double Rotor_resistance_Rr,
    double Rotor_inductance_Lr,
    double Mutual_inductance_Lm,
    double Moment_of_intertia_Jm,
    double Damping_coefficient_B,
    double Induction_motor_pole_P
    );
        [DllImport("NexoDLL.dll")]
        public static extern void SetReducer
    (
    double Reducing_ratio
    );

        [DllImport("NexoDLL.dll")]
        public static extern void SetInverter
    (
    double Inverter_rated_voltage
    );
        [DllImport("NexoDLL.dll")]
        public static extern void SetIntercooler
    (
    double Intercooler_inlet_coolant_mass_flow_rate,
    double Intercooler_inlet_relative_humidity,
    double Intercooler_inlet_coolant_temperature,
    double Intercooler_area
    );

        [DllImport("NexoDLL.dll")]
        public static extern void SetBattery
    (
    double Battery_initial_SOC,
    double Battery_maximum_voltage,
    double Battery_minimum_voltage,
    double Number_of_battery_modules,
    double Battery_parallel_mod_number,
    double SOC_maximum_limit,
    double SOC_minimum_limit,
    double Power_bus_efficiency,
    double Battery_average_temperature,
    double Motor_minimum_voltage
    );

        [DllImport("NexoDLL.dll")]
        public static extern void SetRadiator
    (
    double Radiator_core_width,
    double Radiator_core_height,
    double Radiator_core_depth,
    double Radiator_tube_depth,
    double Radiator_tube_height,
    double Radiator_tube_thickness,
    double Radiator_fin_pitch,
    double Radiator_fin_thickness,
    double Radiator_fin_depth,
    double Radiator_fin_length,
    double Radiator_louver_length,
    double Radiator_louver_pitch,
    double Radiator_louver_height,
    double Radiator_louver_angle
    );

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void freeResult(IntPtr array);

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getStackResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBlowerResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getAirHumidifierResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getHydrogenHumidifierResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getEjectorResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getPumpResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getValveResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getInductionMotorResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getReducerResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getInverterResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getDCResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getIntercoolerResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getBatteryResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getRadiatorResult();

        [DllImport("NexoDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr getFanResult();

        #endregion
    }
}
