﻿using System;
using JetBrains.Annotations;

namespace fCraft.ConfigCLI {
    class TextOption : ICloneable {

        [CanBeNull]
        public object Tag { get; set; }

        [CanBeNull]
        public string Label { get; set; }

        [NotNull]
        public string Text { get; set; }


        public Column Column { get; set; }

        public ConsoleColor ForeColor { get; set; }

        public ConsoleColor BackColor { get; set; }


        public TextOption( string label, [NotNull] string text, Column column ) {
            Label = label;
            Text = text;
            ForeColor = ForeColorDefault;
            BackColor = BackColorDefault;
            Column = column;
        }

        
        public object Clone() {
            return new TextOption( Label, Text, Column ) {
                ForeColor = ForeColor,
                BackColor = BackColor
            };
        }


        static TextOption() {
            ForeColorDefault = ConsoleColor.Gray;
            BackColorDefault = ConsoleColor.Black;
        }

        public static readonly TextOption SpacerLeft = new TextOption( null, "", Column.Left );
        public static readonly TextOption SpacerRight = new TextOption( null, "", Column.Right );
        public static ConsoleColor ForeColorDefault { get; set; }
        public static ConsoleColor BackColorDefault { get; set; }
    }
}
