﻿using System.IO;
using System.Text;

namespace Loki.Utils
{
    /// <summary>
    /// Summary description for ConsoleWriter.
    /// </summary>
    /// 

    public enum WinConsoleFlashMode
    {
        NoFlashing,
        FlashOnce,
        FlashUntilResponse,
    }

    public class ConsoleWriter : TextWriter
    {
        #region Variables
        TextWriter writer;
        WinConsoleColor color;
        WinConsoleFlashMode flashing;
        bool beep;
        #endregion

        #region Construction
        public ConsoleWriter(TextWriter writer, WinConsoleColor color, WinConsoleFlashMode mode, bool beep)
        {
            this.color = color;
            this.flashing = mode;
            this.writer = writer;
            this.beep = beep;
        }
        #endregion

        #region Properties
        public WinConsoleColor Color
        {
            get { return color; }
            set { color = value; }
        }

        public WinConsoleFlashMode FlashMode
        {
            get { return flashing; }
            set { flashing = value; }
        }

        public bool BeepOnWrite
        {
            get { return beep; }
            set { beep = value; }
        }
        #endregion

        #region Write Routines

        public override Encoding Encoding
        {
            get { return writer.Encoding; }
        }

        protected void Flash()
        {
            switch (flashing)
            {
                case WinConsoleFlashMode.FlashOnce:
                    WinConsole.Flash(true);
                    break;
                case WinConsoleFlashMode.FlashUntilResponse:
                    WinConsole.Flash(false);
                    break;
            }

            if (beep)
                WinConsole.Beep();
        }

        public override void Write(char ch)
        {
            WinConsoleColor oldColor = WinConsole.Color;
            try
            {
                WinConsole.Color = color;
                writer.Write(ch);
            }
            finally
            {
                WinConsole.Color = oldColor;
            }
            Flash();
        }

        public override void Write(string s)
        {
            WinConsoleColor oldColor = WinConsole.Color;
            try
            {
                WinConsole.Color = color;
                Flash();
                writer.Write(s);
            }
            finally
            {
                WinConsole.Color = oldColor;
            }
            Flash();
        }

        public override void Write(char[] data, int start, int count)
        {
            WinConsoleColor oldColor = WinConsole.Color;
            try
            {
                WinConsole.Color = color;
                writer.Write(data, start, count);
            }
            finally
            {
                WinConsole.Color = oldColor;
            }
            Flash();
        }
        #endregion
    }
}
