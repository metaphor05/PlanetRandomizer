using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace PlanetRandomizer
{
    public class DisplayItem
    {

        public string Name { get; set; }
        public string Units { get; set; }
        public string Format { get; set; }
        public object Object { get; set; }
        public MemberInfo Member { get; set; }

        public DisplayItem() { }

        public DisplayItem(object obj, MemberInfo member, DisplayItemAttribute attribute)
        {
            Name = attribute.Name;
            Units = attribute.Units;
            Format = attribute.Format;
            Object = obj;
            Member = member;
        }

        object GetValue()
        {
            if (Member is PropertyInfo) return ((PropertyInfo)Member).GetValue(Object, new object[] { });
            else return null;
        }

        string GetStringValue(object value)
        {
            if (value == null) return "null";

            if (value is string) return (string)value + " " + Units;

            if (value is int) return ((int)value).ToString() + " " + Units;

            if (value is Vector3d) return ((Vector3d)value).ToString();

            if (value is Vector3) return ((Vector3)value).ToString();

            double doubleValue = -999;
            if (value is double) doubleValue = (double)value;
            else if (value is float) doubleValue = (float)value;
            return doubleValue.ToString(Format) + " " + Units;
        }

        public void DrawItem()
        {
            object value = GetValue();

            string stringValue = GetStringValue(value);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Name, GUILayout.ExpandWidth(true));
            GUILayout.Label(stringValue, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

    }
}
