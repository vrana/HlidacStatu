﻿using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HlidacStatu.Lib.Render
{
    public class ReportDataSource<T>
    {
        public static System.Func<T, string> NoRender = (s) => { return string.Empty; };

        /// <summary>
        /// Obsahuje metada o tom, jak se má který sloupec vykreslit.
        /// </summary>
        public class Column //: IColumn<T>
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; }
            public string CssClass { get; set; }
            public System.Func<T, string> HtmlRender { get; set; } = (s) => { return s.ToString(); };
            public System.Func<T, string> TextRender { get; set; } = (s) => { return s.ToString(); };
            public System.Func<T, object> ValueRender { get; set; } = (s) => { return s; };
            public System.Func<T, string> OrderValueRender { get; set; } = (s) => { return null; };
        }

        /// <summary>
        /// Obsahuje hodnoty s referencí na sloupec
        /// </summary>
        public class DataValue
        {
            public T Value { get; set; }
            public Column Column { get; set; }
        }

        public List<Column> Columns { get; protected set; } = null;
        public List<DataValue[]> Data { get; protected set; } = new List<DataValue[]>();

        public string Title { get; set; }

        public ReportDataSource<T> Clone(
            IEnumerable<T> newData,
            IEnumerable<string> copyColumnNames, int numRowsToCopy = int.MaxValue, string newTitle = null)
        {
            return Clone(newData, ColumnsNamesToIndexes(copyColumnNames.ToArray()), numRowsToCopy, newTitle);
        }

        public ReportDataSource<T> Clone(
        IEnumerable<T> newData,
        IEnumerable<int> copyColumnIndexess, int numRowsToCopy = int.MaxValue, string newTitle = null)
        {
            ReportDataSource<T> inst = new ReportDataSource<T>(new string[] { });
            inst.Title = newTitle ?? this.Title;
            inst.Columns = new List<ReportDataSource<T>.Column>();
            foreach (var c in copyColumnIndexess)
                inst.Columns.Add(this.Columns[c]);

            foreach (var i in newData.Take(numRowsToCopy))
            {
                inst.AddRow(i);
            }

            return inst;
        }

        protected ReportDataSource() { }
        public ReportDataSource<T> Filter(IEnumerable<string> copyColumnNames, int numRowsToCopy, string newTitle = null)
        {
            return Filter(ColumnsNamesToIndexes(copyColumnNames.ToArray()), numRowsToCopy, newTitle);
        }

        public ReportDataSource<T> Filter(IEnumerable<int> copyColumnIndexes, int numRowsToCopy, string newTitle = null)
        {
            ReportDataSource<T> newrds = new ReportDataSource<T>
            {
                Title = newTitle ?? this.Title,
                Columns = new List<ReportDataSource<T>.Column>()
            };

            foreach (var c in copyColumnIndexes)
                newrds.Columns.Add(this.Columns[c]);

            newrds.Data = this.Data.Take(numRowsToCopy)
                .Select(m =>
                {
                    List<ReportDataSource<T>.DataValue> newData = new List<ReportDataSource<T>.DataValue>();
                    foreach (var c in copyColumnIndexes)
                        newData.Add(m[c]);
                    return newData.ToArray();
                })
                .ToList();
            return newrds;
        }

        public ReportDataSource<T> Filter(int numRowsToCopy)
        {
            return this.Filter(Enumerable.Range(0, this.Columns.Count).ToArray(), numRowsToCopy);
        }

        public ReportDataSource(System.Data.DataTable dt)
        {
            this.Columns = new List<Column>();
            foreach (System.Data.DataColumn col in dt.Columns)
            {
                if (col.ColumnName != "TableName") //old compatibility
                    this.Columns.Add(new Column() { Name = col.ColumnName });
            }

            foreach (DataRow row in dt.Rows)
            {
                List<DataValue> vals = new List<DataValue>();
                foreach (var col in this.Columns)
                {
                    vals.Add(new DataValue() { Column = col, Value = (T)row[col.Name] });
                }

            }
        }

        public ReportDataSource(params string[] columns)
            : this(columns.Select(s => new Column() { Name = s }))
        {
        }
        public ReportDataSource(IEnumerable<Column> columns)
            : this(columns.ToArray())
        {
        }
        public ReportDataSource(params Column[] columns)
        {
            this.Columns = columns.ToList();
        }

        public Type GetGenericType() { return typeof(T); }

        public virtual void Clear()
        {
            Data.Clear();
        }


        /// <summary>
        /// Každý sloupec má referenci na stejná data.
        /// </summary>
        /// <param name="value"></param>
        public virtual void AddRow(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            List<DataValue> vals = new List<DataValue>();
            for (int i = 0; i < this.Columns.Count(); i++)
            {
                vals.Add(new DataValue() { Column = this.Columns[i], Value = value });
            }
            Data.Add(vals.ToArray());
        }

        public IEnumerable<int> ColumnsNamesToIndexes(params string[] columnNames)
        {
            var idxs = new List<int>();
            foreach (var cn in columnNames)
            {
                int idx = this.Columns.FindIndex(m => m.Id == cn);
                if (idx > -1)
                    idxs.Add(idx);
            }
            return idxs;
        }
    }


    public class ReportDataSource : ReportDataSource<object>
    {
        public ReportDataSource(System.Data.DataTable dt) : base(dt)
        { }

        public ReportDataSource(params string[] columns)
            : this(columns.Select(s => new Column() { Name = s }))
        {
        }
        public ReportDataSource(IEnumerable<Column> columns) : base(columns)
        { }

        public ReportDataSource(params Column[] columns) : base(columns)
        {
        }

        public void AddRow(params object[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            else if (values.Count() == 1)
            {
                base.AddRow(values[0]);
                return;
            }
            if (values.Count() != Columns.Count)
                throw new ArgumentOutOfRangeException("Pocet dat nesouhlasi s poctem sloupcu");

            List<DataValue> vals = new List<DataValue>();
            for (int i = 0; i < values.Count(); i++)
            {
                vals.Add(new DataValue() { Column = this.Columns[i], Value = values.Skip(i).First() });
            }
            Data.Add(vals.ToArray());

        }

        public static void FillCountDataFromDateHistogram<TObj>(ISearchResponse<TObj> res, ref ReportDataSource rds,
            string aggregationName)
            where TObj : class
        {
            foreach (Nest.DateHistogramBucket val in (
            (BucketAggregate)(
                (SingleBucketAggregate)res.Aggregations[aggregationName]).First().Value
            ).Items
        )
            {
                rds.AddRow(new DateTime(val.Date.Ticks, DateTimeKind.Utc).ToLocalTime(), val.DocCount);
            }
        }


        public static void FillSubAggsDataFromDateHistogram<TObj>(ISearchResponse<TObj> res, ref ReportDataSource rds,
            string aggregationName, string subAggsName)
            where TObj : class
        {
            foreach (Nest.DateHistogramBucket val in (
            (BucketAggregate)(
                (SingleBucketAggregate)res.Aggregations[aggregationName]).First().Value
            ).Items
        )
            {
                rds.AddRow(
                    new DateTime(val.Date.Ticks, DateTimeKind.Utc).ToLocalTime(),
                    ((Nest.ValueAggregate)val[subAggsName]).Value
                    );

            }

        }

        public static ReportDataSource SimpleReportDataForDateChart(string xAxisName, string yAxisName, Dictionary<DateTime,decimal> data)
        {
            ReportDataSource rds = new ReportDataSource(new ReportDataSource.Column[]
            {
                new  ReportDataSource.Column() {
                    Name =xAxisName,
                    ValueRender = (s) => {
                        DateTime dt = (DateTime)s;
                        if (dt.Kind != DateTimeKind.Utc)
                            dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
                        return Devmasters.DT.Util.ToEpochTimeFromUTC( dt)*1000;
                    }
                },
                new  ReportDataSource.Column() { Name=yAxisName},
            }
            );

            foreach (var item in data)
            {
                rds.AddRow(item.Key, item.Value);
            }

            return rds;
        }
    }

    public class ReportDataTimeValue {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class Series
    {
        public enum SeriesType
        {
            column, line
        }

        private int? _yAxis = null;

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("color")]
        public string Color { get; set; }
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SeriesType Type { get; set; } = SeriesType.column;
        [JsonProperty("yAxis")]
        public int YAxis 
        { 
            get
            {
                if (_yAxis is null)
                {
                    switch (Type)
                    {
                        case SeriesType.column:
                            return 0;
                        case SeriesType.line:
                            return 1;
                        default:
                            return 0;
                    }
                }
                return _yAxis.Value;
            }

            set
            {
                _yAxis = value;
            } 
        }
        [JsonProperty("data")]
        public SeriesData[] Data { get; set; }
        [JsonProperty("tooltip")]
        public SeriesTooltip SeriesTooltip { get; set; }
    }

    public class SeriesTooltip
    {
        [JsonProperty("valuePrefix")]
        public string ValuePrefix { get; set; }
        [JsonProperty("valueSuffix")]
        public string ValueSuffix { get; set; }
        [JsonProperty("valueDecimals")]
        public int ValueDecimals { get; set; }
    }

    public class SeriesData
    {
        public SeriesData() { }
        public SeriesData(int x, decimal y)
        {
            X = x;
            Y = y;
        }
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public decimal Y { get; set; }
    }
}