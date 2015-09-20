using System;
namespace KH2FM_Toolkit
{
    internal class ConsoleProgress
    {
        private const int width = 80;
        private ConsoleColor _ColorBack;
        private ConsoleColor _ColorFore;
        private int _ConsoleTop;
        private double _Multiplier;
        private long _Total;
        public long Current
        {
            get;
            private set;
        }
        public long Total
        {
            get
            {
                return this._Total;
            }
            set
            {
                if (value != this._Total)
                {
                    this._Total = value;
                    this._Multiplier = 78.0 / (double)value;
                    if (value < this.Current)
                    {
                        this.Current = value;
                    }
                }
            }
        }
        public ConsoleColor Color
        {
            get
            {
                return this._ColorBack;
            }
            set
            {
                if (value != this._ColorBack)
                {
                    this._ColorBack = value;
                    switch (this._ColorBack)
                    {
                        case ConsoleColor.Black:
                        case ConsoleColor.DarkBlue:
                        case ConsoleColor.DarkGreen:
                        case ConsoleColor.DarkCyan:
                        case ConsoleColor.DarkRed:
                        case ConsoleColor.DarkMagenta:
                        case ConsoleColor.DarkYellow:
                        case ConsoleColor.DarkGray:
                        case ConsoleColor.Blue:
                            this._ColorFore = ConsoleColor.White;
                            return;
                    }
                    this._ColorFore = ConsoleColor.Black;
                }
            }
        }
        public string Text
        {
            get;
            set;
        }
        public ConsoleProgress(long total, string text = null, ConsoleColor color = ConsoleColor.Green)
        {
            this.Color = color;
            this.Text = text;
            this.Total = total;
            if (Console.CursorLeft > 0)
            {
                Console.WriteLine();
            }
            this._ConsoleTop = Console.CursorTop;
            Console.WriteLine();
            Console.CursorVisible = false;
        }
        ~ConsoleProgress()
        {
            if (this.Current != this.Total)
            {
                this.Finish();
            }
        }
        public void Increment(long value = 1L)
        {
            this.Update(this.Current + value);
        }
        public void ReDraw()
        {
            Tuple<int, int> tuple = new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);
            Console.SetCursorPosition(0, this._ConsoleTop);
            Console.Write("[");
            Console.BackgroundColor = this._ColorBack;
            Console.ForegroundColor = this._ColorFore;
            int i = 0;
            int num = (int)(this._Multiplier * (double)this.Current);
            while (i < 78)
            {
                if (i == num)
                {
                    Console.ResetColor();
                }
                Console.Write((this.Text != null && i <= this.Text.Length && i != 0) ? this.Text[i - 1] : ' ');
                i++;
            }
            Console.ResetColor();
            Console.Write("]");
            Console.SetCursorPosition(tuple.Item1, tuple.Item2);
        }
        public void Update(long current)
        {
            this.Current = ((current > this.Total) ? this.Total : current);
            this.ReDraw();
        }
        public void Finish()
        {
            Console.CursorVisible = true;
            this.Update(this.Total);
        }
    }
}
