﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno
{
    public class Card
    {
        public char color;
        public string value;

        public Card(char Color, string Value)
        {
            color = Color;
            value = Value;
        }

        public int getValuePoint()
        {
            switch (this.value)
            {
                case "0":
                    return 0;
                case "1":
                    return 1;
                case "2":
                    return 2;
                case "3":
                    return 3;
                case "4":
                    return 4;
                case "5":
                    return 5;
                case "6":
                    return 6;
                case "7":
                    return 7;
                case "8":
                    return 8;
                case "9":
                    return 9;
                case "dr2":
                    return 20;
                case "r":
                    return 20;
                case "wild":
                    return 50;
                case "s":
                    return 20;
                case "wdr4":
                    return 50;
                default:
                    return 0;
            }
        }

        public override string ToString()
        {
            string thecard = "";
            if (this.value == "wild" || this.value == "wdr4")
                thecard = this.value;
            else
            {
                thecard = this.color.ToString() + this.value;
            }
            return thecard;
        }
    }
}