using DevExpress.Data.Filtering;
using DevExpress.Diagram.Core;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml.Serialization;

namespace HoRang2Sea.Models
{
    public class XmlDefinitioncs
    {
    }


    [XmlInclude(typeof(TableDefinition)), XmlInclude(typeof(ConnectionDefinition))]
    [XmlRoot("Database")]
    public class DatabaseDefinition
    {

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
        [XmlArray("Tables"), XmlArrayItem(typeof(TableDefinition))]
        public ObservableCollection<TableDefinition> Tables { get; set; }
        [XmlArray("Connections"), XmlArrayItem(typeof(ConnectionDefinition))]
        public ObservableCollection<ConnectionDefinition> Connections { get; set; }
        public TableDefinition this[string tableName]
        {
            get
            {
                if (string.IsNullOrEmpty(tableName))
                    throw new ArgumentException("tableName");
                return Tables.SingleOrDefault(x => string.Equals(x.Name, tableName));
            }
        }




        public DatabaseDefinition()
            : this(Enumerable.Empty<TableDefinition>(), Enumerable.Empty<ConnectionDefinition>())
        {
        }
        public DatabaseDefinition(IEnumerable<TableDefinition> tables, IEnumerable<ConnectionDefinition> connections)
        {
            Tables = new ObservableCollection<TableDefinition>(tables.ToList());
            Connections = new ObservableCollection<ConnectionDefinition>(connections.ToList());
        }
    }


    [XmlInclude(typeof(ColumnDefinition))]
    public class TableDefinition : ViewModelBase
    {

        [XmlArray("Columns")]
        [XmlArrayItem(typeof(ColumnDefinition))]
        public ObservableCollection<ColumnDefinition> Columns { get; set; }
        [XmlAttribute("Name")]
        public string Name
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                {
                    ImageDefinition.TableName = value;
                    ImageDefinition.ImagePath = @"pack://application:,,,/HoRang2Sea;component/Resource/ModelImage/" + value + @".svg";
                }
            }
        }
        public ObservableCollection<OutputDefinition> OutputColumns { get; set; }

        [XmlAttribute("PositionX")]
        public int PositionX { get; set; }
        [XmlAttribute("PositionY")]
        public int PositionY { get; set; }

        public ImageDefinition ImageDefinition { get; set; }

        public ObservableCollection<ColumnDefinition> Columns_Visible { get; set; }

        public ObservableCollection<object> VisibleData { get; set; }
        public ColumnDefinition this[string columnName]
        {
            get
            {
                if (string.IsNullOrEmpty(columnName))
                    throw new ArgumentException("columnName");
                return Columns.SingleOrDefault(x => string.Equals(x.Name, columnName));
            }
        }
        public TableDefinition()
            : this(Enumerable.Empty<ColumnDefinition>())
        {
        }
        public TableDefinition(IEnumerable<ColumnDefinition> columns)
        {
            Columns = new ObservableCollection<ColumnDefinition>(columns.ToList());
            Columns_Visible = new ObservableCollection<ColumnDefinition>(columns.ToList());
            VisibleData = new ObservableCollection<object>();
            string projectpath = System.IO.Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName;
            ImageDefinition = new ImageDefinition();
            OutputColumns = new ObservableCollection<OutputDefinition>();

        }
    }

    public class ColumnDefinition : ViewModelBase
    {
        [XmlAttribute("TableName")]
        public string TableName { get; set; }
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAttribute("Init")]
        public string Init
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertiesChanged(Init);
            }
        }
        [XmlAttribute("IsVisible")]
        public bool IsVisible
        {
            get => GetValue<bool>();
            set
            {
                if (SetValue(value))
                {
                    RaisePropertiesChanged("IsVisible");
                }
            }
        }

        [XmlAttribute("min")]
        public string Min { get; set; }
        [XmlAttribute("max")]
        public string Max { get; set; }
        [XmlAttribute("digit")]
        public string Digit { get; set; }
        [XmlAttribute("unit")]
        public string Unit { get; set; }
        [XmlIgnore]
        public string Id { get { return string.Join(".", TableName, Name); } }
    }

    public class ImageDefinition
    {
        public string TableName { get; set; }
        public string ImagePath { get; set; }
    }

    public class OutputDefinition : ViewModelBase
    {
        public string TableName { get; set; }
        public string Name { get; set; }
        public string Value
        {
            get { return GetValue<string>(); }
            set
            {
                if (SetValue(value))
                    RaisePropertiesChanged(Value);
            }
        }
        public string Unit { get; set; }
        public bool IsVisible { get; set; }
        [XmlIgnore]
        public IDisposable Ob;
        public void Dispose()
        {
            if (Ob is not null)
                Ob.Dispose();
        }
    }


    [XmlInclude(typeof(TableRelation))]
    public class ConnectionDefinition
    {
        [XmlAttribute("From")]
        public string From { get; set; }
        [XmlAttribute("To")]
        public string To { get; set; }
        [XmlAttribute("FromRelation")]
        public TableRelation FromRelation { get; set; }
        [XmlAttribute("ToRelation")]
        public TableRelation ToRelation { get; set; }

        public ConnectionDefinition(ColumnDefinition from, ColumnDefinition to)
        {
            From = from.Id;
            To = to.Id;
        }
        public ConnectionDefinition() { }
    }

    public enum TableRelation
    {
        One,
        Many
    }

    public class DatabaseDefinitionKeySelector : IKeySelector
    {
        object IKeySelector.GetKey(object obj)
        {
            if (obj is TableDefinition)
                return ((TableDefinition)obj).Name;
            else if (obj is ColumnDefinition)
                return ((ColumnDefinition)obj).Id;
            return obj;
        }
    }

    public class TableRelationEvaluationOperator : ICustomFunctionOperator
    {
        string ICustomFunctionOperator.Name { get { return "TableRelation"; } }

        object ICustomFunctionOperator.Evaluate(params object[] operands)
        {
            switch ((TableRelation)operands[0])
            {
                case TableRelation.One:
                    return "1";
                case TableRelation.Many:
                    return "*";
            }
            throw new ArgumentException();
        }

        Type ICustomFunctionOperator.ResultType(params Type[] operands)
        {
            return typeof(string);
        }
    }


    public class PositionXYToPointConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var positions = Array.ConvertAll(values, o => System.Convert.ToDouble(o));
            return new Point(positions[0], positions[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var point = (Point)value;
            return new object[] { (int)point.X, (int)point.Y };
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }



    public class DatabaseDiagramItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TableTemplate { get; set; }
        public DataTemplate ColumnTemplate { get; set; }
        public DataTemplate ContainerTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate OutputTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TableDefinition)
                return TableTemplate;
            else if (item is ColumnDefinition)
                return ColumnTemplate;
            else if (item is ImageDefinition)
                return ImageTemplate;
            else if (item is OutputDefinition)
                return OutputTemplate;

            return base.SelectTemplate(item, container);
        }
    }

    public class ChildrenSelector : MarkupExtension, IChildrenSelector
    {
        public IEnumerable<object> GetChildren(object parent)
        {
            if (parent is TableDefinition item)
            {
                var data = new ObservableCollection<object>();

                data.Add(item.ImageDefinition);
                foreach (var column in item.Columns_Visible)
                {
                    data.Add(column);
                }
                return data;
            }
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

}
