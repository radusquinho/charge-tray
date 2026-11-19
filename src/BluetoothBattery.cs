using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace ChargeTray
{
    internal static class BluetoothBattery
    {
        private static readonly Guid BatteryService = new Guid("0000180f-0000-1000-8000-00805f9b34fb");
        private static readonly Guid BatteryLevel = new Guid("00002a19-0000-1000-8000-00805f9b34fb");

        public static async Task<List<BatteryReading>> ReadAsync(Action<string> log)
        {
            var readings = new List<BatteryReading>();
            var selector = BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected);
            DeviceInformationCollection devices;
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                devices = await DeviceInformation.FindAllAsync(
                    selector, null, DeviceInformationKind.AssociationEndpoint).AsTask(timeout.Token);

            foreach (var info in devices)
            {
                try
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                        readings.AddRange(await ReadDeviceAsync(info, timeout.Token));
                }
                catch (OperationCanceledException) { log("Bluetooth battery read timed out."); }
                catch (UnauthorizedAccessException) { log("Bluetooth battery access was denied."); }
                catch (COMException error) { log("Bluetooth read: " + error.Message); }
            }
            return readings;
        }

        private static async Task<List<BatteryReading>> ReadDeviceAsync(DeviceInformation info, CancellationToken token)
        {
            var list = new List<BatteryReading>();
            using (var device = await BluetoothLEDevice.FromIdAsync(info.Id).AsTask(token))
            {
                if (device == null || device.ConnectionStatus != BluetoothConnectionStatus.Connected)
                    return list;

                var appearance = device.Appearance;
                var kind = BatteryReading.Classify(appearance == null ? (ushort)0 : appearance.RawValue, info.Name);
                var services = await device.GetGattServicesForUuidAsync(BatteryService, BluetoothCacheMode.Cached).AsTask(token);
                if (services.Status != GattCommunicationStatus.Success)
                    return list;

                try
                {
                    foreach (var service in services.Services)
                    {
                        var characteristics = await service.GetCharacteristicsForUuidAsync(BatteryLevel, BluetoothCacheMode.Cached).AsTask(token);
                        if (characteristics.Status != GattCommunicationStatus.Success)
                            continue;

                        foreach (var characteristic in characteristics.Characteristics)
                        {
                            if ((characteristic.CharacteristicProperties & GattCharacteristicProperties.Read) == 0)
                                continue;
                            var result = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(token);
                            if (result.Status != GattCommunicationStatus.Success || result.Value.Length != 1)
                                continue;

                            using (var reader = DataReader.FromBuffer(result.Value))
                            {
                                var percent = reader.ReadByte();
                                if (percent <= 100)
                                    list.Add(new BatteryReading(info.Id + "|" + characteristic.AttributeHandle, info.Name, percent, kind));
                            }
                        }
                    }
                }
                finally
                {
                    foreach (var service in services.Services)
                        service.Dispose();
                }
            }

            if (list.Count > 1)
                for (var index = 0; index < list.Count; index++)
                    list[index] = new BatteryReading(list[index].Id, info.Name + " (" + (index + 1) + ")", list[index].Percent, kind: list[index].Kind);
            return list;
        }
    }
}
