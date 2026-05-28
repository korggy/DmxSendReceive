# DmxSendReceive

`DmxSendReceive` is a .NET 10 solution for experimenting with DMX512 over USB-to-RS485 serial adapters. It includes a sender for controlling fixtures and a receiver/monitor for inspecting incoming DMX-like serial traffic.

The tools use standard DMX UART settings, `250000 8N2`, and are designed for practical testing with adapters such as the Waveshare USB TO RS485 (C).

## Projects

| Project | Description |
| --- | --- |
| `DmxSender` | Console app that sends DMX512-style frames through a USB-to-RS485 adapter. Supports fixed channel values, random channel values, mouse-tracked channel values, per-channel value ranges, interval and keypress random updates, and optional output logging. |
| `DmxSender.Tests` | MSTest coverage for sender command parsing, DMX packet formatting, random value behavior, and output logging. |
| `DmxReceiver` | Console app that monitors incoming DMX512-style serial data and reports inferred frame/break diagnostics. |
| `DmxReceiver.Tests` | MSTest coverage for receiver parsing, diagnostics, summary output, and monitor command parsing. |

## Requirements

- .NET 10 SDK
- USB-to-RS485 serial adapter
- DMX fixture or another serial/DMX test target

## Build and Test

```powershell
dotnet build .\DmxSendReceive.slnx -c Debug
dotnet test .\DmxSendReceive.slnx -c Debug
```

## Sending DMX

Basic sender command:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2
```

Constrain random values per channel:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --random 1 --random 2 --range 1=40-180 --range 2=10-20
```

Track mouse position to channels:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --mouse 1=x --mouse 2=y --range 1=40-180 --range 2=10-220
```

Mouse X maps the full screen from left `0` to right `255`; use `xi` to invert it. Mouse Y maps bottom `0` to top `255`; use `yi` to invert it.

Log output only when random values or mouse-tracked values change:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2 --range 1=40-180 --range 2=10-20 --log-output change
```

See [DmxSender/README.md](DmxSender/README.md) for full sender usage.

## Receiving DMX

Basic receiver command:

```powershell
dotnet run --project .\DmxReceiver\DmxReceiver.csproj -- --port COM8
```

The receiver watches for UART framing errors and DMX start-code patterns to infer frame boundaries. PC serial adapters usually cannot measure DMX BREAK length directly, so receiver diagnostics should be treated as practical serial-adapter evidence rather than lab-grade DMX timing measurements.

## Wiring

Typical RS485-to-DMX wiring:

| Adapter | DMX |
| --- | --- |
| A+ | Data+ |
| B- | Data- |
| GND | Signal reference/ground, when available |

If a fixture does not respond, verify the fixture DMX address, channel mode, and Data+/Data- polarity.

## Timing Note

`DmxSender` sends a best-effort DMX BREAK using `SerialPort.BreakState`, then writes a `0x00` DMX start code followed by channel slots. This is useful for testing and simple fixture control with USB-to-RS485 hardware, but a dedicated USB-DMX interface provides more deterministic DMX timing.
