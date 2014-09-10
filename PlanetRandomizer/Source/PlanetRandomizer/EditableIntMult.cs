using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PlanetRandomizer
{
    public class EditableIntMult : IEditable
    {

        [Persistent]
        public int val;
        public readonly int multiplier;

        public bool parsed;
        [Persistent]
        public string _text;
        public virtual string text
        {
            get { return _text; }
            set
            {
                _text = value;
                int parsedValue;
                parsed = int.TryParse(_text, out parsedValue);
                if (parsed) val = parsedValue * multiplier;
            }
        }

        public EditableIntMult() : this(0) { }

        public EditableIntMult(int val, int multiplier = 1)
        {
            this.val = val;
            this.multiplier = multiplier;
            _text = (val / multiplier).ToString();
        }

        public static implicit operator int(EditableIntMult x)
        {
            return x.val;
        }
    }

    public class EditableInt : EditableIntMult
    {
        public EditableInt(int val)
            : base(val)
        {
        }

        public static implicit operator EditableInt(int x)
        {
            return new EditableInt(x);
        }
    }

}
