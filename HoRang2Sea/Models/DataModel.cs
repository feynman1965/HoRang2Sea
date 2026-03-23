using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

# nullable enable

namespace HoRang2Sea.Models
{
    public enum TextType { Default, RelValue, Description, ValueOnly, ValueUnit, NameNValue, NameNValueUnit };
    public class DataModel : INotifyPropertyChanged, ICloneable
    {
        private string _module = "";
        private string _name = "";
        private string? _unit;
        private string? _description;
        private double _value;
        private double[]? _valueArray;
        private string[]? _nameArray;
        private string _raw = string.Empty;
        private string _type = "String.Double";
        private DateTime _time;
        private int _decimal = 3;
        private int id = 0;
        private int length = 1;

        public TextType TextType { get; set; } = TextType.Default;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public int ID
        {
            get => id;
            set
            {
                id = value;
            }
        }
        public int Decimal
        {
            get => _decimal;
            set
            {
                if (value <= 12 && value >= -2)
                    _decimal = value;
                OnPropertyChanged("Decimal");
            }
        }
        public string FullName { get; private set; } = String.Empty;
        public DateTime Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
                OnPropertyChanged("Time");
            }
        }

        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");

                //_valueString = ConvertString(_value, Decimal);
                //_absValueString = AbsConvertString(_value, Decimal);
                //IsNumericValue = true;
                //OnPropertyChanged("Value");
                //OnPropertyChanged("ValueString");
                //OnPropertyChanged("AbsValueString");
            }
        }
        public string Raw
        {
            get
            {
                return _raw;
            }
            set
            {
                _raw = value;
            }
        }
        //public string ValueString
        // {
        //     get
        //     {
        //         return _valueString;
        //     }
        //     set
        //     {
        //         _valueString = value;
        //         _absValueString = value;
        //         OnPropertyChanged("ValueString");
        //         OnPropertyChanged("AbsValueString");

        //         Double.TryParse(_valueString, out double v);
        //         if (v == 0 && !(_valueString == "0.0000000000E+000" || _valueString == "0000" || _valueString == "0"))
        //         {
        //             IsNumericValue = false;
        //         }
        //         else
        //         {
        //             IsNumericValue = true;
        //             Value = v;
        //         }
        //     }
        // }
        // public string AbsValueString
        // {
        //     get
        //     {
        //         return _absValueString;
        //     }
        // }
        public string Module
        {
            get
            {
                return _module;
            }
            set
            {
                _module = value;
                FullName = $"{_module}.{_name}";
                OnPropertyChanged("Module");
                OnPropertyChanged("FullName");
            }
        }
        public string Name
        {
            get
            {
                return $"{_name}";
            }
            set
            {
                _name = value;
                FullName = $"{_module}.{_name}";
                OnPropertyChanged("Name");
                OnPropertyChanged("FullName");
            }
        }
        public string Unit
        {
            get
            {
                if (_unit != null)
                    return _unit;
                else
                    return string.Empty;
            }
            set
            {
                _unit = value;
                OnPropertyChanged("Unit");
            }
        }
        public string Description
        {
            get
            {
                if (_description != null)
                    return _description;
                else
                    return string.Empty;
            }
            set
            {
                _description = value;
                OnPropertyChanged("Description");
            }
        }

        public double[]? ValueArray { get => _valueArray; }
        public string[]? NameArray { get => _nameArray; }

        public DataModel()
        {
        }
        public DataModel(string name, string type, string raw)
        {
            Name = name;
            Type = type;
            Raw = raw;
            UpdateData();
            switch (Type)
            {
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.Boolean[]":
                    {
                        CreateNameArray();
                        break;
                    }
            }
        }

        private void CreateNameArray()
        {
            _nameArray = new string[length];
            for (int i = 0; i < length; i++)
            {
                if (length < 10)
                {
                    _nameArray[i] = $"{i:0}";
                }
                else if (length < 100)
                {
                    _nameArray[i] = $"{i:00}";
                }
                else if (length < 1000)
                {
                    _nameArray[i] = $"{i:000}";
                }
                else
                {
                    _nameArray[i] = $"{i:0000}";
                }
            }
        }

        public int Count()
        {
            switch (Type)
            {
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.String":
                case "System.Boolean":
                default:
                    {
                        return 1;
                    }
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.Boolean[]":
                    {
                        return length;
                    }
            }
        }
        public List<DataModel> ExportValue()
        {
            List<DataModel> dataModels = new List<DataModel>();

            switch (Type)
            {
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.String":
                case "System.Boolean":
                    {
                        dataModels.Add(new DataModel { FullName = FullName, Name = Name, Time = Time, Value = Value });
                        break;
                    }
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.Boolean[]":
                    {
                        for (int i = 0; i < length; i++)
                        {
                            if (_nameArray != null && _valueArray != null)
                                dataModels.Add(new DataModel { FullName = FullName, Name = _nameArray[i], Time = Time, Value = _valueArray[i] });
                        }
                        break;
                    }
            }
            return dataModels;
        }
        public void Update(DataModel data)
        {
            this.Raw = data.Raw;
            this.Time = data.Time;
            this.Type = data.Type;
            UpdateData();
        }
        private void UpdateData()
        {
            switch (Type)
            {
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                    {
                        double.TryParse(Raw, out double v);
                        Value = v;
                        break;
                    }
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                    {
                        string[] s = Raw.Split(',');
                        var l = s.Length;
                        _valueArray = new double[l];
                        for (int i = 0; i < l; i++)
                        {
                            double.TryParse(s[i], out double v);
                            _valueArray[i] = v;
                        }

                        if (length != l)
                        {
                            length = l;
                            CreateNameArray();
                        }
                        break;
                    }
                case "System.Boolean[]":
                    {
                        string[] s = Raw.Split(',');
                        var l = s.Length;
                        _valueArray = new double[l];
                        for (int i = 0; i < l; i++)
                        {
                            bool.TryParse(s[i], out bool v);
                            if (v)
                                _valueArray[i] = 1;
                            else
                                _valueArray[i] = 0;
                        }

                        if (length != l)
                        {
                            length = l;
                            CreateNameArray();
                        }
                        break;
                    }

                case "System.String":
                case "System.Boolean":
                default:
                    {
                        break;
                    }
            }
        }
        public override string ToString()
        {
            // Default, RelValue, Description, ValueOnly, ValueUnit, NameNValue, NameNValueUnit
            switch (_type)
            {
                case "System.Double":
                case "System.Single":
                    {
                        switch (this.TextType)
                        {
                            case TextType.Default:
                            default:
                                {
                                    return $"{Name}:{AbsConvertString(_value, _decimal)}{Unit}";
                                }
                            case TextType.RelValue:
                                {
                                    return $"{Name}:{ConvertString(_value, _decimal)}{Unit}";
                                }
                            case TextType.Description:
                                {
                                    return $"{Name}:{AbsConvertString(_value, _decimal)}{Unit} ({Description})";
                                }
                            case TextType.ValueOnly:
                                {
                                    return $"{AbsConvertString(_value, _decimal)}";
                                }
                            case TextType.ValueUnit:
                                {
                                    return $"{AbsConvertString(_value, _decimal)}{Unit}";
                                }
                            case TextType.NameNValue:
                                {
                                    return $"{Name}{Environment.NewLine}{AbsConvertString(_value, _decimal)}";
                                }
                            case TextType.NameNValueUnit:
                                {
                                    return $"{Name}{Environment.NewLine}{AbsConvertString(_value, _decimal)}{Unit}";
                                }
                        }
                    }
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.Boolean[]":
                    {
                        string[] s = new string[length];
                        if (_valueArray != null)
                        {
                            switch (this.TextType)
                            {
                                case TextType.Default:
                                default:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{_nameArray[i]}:{AbsConvertString(_valueArray[i], _decimal)}{Unit}";
                                        }
                                        break;
                                    }
                                case TextType.RelValue:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{_nameArray[i]}:{ConvertString(_valueArray[i], _decimal)}{Unit}";
                                        }
                                        break;
                                    }
                                case TextType.Description:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{_nameArray[i]}:{AbsConvertString(_valueArray[i], _decimal)}{Unit} ({Description})";
                                        }
                                        break;
                                    }
                                case TextType.ValueOnly:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{AbsConvertString(_valueArray[i], _decimal)}";
                                        }
                                        break;
                                    }
                                case TextType.ValueUnit:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{AbsConvertString(_valueArray[i], _decimal)}{Unit}";
                                        }
                                        break;
                                    }
                                case TextType.NameNValue:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{_nameArray[i]}{Environment.NewLine}{AbsConvertString(_valueArray[i], _decimal)}";
                                        }
                                        break;
                                    }
                                case TextType.NameNValueUnit:
                                    {
                                        for (int i = 0; i < length; i++)
                                        {
                                            s[i] += $"{_nameArray[i]}{Environment.NewLine}{AbsConvertString(_valueArray[i], _decimal)}{Unit}";
                                        }
                                        break;
                                    }
                            }
                        }
                        return string.Join(Environment.NewLine, s);
                    }
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                    {
                        switch (this.TextType)
                        {
                            case TextType.Default:
                            case TextType.RelValue:
                            default:
                                {
                                    return $"{Name}:{Value:0}{Unit}";
                                }
                            case TextType.Description:
                                {
                                    return $"{Name}:{Value:0}{Unit} ({Description})";
                                }
                            case TextType.ValueOnly:
                                {
                                    return $"{Value:0}";
                                }
                            case TextType.ValueUnit:
                                {
                                    return $"{Value:0}{Unit}";
                                }
                            case TextType.NameNValue:
                                {
                                    return $"{Value:0}{Environment.NewLine}{_raw}";
                                }
                            case TextType.NameNValueUnit:
                                {
                                    return $"{Value:0}{Environment.NewLine}{_raw}{Unit}";
                                }
                        }
                    }
                case "System.String":
                case "System.Boolean":
                default:
                    {
                        switch (this.TextType)
                        {
                            case TextType.Default:
                            case TextType.RelValue:
                            default:
                                {
                                    return $"{Name}:{_raw}{Unit}";
                                }
                            case TextType.Description:
                                {
                                    return $"{Name}:{_raw}{Unit} ({Description})";
                                }
                            case TextType.ValueOnly:
                                {
                                    return $"{_raw}";
                                }
                            case TextType.ValueUnit:
                                {
                                    return $"{_raw}{Unit}";
                                }
                            case TextType.NameNValue:
                                {
                                    return $"{Name}{Environment.NewLine}{_raw}";
                                }
                            case TextType.NameNValueUnit:
                                {
                                    return $"{Name}{Environment.NewLine}{_raw}{Unit}";
                                }
                        }
                    }
            }
        }
        public string ConvertString(double d, int n)
        {
            double dAbs = Math.Abs(d);
            int i = 0;
            if (dAbs > 10000)
                i = -5;
            else if (dAbs > 1000)
                i = -4;
            else if (dAbs > 100)
                i = -3;
            else if (dAbs > 10)
                i = -2;
            else if (dAbs > 1)
                i = -1;
            else if (dAbs > 0.1 || dAbs == 0)
                i = 0;
            else if (dAbs > 0.01)
                i = 1;
            else if (dAbs > 0.001)
                i = 2;
            else if (dAbs > 0.0001)
                i = 3;
            else if (dAbs > 0.00001)
                i = 4;
            else
                i = 5;
            return DoubleFormat(d, i + n);

            string DoubleFormat(double f, int m)
            {
                string space = " ";
                if (f < 0)
                    space = "";
                if (m <= 0)
                    return $"{space}{f:F0}";
                else if (m == 1)
                    return $"{space}{f:F1}";
                else if (m == 2)
                    return $"{space}{f:F2}";
                else if (m == 3)
                    return $"{space}{f:F3}";
                else if (m == 4)
                    return $"{space}{f:F4}";
                else if (m == 5)
                    return $"{space}{f:F5}";
                else if (m == 6)
                    return $"{space}{f:F6}";
                else if (m == 7)
                    return $"{space}{f:F7}";
                else if (m == 8)
                    return $"{space}{f:F8}";
                else if (m == 9)
                    return $"{space}{f:F9}";
                else if (m == 10)
                    return $"{space}{f:F10}";
                else if (m == 11)
                    return $"{space}{f:F11}";
                else
                    return $"{space}{f:F12}";
            }
        }
        public string AbsConvertString(double f, int m)
        {
            string space = " ";
            if (f < 0)
                space = "";
            if (m <= 0)
                return $"{space}{f:F0}";
            else if (m == 1)
                return $"{space}{f:F1}";
            else if (m == 2)
                return $"{space}{f:F2}";
            else if (m == 3)
                return $"{space}{f:F3}";
            else if (m == 4)
                return $"{space}{f:F4}";
            else if (m == 5)
                return $"{space}{f:F5}";
            else if (m == 6)
                return $"{space}{f:F6}";
            else if (m == 7)
                return $"{space}{f:F7}";
            else if (m == 8)
                return $"{space}{f:F8}";
            else if (m == 9)
                return $"{space}{f:F9}";
            else if (m == 10)
                return $"{space}{f:F10}";
            else if (m == 11)
                return $"{space}{f:F11}";
            else
                return $"{space}{f:F12}";
        }
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public object Clone() => MemberwiseClone();
    }
    public class RecordData
    {
        public int ID { get; set; } = 0;
        public string Module { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = "System.String";
        public string Unit { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Length { get; set; }

        [JsonIgnore]
        public DateTime Time { get; set; } = DateTime.Now;
        [JsonIgnore]
        public bool IsUpdated { get; set; } = true;
        [JsonIgnore]
        public double UpdateInterval { get; set; } = 10; // seconds
        public RecordData()
        {
        }
        public void Update(RecordValue data, int id = 0)
        {
            if (Value != data.Value)
            {
                ID = id;
                Time = data.Time;
                Type = data.Type;
                Value = data.Value;
                Length = data.Length;
                IsUpdated = true;
            }
            else if (data.Time.Subtract(Time).TotalSeconds > UpdateInterval)
            {
                Time = data.Time;
                IsUpdated = true;
            }
        }
        public void UpdateHeader()
        {
            switch (Type)
            {
                case "System.Double":
                case "System.Single":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.String":
                case "System.Boolean":
                default:
                    {
                        Name = Name;
                        Unit = Unit;
                        break;
                    }
                case "System.Double[]":
                case "System.Single[]":
                case "System.Int16[]":
                case "System.Int32[]":
                case "System.Int64[]":
                case "System.Boolean[]":
                    {
                        string[] n = new string[Length];
                        string[] u = new string[Length];
                        for (int i = 0; i < Length; i++)
                        {
                            if (Length < 10)
                                n[i] = $"{Name}{i + 1:0}";
                            else if (Length < 100)
                                n[i] = $"{Name}{i + 1:00}";
                            else if (Length < 1000)
                                n[i] = $"{Name}{i + 1:000}";
                            else if (Length < 10000)
                                n[i] = $"{Name}{i + 1:0000}";
                            else
                                n[i] = $"{Name}{i + 1:00000}";
                            u[i] = Unit;
                        }
                        Name = string.Join(',', n);
                        Unit = string.Join(',', u);
                        break;
                    }
            }
        }
    }
    public abstract class RecordValue
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = "System.String";
        public int Length { get; set; } = 1;
    }
    public class RecordValue<T> : RecordValue
    {
        public RecordValue(T value)
        {
            if (value == null)
                return;
            else
            {
                Type = value.GetType().FullName;

                if (Type != null)
                {
                    switch (Type)
                    {
                        case "System.Double":
                            {
                                Value = $"{value:E16}";
                                break;
                            }
                        case "System.Single":
                            {
                                Value = $"{value:E7}";
                                break;
                            }
                        case "System.Double[]":
                            {
                                dynamic v = value;
                                Length = v.Length;
                                string[] s = new string[Length];
                                for (int i = 0; i < Length; i++)
                                {
                                    s[i] = $"{v[i]:E16}";
                                }
                                Value = string.Join(',', s);
                                break;
                            }
                        case "System.Single[]":
                            {
                                dynamic v = value;
                                Length = v.Length;
                                string[] s = new string[Length];
                                for (int i = 0; i < Length; i++)
                                {
                                    s[i] = $"{v[i]:E7}";
                                }
                                Value = string.Join(',', s);
                                break;
                            }
                        case "System.Int16[]":
                        case "System.Int32[]":
                        case "System.Int64[]":
                        case "System.Boolean[]":
                            {
                                dynamic v = value;
                                Length = v.Length;
                                string[] s = new string[Length];
                                for (int i = 0; i < Length; i++)
                                {
                                    s[i] = $"{v[i]}";
                                }
                                Value = string.Join(',', s);
                                break;
                            }
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                        case "System.String":
                        case "System.Boolean":
                        default:
                            {
                                Value = $"{value}";
                                break;
                            }
                    }
                }
            }
        }
        public override string ToString() => Value;
    }
    public class MonitorSetupModel
    {
        public int DefaultPort { get; set; }
        public int UpdateInterval { get; set; }
        public int UpdateIntervalXY { get; set; }
    }
}
