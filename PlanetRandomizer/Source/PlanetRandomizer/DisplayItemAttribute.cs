using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PlanetRandomizer
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DisplayItemAttribute : Attribute
    {

        public string Units = "";
        public string Name = "";
        public string Format = "";

        public DisplayItemAttribute(string name, string units, string format)
        {
            Units = units;
            Name = name;
            Format = format;
        }

    }
}
