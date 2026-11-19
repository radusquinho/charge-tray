using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ChargeTray
{
    internal static class RazerBattery
    {
        private const int ReportLength = 91;
        private const byte Transaction = 0x1f;
        private const byte BatteryClass = 0x07;
        private const byte BatteryCommand = 0x80;

        public static IEnumerable<BatteryReading> Read(Action<string> log)
        {
            Guid hid;
            HidD_GetHidGuid(out hid);
            uint length;
            if (CM_Get_Device_Interface_List_SizeW(out length, ref hid, null, 0) != 0)
                yield break;
            var buffer = new char[length];
            if (CM_Get_Device_Interface_ListW(ref hid, null, buffer, length, 0) != 0)
                yield break;

            foreach (var path in new string(buffer).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (path.IndexOf("vid_1532&pid_00b8", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                using (var handle = CreateFileW(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
                {
                    if (handle.IsInvalid)
                    {
                        log("Razer HID open: " + Marshal.GetLastWin32Error());
                        continue;
                    }
                    var reportSize = GetReportSize(handle);
                    if (reportSize < ReportLength)
                        continue;
                    var percent = Query(handle, reportSize, log);
                    if (percent.HasValue)
                        yield return new BatteryReading(path, "Razer Viper V3 HyperSpeed", percent.Value, DeviceKind.Mouse);
                }
            }
        }

        private static int? Query(SafeFileHandle handle, int reportSize, Action<string> log)
        {
            var request = new byte[reportSize];
            // At index 0 the Windows HID API expects the report ID.
            request[2] = Transaction;
            request[6] = 2;
            request[7] = BatteryClass;
            request[8] = BatteryCommand;
            request[89] = Checksum(request);

            if (!HidD_SetFeature(handle, request, request.Length))
            {
                log("Razer HID request: " + Marshal.GetLastWin32Error());
                return null;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                Thread.Sleep(20);
                var reply = new byte[reportSize];
                if (!HidD_GetFeature(handle, reply, reply.Length))
                {
                    log("Razer HID response: " + Marshal.GetLastWin32Error());
                    return null;
                }
                if (reply[1] == 2 && reply[2] == Transaction && reply[6] >= 2 &&
                    reply[7] == BatteryClass && reply[8] == BatteryCommand && reply[89] == Checksum(reply))
                    return (reply[10] * 100 + 127) / 255;
                log(string.Format("Razer reply status={0:X2}, transaction={1:X2}, command={2:X2}/{3:X2}",
                    reply[1], reply[2], reply[7], reply[8]));
            }
            return null;
        }

        private static byte Checksum(byte[] report)
        {
            byte result = 0;
            for (var index = 3; index < 89; index++)
                result ^= report[index];
            return result;
        }

        private static int GetReportSize(SafeFileHandle handle)
        {
            IntPtr data;
            if (!HidD_GetPreparsedData(handle, out data))
                return 0;
            try
            {
                // HIDP_CAPS contains 32 USHORT fields, including its reserved fields.
                var caps = new ushort[32];
                return HidP_GetCaps(data, caps) == 0x00110000 ? caps[4] : 0;
            }
            finally
            {
                HidD_FreePreparsedData(data);
            }
        }

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint CM_Get_Device_Interface_List_SizeW(out uint length, ref Guid guid, string device, uint flags);
        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint CM_Get_Device_Interface_ListW(ref Guid guid, string device, [Out] char[] buffer, uint length, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint sharing, IntPtr security, uint mode, uint flags, IntPtr template);
        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid guid);
        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_FreePreparsedData(IntPtr data);
        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr data, [Out] ushort[] caps);
        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_SetFeature(SafeFileHandle handle, byte[] buffer, int length);
        [DllImport("hid.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool HidD_GetFeature(SafeFileHandle handle, [In, Out] byte[] buffer, int length);
    }
}
