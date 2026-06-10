using System.Collections.Generic;

namespace HoRang2Sea.ViewModels
{
    // 탑재 Output name → 컴포넌트(구 Simulink 접두어). 트리 그룹화용. 자동생성(rename_repo.py).
    public static class OutputComponentRegistry
    {
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            { "Air humidifier outlet relative humidity", "AH" },
            { "Anode gas temperature", "HH" },
            { "Anode gas total pressure", "HH" },
            { "Anode inlet mass flow rate", "HH" },
            { "Battery SOC(State Of Charge)", "BAT" },
            { "Battery output current", "BAT" },
            { "Battery output power", "BAT" },
            { "Battery output voltage", "BAT" },
            { "Bypass coolant inlet pressure", "TVW" },
            { "Bypass coolant mass flow rate", "TVW" },
            { "Bypass coolant temperature", "TVW" },
            { "Cathode gas temperature", "AH" },
            { "Cathode gas total pressure", "AH" },
            { "Compressor air mass flow rate", "Bwr" },
            { "Compressor efficiency", "Bwr" },
            { "Compressor torque", "Bwr" },
            { "Converter current", "DCC" },
            { "Converter voltage", "DCC" },
            { "Duty ratio", "DCC" },
            { "Electric power", "IM" },
            { "Fresh water pump coolant outlet pressure", "FWP" },
            { "Fresh water pump cooling water flow rate", "FWP" },
            { "Fresh water pump cooling water temperature", "FWP" },
            { "Heat exchanger coolant outlet temperature", "HX" },
            { "Heat exchanger sea water outlet temperature", "HX" },
            { "Hydrogen humidifier water mole fraction", "HH" },
            { "Inlet current density", "FCS" },
            { "Intercooler outlet air mass flow rate", "IC" },
            { "Intercooler outlet air temperature", "IC" },
            { "Intercooler outlet pressure", "IC" },
            { "Intercooler outlet relative humidity", "IC" },
            { "Motor RPM", "IM" },
            { "Plenum downstream pressure", "Bwr" },
            { "Sea water pump sea water flow rate", "SWP" },
            { "Sea water pump sea water outlet pressure", "SWP" },
            { "Sea water pump sea water temperature", "SWP" },
            { "Stack OCV", "FCS" },
            { "Stack bypass coolant flow rate", "TWV" },
            { "Stack bypass coolant inlet pressure", "TWV" },
            { "Stack bypass coolant temperature", "TWV" },
            { "Stack coolant flow rate", "TWV" },
            { "Stack coolant pressure", "TWV" },
            { "Stack coolant temperature", "TWV" },
            { "Stack net power", "FCS" },
            { "Stack temperature", "FCS" },
            { "Stack valve opening ratio", "TWV" },
            { "Stack voltage", "FCS" },
            { "Total humidified air mass flow rate", "AH" },
            { "Valve opening ratio", "TWV" },
            { "heatexchanger coolant inlet pressure", "TWV" },
            { "heatexchanger coolant mass flow rate", "TVW" },
            { "heatexchanger coolant temperature", "TVW" },
        };
    }
}
