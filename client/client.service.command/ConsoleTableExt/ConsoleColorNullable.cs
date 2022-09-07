using System;

namespace ConsoleTableExt
{
    public class ConsoleColorNullable
    {
        public ConsoleColorNullable()
        {

        }

        public ConsoleColorNullable(ConsoleColor foregroundColor)
        {
            ForegroundColor = foregroundColor;
            IsForegroundColorNull = false;
        }

        public ConsoleColorNullable(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
            IsForegroundColorNull = false;
            IsBackgroundColorNull = false;
        }

        public bool IsForegroundColorNull = true;
        public bool IsBackgroundColorNull = true;
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
    }
}
