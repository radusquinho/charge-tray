using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChargeTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--report")
            {
                using (var writer = File.CreateText(args[1]))
                {
                    foreach (var device in RazerBattery.Read(writer.WriteLine))
                        writer.WriteLine("{0}: {1}%", device.Name, device.Percent);
                    try
                    {
                        foreach (var device in BluetoothBattery.ReadAsync(writer.WriteLine).GetAwaiter().GetResult())
                            writer.WriteLine("{0}: {1}%", device.Name, device.Percent);
                    }
                    catch (Exception error) { writer.WriteLine("Bluetooth scan: " + error); }
                }
                return;
            }

            bool first;
            using (var mutex = new Mutex(true, @"Local\ChargeTray.Windows", out first))
            {
                if (!first)
                    return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var tray = new TrayContext())
                    Application.Run(tray);
            }
        }
    }

    internal sealed class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private readonly System.Windows.Forms.Timer timer;
        private readonly ToolStripMenuItem refresh;
        private readonly bool spanish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";
        private List<BatteryReading> readings = new List<BatteryReading>();
        private string selectedId;
        private Icon icon;
        private int? displayedPercent;
        private DeviceKind displayedKind;
        private bool displayedLightTheme;
        private bool busy;
        private bool disposed;
        private bool failed;

        public TrayContext()
        {
            menu = new ContextMenuStrip();
            menu.MinimumSize = new Size(240, 0);
            menu.Padding = new Padding(3, 5, 3, 5);
            menu.Opening += delegate { BuildMenu(); };
            refresh = new ToolStripMenuItem(L("Actualizar", "Refresh now"));
            refresh.Click += async delegate { await RefreshAsync(); };

            tray = new NotifyIcon();
            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left)
                    menu.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
            };
            UpdateIcon();
            tray.Visible = true;

            // Start after the Forms message loop exists so Bluetooth awaits stay on the UI thread.
            timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += async delegate
            {
                timer.Interval = 60000;
                await RefreshAsync();
            };
            timer.Start();
        }

        private string L(string es, string en) { return spanish ? es : en; }

        private async Task RefreshAsync()
        {
            if (busy || disposed)
                return;
            busy = true;
            refresh.Enabled = false;
            var next = new List<BatteryReading>();
            failed = false;
            try
            {
                next.AddRange(await Task.Run(() => RazerBattery.Read(message => Debug.WriteLine(message)).ToList()));
            }
            catch (Exception error)
            {
                failed = true;
                Debug.WriteLine(error);
            }
            if (disposed)
                return;
            try
            {
                next.AddRange(await BluetoothBattery.ReadAsync(message => Debug.WriteLine(message)));
            }
            catch (Exception error)
            {
                failed = true;
                Debug.WriteLine(error);
            }
            if (disposed)
                return;
            readings = next.OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            busy = false;
            refresh.Enabled = true;
            UpdateIcon();
        }

        private void BuildMenu()
        {
            // Dispose generated entries after removing them from the menu.
            foreach (ToolStripItem item in menu.Items.Cast<ToolStripItem>().ToArray())
            {
                menu.Items.Remove(item);
                if (item != refresh)
                    item.Dispose();
            }
            var title = new ToolStripLabel("Charge Tray");
            title.Padding = new Padding(5, 2, 5, 4);
            menu.Items.Add(title);
            if (readings.Count == 0)
            {
                var message = busy ? L("Leyendo dispositivos…", "Reading devices…") :
                    failed ? L("No se pudo completar la lectura", "Could not complete the scan") :
                    L("Sin lecturas de batería", "No battery readings");
                menu.Items.Add(new ToolStripMenuItem(message) { Enabled = false });
            }
            foreach (var device in readings)
            {
                var item = new ToolStripMenuItem(device.Name + "    " + device.Percent + "%");
                item.Checked = device.Id == selectedId;
                item.ToolTipText = L("Mostrar este porcentaje en la bandeja", "Show this percentage in the tray");
                item.Click += delegate { selectedId = device.Id; UpdateIcon(); };
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(refresh);
            var exit = new ToolStripMenuItem(L("Salir", "Quit"));
            exit.Click += delegate { ExitThread(); };
            menu.Items.Add(exit);
        }

        private void UpdateIcon()
        {
            var selected = readings.FirstOrDefault(device => device.Id == selectedId) ?? readings.FirstOrDefault();
            if (selected != null)
                selectedId = selected.Id;
            int? percent = selected == null ? (int?)null : selected.Percent;
            string label = selected == null ? "Charge Tray" : selected.Name + " · " + percent + "%";
            tray.Text = label.Length <= 63 ? label : label.Substring(0, 57) + "… " + percent + "%";
            var kind = selected == null ? DeviceKind.Other : selected.Kind;
            var lightTheme = TrayArtwork.UsesLightTheme();
            if (icon != null && percent == displayedPercent && kind == displayedKind && lightTheme == displayedLightTheme)
                return;

            var old = icon;
            icon = TrayArtwork.Create(kind, percent, lightTheme);
            displayedPercent = percent;
            displayedKind = kind;
            displayedLightTheme = lightTheme;
            tray.Icon = icon;
            if (old != null)
                old.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                timer.Dispose();
                tray.Visible = false;
                tray.Dispose();
                menu.Dispose();
                refresh.Dispose();
                if (icon != null)
                    icon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
