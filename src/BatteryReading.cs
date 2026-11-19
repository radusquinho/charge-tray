namespace ChargeTray
{
    internal sealed class BatteryReading
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Percent;

        public BatteryReading(string id, string name, int percent)
        {
            Id = id;
            Name = name;
            Percent = percent;
        }
    }
}
