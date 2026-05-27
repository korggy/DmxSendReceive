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
--random-range <channel=min-max>
                                Per-channel random value range. Repeat for multiple random channels.
--random-update <mode>          interval or keypress. Default: interval
--random-interval-ms <ms>       Random update interval when mode is interval. Default: 1000
--refresh-hz <hz>               DMX frame refresh rate. Default: 30
--log-output <mode>             none, continuous, or random-change. Default: none
```

Channels are absolute DMX channel numbers from 1 to 512. Fixed and random channels must be inside the configured `--start-channel` through `--start-channel + --channel-count - 1` range.

If a channel is both fixed and random, the fixed value wins.

Random channels use the full `0-255` range unless constrained with `--random-range`. Each random range is per-channel and must reference a channel also specified with `--random`.

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --random 1 --random 2 --random-range 1=40-180 --random-range 2=10-20
```

## Output Logging

Output logging is off by default.

Use continuous logging to print every frame sent:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2 --random-range 1=40-180 --random-range 2=10-20 --log-output continuous
```

Use random-change logging to print the first generated random values and then only print when random values change:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --fixed 5=255 --fixed 6=100 --random 1 --random 2 --random-range 1=40-180 --random-range 2=10-20 --log-output random-change
```

Log lines show the frame number and configured channel range:

```text
frame=12 channels 1-9: 1=124 2=8 3=0 4=0 5=255 6=100 7=0 8=0 9=0
```

## Examples

Send 9 channels starting at channel 1, with channel 6 fixed at full intensity and pan/tilt randomized every 250 ms:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 1 --channel-count 9 --fixed 6=255 --random 1 --random 2 --random-update interval --random-interval-ms 250
```

Send the same values, but only choose new random values when a key is pressed:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 1 --channel-count 9 --fixed 6=255 --random 1 --random 2 --random-update keypress
```

Control a fixture addressed at DMX channel 10:

```powershell
dotnet run --project .\DmxSender\DmxSender.csproj -- --port COM8 --start-channel 10 --channel-count 9 --fixed 15=255 --random 10 --random 11
```

## Notes

The app sends a best-effort DMX BREAK using `SerialPort.BreakState`, then writes a DMX start code of `0x00` followed by channel slots. This can work with USB-to-RS485 adapters for testing and simple fixture control, but a dedicated USB-DMX interface gives more deterministic DMX timing.

The Waveshare adapter labels the RS485 pins as `A+`, `B-`, and `GND`; if the fixture does not respond, verify Data+/Data- polarity and the fixture DMX address/channel mode.
