using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ChargeTray
{
    internal static class TrayArtwork
    {
        private static readonly string[] Numerals =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111",
            "101101111001001", "111100111001111", "111100111101111", "111001010010010",
            "111101111101111", "111101111001111"
        };

        public static bool UsesLightTheme()
        {
            return object.Equals(Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "SystemUsesLightTheme", 0), 1);
        }

        public static Icon Create(DeviceKind kind, int? percent, bool lightTheme)
        {
            // Design on a 16-pixel grid, the normal size of a Windows notification icon.
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var ink = new SolidBrush(lightTheme ? Color.FromArgb(22, 22, 22) : Color.White))
            {
                graphics.ScaleTransform(2, 2);
                if (!percent.HasValue)
                    Draw(graphics, ink, "001000111100100101100101111100", 6, 5, 5);
                else
                {
                    var x = percent < 10 ? 2 : 0;
                    Draw(graphics, ink, DeviceGlyph(kind), 4, x, 5);
                    DrawPercent(graphics, ink, percent.Value, x + (percent == 100 ? 4 : 5));
                }

                var handle = bitmap.GetHicon();
                try
                {
                    using (var borrowed = Icon.FromHandle(handle))
                        return (Icon)borrowed.Clone();
                }
                finally { DestroyIcon(handle); }
            }
        }

        private static string DeviceGlyph(DeviceKind kind)
        {
            switch (kind)
            {
                case DeviceKind.Mouse:       return "0110" + "1011" + "1011" + "1001" + "1001" + "0110";
                case DeviceKind.Headphones:  return "0110" + "1001" + "1001" + "1001" + "1001" + "1001";
                case DeviceKind.Keyboard:    return "0000" + "1111" + "1001" + "1111" + "1001" + "1111";
                case DeviceKind.Controller:  return "0000" + "0110" + "1001" + "1111" + "1001" + "0000";
                default:                    return "0110" + "1111" + "1001" + "1001" + "1001" + "1111";
            }
        }

        private static void DrawPercent(Graphics graphics, Brush ink, int percent, int x)
        {
            if (percent == 100)
            {
                // Narrow digits make room for all three digits and the percent sign.
                Draw(graphics, ink, "11111", 1, x, 6);
                Draw(graphics, ink, Numerals[0], 3, x + 2, 6);
                Draw(graphics, ink, Numerals[0], 3, x + 6, 6);
                Draw(graphics, ink, "1001011010", 2, x + 10, 6);
                return;
            }
            else
            {
                foreach (var digit in percent.ToString(CultureInfo.InvariantCulture))
                {
                    Draw(graphics, ink, Numerals[digit - '0'], 3, x, 6);
                    x += 4;
                }
            }
            Draw(graphics, ink, "101001010100101", 3, x, 6);
        }

        private static void Draw(Graphics graphics, Brush ink, string pixels, int width, int x, int y)
        {
            for (var index = 0; index < pixels.Length; index++)
                if (pixels[index] == '1')
                    graphics.FillRectangle(ink, x + index % width, y + index / width, 1, 1);
        }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);
    }
}
