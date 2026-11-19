using System.Text.RegularExpressions;

namespace ChargeTray
{
    internal enum DeviceKind { Other, Mouse, Headphones, Keyboard, Controller }

    internal sealed class BatteryReading
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Percent;
        public readonly DeviceKind Kind;

        public BatteryReading(string id, string name, int percent, DeviceKind kind)
        {
            Id = id;
            Name = name;
            Percent = percent;
            Kind = kind;
        }

        public static DeviceKind Classify(ushort appearance, string name)
        {
            // Bluetooth SIG assigned Appearance values take precedence over the device name.
            switch (appearance)
            {
                case 0x03C1: return DeviceKind.Keyboard;
                case 0x03C2: return DeviceKind.Mouse;
                case 0x03C3:
                case 0x03C4: return DeviceKind.Controller;
                case 0x0940:
                case 0x0941:
                case 0x0942:
                case 0x0943:
                case 0x0944: return DeviceKind.Headphones;
            }

            name = name ?? string.Empty;
            if (Matches(name, @"mouse|rat[oó]n|trackball")) return DeviceKind.Mouse;
            if (Matches(name, @"keyboard|teclado|keychron")) return DeviceKind.Keyboard;
            if (Matches(name, @"headphones?|headsets?|earbuds?|auriculares|airpods")) return DeviceKind.Headphones;
            if (Matches(name, @"gamepad|controllers?|mando|dualsense")) return DeviceKind.Controller;
            return DeviceKind.Other;
        }

        private static bool Matches(string name, string words)
        {
            return Regex.IsMatch(name, @"\b(?:" + words + @")\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
