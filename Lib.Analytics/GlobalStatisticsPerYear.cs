﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HlidacStatu.Lib.Analytics
{
    public partial class GlobalStatisticsPerYear<T>
        where T:new() // tohle asi není třeba
    {
        public class PropertyOrderedList : OrderedList
        {
            public PropertyOrderedList() : base() { }
           
            public PropertyOrderedList(string propertyName, int year, IEnumerable<decimal> data) : base(data) {
                this.PropertyName = propertyName;
                this.Year = year;
            }

            public int Year { get; set; }
            public string PropertyName { get; set; }
        }

        public int[] CalculatedYears = null;
        
        // Ordered List by neměl být asi úplně ordered list
        public List<PropertyOrderedList> StatisticData { get; set; } =
            new List<PropertyOrderedList>();

        [Obsolete("Only for JSON deserialization")]
        public GlobalStatisticsPerYear() { }

        public GlobalStatisticsPerYear(int[] calculatedYears, IEnumerable<SubjectStatisticsPerYear<T>> dataForAllIcos)
        {
            this.CalculatedYears = calculatedYears;

            // kdyby nás někoho náhodou napadlo dát do statistik string, tak tohle by to mělo pohlídat
            var numericProperties = typeof(T).GetProperties().Where(p => IsNumericType(p.PropertyType));

            //todo: asi by se dalo zrychlit, kdyby se nejelo po jednotlivých property, ale všechny property najednou
            // dneska na to už ale mentálně nemam :)
            // případně by se dalo paralelizovat do threadů (udělat paralel foreach a jet každý rok v samostatném threadu)
            // musel by se jen zamykat zápis do statistic data (třeba v setteru)
            foreach(var year in CalculatedYears)
            {
                foreach(var property in numericProperties)
                {
                    IEnumerable<decimal> globalData = dataForAllIcos.Select(d => 
                        GetDecimalValueOfNumericProperty(property, d.StatisticsForYear(year)));

                    var val = new PropertyOrderedList(property.Name, year, globalData);
                    StatisticData.Add(val);
                }
            }

        }

        public virtual PropertyOrderedList GetRank(int year, string propertyName)
        {
            return StatisticData.Where(sd => sd.Year == year && sd.PropertyName == propertyName)
                .FirstOrDefault();
        }

        #region helper funcions
        private static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(uint),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private static bool IsNumericType(Type type)
        {
            return NumericTypes.Contains(type) ||
                   NumericTypes.Contains(Nullable.GetUnderlyingType(type));
        }

        private static decimal GetDecimalValueOfNumericProperty(PropertyInfo property, T obj)
        {
            return Convert.ToDecimal(property.GetValue(obj, null));
        }
        #endregion
    }
}
