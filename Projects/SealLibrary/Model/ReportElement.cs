﻿//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using DynamicTypeDescriptor;
using Seal.Converter;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;
using Seal.Forms;
using System.Drawing.Design;

namespace Seal.Model
{
    [ClassResource(BaseName = "DynamicTypeDescriptorApp.Properties.Resources", KeyPrefix = "ReportElement_")]
    public class ReportElement : MetaColumn
    {
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                bool showFormat = IsEnum || TypeEd == ColumnType.DateTime || TypeEd == ColumnType.Numeric;

                GetProperty("DisplayNameEl").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(!Source.IsNoSQL);
                GetProperty("CellCss").SetIsBrowsable(true);
                GetProperty("HasHTMLTags").SetIsBrowsable(true);
                GetProperty("SortOrder").SetIsBrowsable(true);
                GetProperty("Format").SetIsBrowsable(showFormat);
                GetProperty("TypeEd").SetIsBrowsable(!Source.IsNoSQL);

                GetProperty("AggregateFunction").SetIsBrowsable(PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate);
                GetProperty("TotalAggregateFunction").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("ShowTotal").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("CellScript").SetIsBrowsable(true);
                GetProperty("CalculationOption").SetIsBrowsable(PivotPosition == PivotPosition.Data && IsNumeric);

                if (showFormat)
                {
                    GetProperty("NumericStandardFormat").SetIsBrowsable(IsNumeric);
                    GetProperty("DateTimeStandardFormat").SetIsBrowsable(IsDateTime);
                }

                GetProperty("SerieDefinition").SetIsBrowsable(PivotPosition != PivotPosition.Page);
                GetProperty("SerieType").SetIsBrowsable(PivotPosition == PivotPosition.Data && _serieDefinition == SerieDefinition.Serie);
                GetProperty("Nvd3Serie").SetIsBrowsable(PivotPosition == PivotPosition.Data && _serieDefinition == SerieDefinition.NVD3Serie);
                GetProperty("XAxisType").SetIsBrowsable(PivotPosition == PivotPosition.Row || PivotPosition == PivotPosition.Column || (PivotPosition == PivotPosition.Data && SerieDefinition == SerieDefinition.Serie));
                GetProperty("YAxisType").SetIsBrowsable(PivotPosition == PivotPosition.Data);
                GetProperty("SerieSortOrder").SetIsBrowsable(PivotPosition == PivotPosition.Data && (SerieDefinition == SerieDefinition.Serie || SerieDefinition == SerieDefinition.NVD3Serie));
                GetProperty("SerieSortType").SetIsBrowsable(PivotPosition == PivotPosition.Data && (SerieDefinition == SerieDefinition.Serie || SerieDefinition == SerieDefinition.NVD3Serie));
                GetProperty("AxisUseValues").SetIsBrowsable(PivotPosition != PivotPosition.Data && (IsNumeric || IsDateTime));

                //Read only
                GetProperty("Format").SetIsReadOnly((IsNumeric && NumericStandardFormat != NumericStandardFormat.Custom) || (IsDateTime && DateTimeStandardFormat != DateTimeStandardFormat.Custom));
                GetProperty("TotalAggregateFunction").SetIsReadOnly(ShowTotal == ShowTotal.No);
                GetProperty("SerieType").SetIsReadOnly(SerieDefinition != SerieDefinition.Serie);
                GetProperty("XAxisType").SetIsReadOnly(SerieDefinition == SerieDefinition.None || SerieDefinition == SerieDefinition.NVD3Serie || _serieDefinition == SerieDefinition.SplitterBoth);
                GetProperty("YAxisType").SetIsReadOnly(SerieDefinition != SerieDefinition.Serie && SerieDefinition != SerieDefinition.NVD3Serie);
                GetProperty("SerieSortOrder").SetIsReadOnly((SerieDefinition != SerieDefinition.Serie && SerieDefinition != SerieDefinition.NVD3Serie) || _serieSortType == SerieSortType.None);
                GetProperty("SerieSortType").SetIsReadOnly(SerieDefinition != SerieDefinition.Serie && SerieDefinition != SerieDefinition.NVD3Serie);
                GetProperty("AxisUseValues").SetIsReadOnly(SerieDefinition != SerieDefinition.Axis);

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion


        public static ReportElement Create()
        {
            return new ReportElement() { GUID = Guid.NewGuid().ToString(), _type = ColumnType.Default, _numericStandardFormat = NumericStandardFormat.Default, _datetimeStandardFormat = DateTimeStandardFormat.Default, SortOrder = SortOrderConverter.kAutomaticAscSortKeyword };
        }

