# CPU Power Gadget

CPU Power Gadget is a free tool for Windows that can monitor power usage of your Intel or AMD CPU, using [Libre Hardware Monitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

## Description

Provides real-time graph of processor *Power* (Watts), *Frequency* (GHz), *Temperature* (Celsius) and *Utilization*. Right click anywhere for context menu to enable *Always On Top* mode, display the About dialog and Legal information, and modify preferences: *Screen Update Resolution* is how often the graphs are updated, *Sampling Resolution* is the frequency of pulling power information from the processor.

## Supported Hardware

Whatever CPUs Libre Hardware Monitor supports:

- Intel: up to Tiger Lake
- AMD: up to Zen 2

## System Requirements

- Windows Vista SP2 or later (32-bit and 64-bit)
- .NET Framework 4.5.2 or later

## Building

CPU Power Gadget can be built using Visual Studio. Until the Libre Hardware Monitor dependency is available as an up-to-date NuGet package, it must be built seperately, and the resulting `LibreHardwareMonitorLib.0.8.1.nupkg` must be placed into the directory named `feed` in the solution root.

## License

Mozilla Public License Version 2.0

See [Legal information](<CPU Power Gadget/Resources/legal.txt>)