# DmxSender

`DmxSender` sends DMX512-style serial frames through a USB-to-RS485 adapter.

The app is intended for adapters like the Waveshare USB TO RS485 (C), connected to a DMX fixture such as the U`King B242/B242X moving head.

## Wiring

Connect the RS485 adapter to the DMX input:

| Adapter | DMX |
| --- | --- |
| A+ | Data+ |
| B- | Data- |
| GND | Signal reference/ground, when available |

The sender uses serial settings `250000 8N2`, which are the normal DMX512 UART settings.

## Usage

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- [options]
```

Required:

```text
--port, -p <port>    Serial COM port for the USB-to-RS485 adapter.
```

Common options:

```text
--start-channel <channel>       First DMX channel controlled by this command. Default: 1
--channel-count <count>         Number of consecutive channels to send. Default: 9
--fixed, -f <channel=value>     Fixed channel value. Repeat for multiple fixed values.
--random, -r <channel>          Channel that receives random values. Repeat for multiple channels.
--mouse, -m <channel=x|xi|y|yi>
                                Channel that tracks mouse X or Y position. Repeat for multiple channels.
--range <channel=min-max>       Per-channel value range for random or mouse-tracked channels.
--random-interval-ms <ms>       Random update interval. Default: 1000
--refresh-hz <hz>               DMX frame refresh rate. Default: 30
--log-output <mode>             none, continuous, or change. Default: none
```

Channels are absolute DMX channel numbers from 1 to 512. Fixed, random, and mouse-tracked channels must be inside the configured `--start-channel` through `--start-channel + --channel-count - 1` range.

If a channel is configured more than once, fixed values win over mouse-tracked values, and mouse-tracked values win over random values.

Random and mouse-tracked channels use the full `0-255` range unless constrained with `--range`. Each range is per-channel and must reference a channel also specified with `--random` or `--mouse`. Random values update at the configured `--random-interval-ms` and whenever any key is pressed.

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --random 1 --random 2 --range 1=40-180 --range 2=10-20
```

Mouse-tracked channels map the full virtual screen to `0-255`. Use `x` for left `0` to right `255`, or `xi` to invert X. Use `y` for bottom `0` to top `255`, or `yi` to invert Y.

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --mouse 1=x --mouse 2=y --range 1=40-180 --range 2=10-220
```

## Output Logging

Output logging is off by default.

Use continuous logging to print every frame sent:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2 --range 1=40-180 --range 2=10-20 --log-output continuous
```

Use change logging to print the first tracked output values and then only print when random values or mouse-tracked values change:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2 --range 1=40-180 --range 2=10-20 --log-output change
```

Log lines show the frame number and configured channel range:

```text
frame=12 channels 1-9: 1=124 2=8 3=0 4=0 5=255 6=100 7=0 8=0 9=0
```

## Examples

Send 9 channels starting at channel 1, with channel 6 fixed at full intensity and pan/tilt randomized every 250 ms or when any key is pressed:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 1 --channel-count 9 --fixed 6=255 --random 1 --random 2 --random-interval-ms 250
```

Control a fixture addressed at DMX channel 10:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 10 --channel-count 9 --fixed 15=255 --random 10 --random 11
```

Track mouse position to pan and tilt channels:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 1 --channel-count 9 --fixed 6=255 -m 1=x -m 2=y
```

## Notes

The app sends a best-effort DMX BREAK using `SerialPort.BreakState`, then writes a DMX start code of `0x00` followed by channel slots. This can work with USB-to-RS485 adapters for testing and simple fixture control, but a dedicated USB-DMX interface gives more deterministic DMX timing.

The Waveshare adapter labels the RS485 pins as `A+`, `B-`, and `GND`; if the fixture does not respond, verify Data+/Data- polarity and the fixture DMX address/channel mode.