        public bool IsEnum
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (MetaColumn.Enum != null);
            }
        }


        public bool IsNumeric
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return true;
                return (TypeEl == ColumnType.Numeric);
            }
        }

        public bool IsText
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.Text);
            }
        }

        public bool IsDateTime
        {
            get
            {
                if (PivotPosition == PivotPosition.Data && AggregateFunction == AggregateFunction.Count) return false;
                return (TypeEl == ColumnType.DateTime);
            }
        }

        public void SetDefaults()
        {
            //Default aggregate
            if (IsEnum)
            {
                AggregateFunction = AggregateFunction.Count;
            }
            else if (IsNumeric)
            {
                AggregateFunction = AggregateFunction.Sum;
            }
            else if (IsDateTime)
            {
                AggregateFunction = AggregateFunction.Max;
            }
            else
            {
                AggregateFunction = AggregateFunction.Count;
            }
        }

        [Browsable(false)]
        private PivotPosition _pivotPosition = PivotPosition.Row;
        public PivotPosition PivotPosition
        {
            get { return _pivotPosition; }
            set { _pivotPosition = value; }
        }


        [Category("Definition"), DisplayName("Name"), Description("Name of the element when displayed in result tables or restrictions."), Id(1, 1)]
        [XmlIgnore]
        [TypeConverter(typeof(CustomNameConverter))]
        public string DisplayNameEl
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName)) return DisplayName;
                return RawDisplayName;
            }
            set
            {
                DisplayName = value;
                if (MetaColumn != null && RawDisplayName == DisplayName) DisplayName = "";
            }
        }

        [XmlIgnore]
        public string DisplayNameElTranslated
        {
            get
            {
                return Model.Report.Repository.TranslateElement(this, DisplayNameEl);
            }
        }

        string _sortOrder;
        [Category("Definition"), DisplayName("Sort Order"), Description("Sort the result tables. Page elements are sorted first, then Row, Column and Data elements."), Id(2, 1)]
        [TypeConverter(typeof(SortOrderConverter))]
        public string SortOrder
        {
            get { return _sortOrder; }
            set { _sortOrder = value; }
        }

        [Category("Options"), DisplayName("Data Type"), Description("Data type of the column."), Id(1, 3)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ColumnType TypeEd
        {
            get { return _type; }
            set
            {
                Type = value;
                UpdateEditorAttributes();
            }
        }

        public ColumnType TypeEl
        {
            get
            {
                if (_type == ColumnType.Default) return MetaColumn.Type;
                return _type;
            }
        }

        public string FormatEl
        {
            get
            {
                SetStandardFormat();
                string result = "";
                if (IsNumeric && NumericStandardFormat == NumericStandardFormat.Default) result = MetaColumn.Format;
                else if (IsNumeric && !string.IsNullOrEmpty(Format)) result = Format;
                else if (IsDateTime && DateTimeStandardFormat == Seal.Model.DateTimeStandardFormat.Default) result = MetaColumn.Format;
                else if (IsDateTime && !string.IsNullOrEmpty(Format)) result = Format;
                else if (IsText && !string.IsNullOrEmpty(Format)) result = Format;
                else result = MetaColumn.Format;

                if (string.IsNullOrEmpty(result)) result = "0";
                return result;
            }
        }

        public bool HasHTMLTagsEl
        {
            get { return HasHTMLTags != null ? HasHTMLTags.Value : (MetaColumn.HasHTMLTags != null ? MetaColumn.HasHTMLTags.Value : false); }
        }


        public string ElementDisplayValue(object value)
        {
            if (value == null) return "";
            if (value is IFormattable)
            {
                string result = value.ToString();
                try
                {
                    result = ((IFormattable)value).ToString(FormatEl, Model.Report.ExecutionView.CultureInfo);
                }
                catch { }
                return result;
            }
            return value.ToString();
        }

        public bool IsSorted
        {
            get { return SortOrder != SortOrderConverter.kNoSortKeyword && !string.IsNullOrEmpty(SortOrder); }
        }

        public string GetEnumSortValue(string enumValue, bool useDisplayValue)
        {
            string result = enumValue;
            bool elementSortPosition = (IsSorted && MetaColumn.Enum.UsePosition);
            MetaEV value = null;
            if (useDisplayValue) value = MetaColumn.Enum.Values.FirstOrDefault(i => i.Val == enumValue);
            else value = MetaColumn.Enum.Values.FirstOrDefault(i => i.Id == enumValue);
            if (value != null)
            {
                string sortPrefix = elementSortPosition ? string.Format("{0:000000}", MetaColumn.Enum.Values.LastIndexOf(value)) : "";
                result = sortPrefix + value.Val;
            }
            return result;
        }

        string _finalSortOrder;
        [XmlIgnore]
        public string FinalSortOrder
        {
            get { return _finalSortOrder; }
            set { _finalSortOrder = value; }
        }

        AggregateFunction _aggregateFunction = AggregateFunction.Sum;
        [Category("Data Options"), DisplayName("Aggregate"), Description("Aggregate function applied to the Data element."), Id(1, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public AggregateFunction AggregateFunction
        {
            get { return _aggregateFunction; }
            set { _aggregateFunction = value; }
        }

        CalculationOption _calculationOption = CalculationOption.No;
        [Category("Data Options"), DisplayName("Calculation Option"), Description("For numeric Data elements, define calculation option applied on the element in the table."), Id(2, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public CalculationOption CalculationOption
        {
            get { return _calculationOption; }
            set
            {
                _calculationOption = value;
                if (Source != null)
                {
                    if (_calculationOption != Seal.Model.CalculationOption.No && string.IsNullOrEmpty(Format)) NumericStandardFormat = NumericStandardFormat.Percentage0;
                    else if (_calculationOption == Seal.Model.CalculationOption.No && NumericStandardFormat != NumericStandardFormat.Custom)
                    {
                        Format = "";
                        NumericStandardFormat = NumericStandardFormat.Custom;
                    }
                }
            }
        }

        ShowTotal _showTotal = ShowTotal.No;
        [Category("Data Options"), DisplayName("Show Total"), Description("For Data elements, add a row or a column showing the total of the element in the table."), Id(3, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public ShowTotal ShowTotal
        {
            get { return _showTotal; }
            set
            {
                _showTotal = value;
                UpdateEditorAttributes();
            }
        }

        AggregateFunction _totalAggregateFunction = AggregateFunction.Sum;
        [Category("Data Options"), DisplayName("Total Aggregate"), Description("Aggregate function applied for the totals."), Id(4, 4)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public AggregateFunction TotalAggregateFunction
        {
            get { return _totalAggregateFunction; }
            set { _totalAggregateFunction = value; }
        }

        //Charts
        SerieDefinition _serieDefinition = SerieDefinition.None;
        [Category("Chart"), DisplayName("Serie Definition"), Description("Define how the element is used in the chart depending on its position. Row or Column elements can be either Axis or Splitter. Data elements can define a Serie."), Id(1, 2)]
        [TypeConverter(typeof(SerieDefinitionConverter))]
        public SerieDefinition SerieDefinition
        {
            get { return _serieDefinition; }
            set
            {
                _serieDefinition = value;
                UpdateEditorAttributes();
            }
        }

        public bool IsSerie
        {
            get { return _serieDefinition == SerieDefinition.Serie || _serieDefinition == SerieDefinition.NVD3Serie; }
        }

        bool _axisUseValues = true;
        [Category("Chart"), DisplayName("Use values for axis"), Description("For Numeric or Date Time axis, if true the values are used to scale the axis, otherwise the values are the labels."), Id(2, 2)]
        public bool AxisUseValues
        {
            get { return _axisUseValues; }
            set { _axisUseValues = value; }
        }


        SeriesChartType _serieType = SeriesChartType.Point;
        [Category("Chart"), DisplayName("Serie Type"), Description("Define the type of serie for the element in the chart."), Id(2, 2)]
        [Editor("System.Windows.Forms.Design.DataVisualization.Charting.ChartTypeEditor", "System.Drawing.Design.UITypeEditor")]
        public SeriesChartType SerieType
        {
            get { return _serieType; }
            set { _serieType = value; }
        }

        NVD3SerieDefinition _nvd3Serie = NVD3SerieDefinition.ScatterChart;
        [Category("Chart"), DisplayName("Serie Type"), Description("Define the type of serie for the element in the chart."), Id(2, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public NVD3SerieDefinition Nvd3Serie
        {
            get { return _nvd3Serie; }
            set { _nvd3Serie = value; }
        }

        private AxisType _xAxisType = AxisType.Primary;
        [Category("Chart"), DisplayName("X Axis Type"), Description("Define the X axis of the serie (Primary or Secondary). This option is only valid for Microsoft charts."), Id(5, 2)]
        public AxisType XAxisType
        {
            get { return _xAxisType; }
            set { _xAxisType = value; }
        }

        private AxisType _yAxisType = AxisType.Primary;
        [Category("Chart"), DisplayName("Y Axis Type"), Description("Define the Y axis of the serie (Primary or Secondary). For NVD3 charts, this option is only valid if the chart contains Series of type Bar, Stacked Area and Line."), Id(6, 2)]
        public AxisType YAxisType
        {
            get { return _yAxisType; }
            set { _yAxisType = value; }
        }

        private PointSortOrder _serieSortOrder = PointSortOrder.Ascending;
        [Category("Chart"), DisplayName("Sort Order"), Description("Define if the serie is sorted ascending or descending in the chart."), Id(4, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public PointSortOrder SerieSortOrder
        {
            get { return _serieSortOrder; }
            set { _serieSortOrder = value; }
        }

        private SerieSortType _serieSortType = SerieSortType.Y;
        [Category("Chart"), DisplayName("Sort Type"), Description("Define how the serie is sorted in the chart."), Id(3, 2)]
        [TypeConverter(typeof(NamedEnumConverter))]
        public SerieSortType SerieSortType
        {
            get { return _serieSortType; }
            set
            {
                _serieSortType = value;
                UpdateEditorAttributes();
            }
        }


        string _metaColumnGUID;
        [Browsable(false)]
        public string MetaColumnGUID
        {
            get { return _metaColumnGUID; }
            set { _metaColumnGUID = value; }
        }

        public void ChangeColumnGUID(string guid)
        {
            _metaColumn = null;
            _metaColumnGUID = guid;
        }

        MetaColumn _metaColumn = null;
        [XmlIgnore, Browsable(false)]
        public MetaColumn MetaColumn
        {
            get
            {
                if (_metaColumn == null)
                {
                    if (!string.IsNullOrEmpty(_metaColumnGUID)) _metaColumn = Source.MetaData.GetColumnFromGUID(MetaColumnGUID);
                }
                return _metaColumn;
            }
        }

        public void SetSourceReference(MetaSource source)
        {
            _metaColumn = null;
            _source = source;
        }

        public string RawDisplayName
        {
            get
            {
                if (MetaColumn != null && PivotPosition == PivotPosition.Data && AggregateFunction != AggregateFunction.Sum && !MetaColumn.IsAggregate) return string.Format("{0} {1}", Model.Report.Translate(Helper.GetEnumDescription(typeof(AggregateFunction), AggregateFunction) + " of"), MetaColumn.DisplayName);
                else if (MetaColumn != null) return MetaColumn.DisplayName;
                return "";
            }
        }


        protected string _SQL;
        [Category("Advanced"), DisplayName("Custom SQL"), Description("If not empty, overwrite the default SQL used for the element in the SELECT statement."), Id(1, 5)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string SQL
        {
            get
            {
                return _SQL;
            }
            set { _SQL = value; }
        }

        private string _cellCss;
        [Category("Advanced"), DisplayName("Custom Cell CSS"), Description("If not empty, overwrite the default CSS style of the cell used to display the element. Up to 3 CSS strings can be specified separated by '|'. The first CSS is applied by default, the second CSS is for empty or zero values, the third CSS string is for the negative values."), Id(2, 5)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CellCss
        {
            get { return _cellCss; }
            set { _cellCss = value; }
        }


        string _cellScript;
        [Category("Advanced"), DisplayName("Cell Script"), Description("If not empty, the script is executed to calculate custom cell value and CSS."), Id(3, 5)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public string CellScript
        {
            get { return _cellScript; }
            set { _cellScript = value; }
        }

        bool _drillEnabled = true;
        [Category("Advanced"), DisplayName("Drill Enabled"), Description("If true, drill navigation is enabled for the column."), Id(3, 6)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public bool DrillEnabled
        {
            get { return _drillEnabled; }
            set { _drillEnabled = value; }
        }

        bool _subReportsEnabled = true;
        [Category("Advanced"), DisplayName("Sub-Reports Enabled"), Description("If true, Sub-Report navigation is enabled for the column."), Id(3, 8)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
        public bool SubReportsEnabled
        {
            get { return _subReportsEnabled; }
            set { _subReportsEnabled = value; }
        }



        [XmlIgnore, Browsable(false)]
        public string RawSQLColumn
        {
            get
            {
                string result = MetaColumn.Name;
                if (PivotPosition == PivotPosition.Data && !MetaColumn.IsAggregate)
                {
                    //aggregate
                    result = string.Format("{0}({1})", AggregateFunction, result);
                }
                return result;
            }
        }

        [XmlIgnore, Browsable(false)]
        public string SQLColumn
        {
            get
            {
                return !string.IsNullOrEmpty(_SQL) ? _SQL : RawSQLColumn;
            }
        }

        private string _SQLColumnName;
        [XmlIgnore, Browsable(false)]
        public string SQLColumnName
        {
            get { return _SQLColumnName; }
            set { _SQLColumnName = value; }
        }

        protected ReportModel _model;
        [XmlIgnore, Browsable(false)]
        public ReportModel Model
        {
            get { return _model; }
            set { _model = value; }
        }

        [XmlIgnore, Browsable(false)]
        public bool IsForNavigation = false;

        public string GetNVD3Format(CultureInfo culture)
        {
            //try to convert from .net to d3 format... from https://github.com/mbostock/d3/wiki/Formatting
            if (IsNumeric)
            {
                string format = FormatEl;
                if (format == "0") return ".0f";
                else if (format == "N0" || format == "D0") return ",.0f";
                else if (format == "N1" || format == "D1") return ",.1f";
                else if (format == "N2" || format == "D2") return ",.2f";
                else if (format == "N3" || format == "D3") return ",.3f";
                else if (format == "N4" || format == "D4") return ",.4f";
                else if (format == "P0") return ",.0%";
                else if (format == "P1") return ",.1%";
                else if (format == "P2") return ",.2%";
                else if (format == "P3") return ",.3%";
                else if (format == "P4") return ",.4%";
                else if (format == "C0") return "$,.0f";
                else if (format == "C1") return "$,.1f";
                else if (format == "C2") return "$,.2f";
                else if (format == "C3") return "$,.3f";
                else if (format == "C4") return "$,.4f";
            }
            else if (IsDateTime)
            {
                string format = FormatEl;
                if (format == "d") format = culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") format = culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") format = culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") format = culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

                StringBuilder result = new StringBuilder();
                for (int i = 0; i < format.Length; i++)
                {
                    if (Helper.FindReplacePattern(format, ref i, "dddd", "%A", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "ddd", "%a", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "dd", "%d", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "d", "%e", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "MMMM", "%B", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MMM", "%b", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "MM", "%m", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "M", "%m", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "yyyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yyy", "%Y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "yy", "%y", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "y", "%y", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "HH", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "H", "%H", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "hh", "%I", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "h", "%I", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "mm", "%M", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "m", "%M", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "ss", "%S", result)) continue;
                    if (Helper.FindReplacePattern(format, ref i, "s", "%S", result)) continue;

                    if (Helper.FindReplacePattern(format, ref i, "FFF", "%L", result)) continue;
                    result.Append(format[i]);
                }
                return result.ToString().Replace("/", culture.DateTimeFormat.DateSeparator);
            }
            return "g";
        }

        public string GetExcelFormat(CultureInfo culture)
        {
            //try to convert from .net to Excel format... 
            if (IsNumeric)
            {
                string format = FormatEl;
                if (format == "0") return "0";
                else if (format == "N0" || format == "D0") return "#,##0";
                else if (format == "N1" || format == "D1") return "#,##0.0";
                else if (format == "N2" || format == "D2") return "#,##0.00";
                else if (format == "N3" || format == "D3") return "#,##0.000";
                else if (format == "N4" || format == "D4") return "#,##0.0000";
                else if (format == "P0") return "0%";
                else if (format == "P1") return "0.0%";
                else if (format == "P2") return "0.00%";
                else if (format == "P3") return "0.00%";
                else if (format == "P4") return "0.0000%";
                else if (format == "C0") return "$ #,##0";
                else if (format == "C1") return "$ #,##0.0";
                else if (format == "C2") return "$ #,##0.00";
                else if (format == "C3") return "$ #,##0.000";
                else if (format == "C4") return "$ #,##0.0000";
            }
            else if (IsDateTime)
            {
                string format = FormatEl;
                if (format == "d") format = culture.DateTimeFormat.ShortDatePattern;
                else if (format == "D") format = culture.DateTimeFormat.LongDatePattern;
                else if (format == "t") format = culture.DateTimeFormat.ShortTimePattern;
                else if (format == "T") format = culture.DateTimeFormat.LongTimePattern;
                else if (format == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                else if (format == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
                else if (format == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
                return format;

            }
            return FormatEl;
        }
    }
}
