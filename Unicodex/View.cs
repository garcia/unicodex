﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicodex.Model;

namespace Unicodex.View
{
    public class ViewBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Character : ViewBase
    {
        public string Codepoint { get; private set; }
        public string Name { get; private set; }
        public string Value { get; private set; }
        public bool HasSpecialValue { get; private set; }

        public Character(Model.Character c)
        {
            Codepoint = "U+" + c.Codepoint.ToString("X4");
            Name = c.Name;

            if (c.Codepoint <= 32)
            {
                Value = char.ConvertFromUtf32(0x2400 + c.Codepoint);
                HasSpecialValue = true;
            }
            else if (c.Codepoint == 127)
            {
                Value = "\u2421";
                HasSpecialValue = true;
            }
            else
            {
                Value = c.Value;
                HasSpecialValue = false;
            }
        }
    }
}
