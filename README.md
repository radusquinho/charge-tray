# Charge Tray

See the battery level of a wireless mouse or another compatible peripheral next to the Windows clock. Click the icon to see all available readings and choose which device appears in the tray.

## Download

Download **ChargeTray.exe** from the [latest release](https://github.com/radusquinho/charge-tray/releases/latest) and run it on Windows 10 (1809 or newer) or Windows 11, x64, with .NET Framework 4.8. The portable executable requires no installation or administrator access. If Windows puts it in the hidden icons area, open **^** next to the clock and drag Charge Tray beside the clock.

- The number on the icon is the selected device's percentage. Hover for its name and exact percentage.
- Click to see the other readings, switch devices, refresh now, or quit. The app refreshes once a minute.
- The menu follows your Windows language in Spanish or English.
- Device readings stay on your PC. The app makes no internet requests and starts when you open it.

## Compatibility

| Connection | Battery source |
| --- | --- |
| Razer Viper V3 HyperSpeed stock USB receiver (`1532:00B8`) | Validated on the actual receiver in Windows. Reads the vendor HID battery report. |
| Bluetooth Low Energy | Connected devices exposing a readable standard GATT Battery Level characteristic. Physical BLE device readings are still unverified for this release. |

Other USB receivers and Bluetooth Classic accessories may need device-specific support. A peripheral with no readable level is omitted. With no readings, the icon shows an outline of a battery.

## Desarrollo y uso

Descarga **ChargeTray.exe** desde la [última versión](https://github.com/radusquinho/charge-tray/releases/latest). Si el icono aparece bajo **^**, arrástralo al lado del reloj para dejarlo a la vista. Haz clic para ver los porcentajes disponibles o elegir cuál mostrar.

To build the executable, install a Windows 10 or 11 SDK and run the build script in PowerShell:

```powershell
.\build.ps1
```

The result is `dist/ChargeTray.exe`. The build uses the Windows .NET Framework compiler and WinRT metadata from the SDK. There are no NuGet dependencies.

## References

- [Microsoft Bluetooth GATT client API](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/gatt-client)
- [Microsoft HID feature reports](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/sending-hid-reports)
- [OpenMouse receiver protocol observations](https://github.com/OpenMouse-Project/mouse-protocol/blob/main/docs/razer-testing.md#viper-v3-hyperspeed-153200b8--verified-on-the-stock-receiver) and [OpenRazer driver](https://github.com/openrazer/openrazer/blob/master/driver/razermouse_driver.c)

Free and open source under the [MIT license](LICENSE).
