﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using CpuPowerGadget.Model;
using CpuPowerGadget.Properties;
using CpuPowerGadget.Utilities;
using CpuPowerGadget.Visitors;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Hardware.CPU;
using Microsoft.Win32;

namespace CpuPowerGadget.View
{
    public partial class MainWindow
    {
        private readonly Computer _computer;
        private readonly UpdateVisitor _updateVisitor = new UpdateVisitor();

        private readonly Timer _timer = new Timer();
        private DateTime _lastGraphUpdate = DateTime.MinValue;

        private readonly float _baseClock = -1;
        private readonly float _turboClock = 5;
        private readonly Vendor _cpuVendor;

        private double _screenUpdateResolution;
        private double _samplingResolution;

        private bool _taskbarProgress;

        private float _clockMin;
        private float _clockMax;
        private readonly SimpleAverage _clockAverage;
        private readonly SimpleAverage _utilAverage;
        private readonly SimpleAverage _tempAverage;

        private SimpleGraph _pkgPowerGraph;
        private SimpleGraph _powerLimitGraph;
        private SimpleGraph _corePowerGraph;
        private SimpleGraph _dramPowerGraph;

        private SimpleGraph _baseFreqGraph;
        private SimpleGraph _minFreqGraph;
        private SimpleGraph _avgFreqGraph;
        private SimpleGraph _maxFreqGraph;

        private SimpleGraph _pkgTempGraph;

        private SimpleGraph _coreUtilGraph;

        private readonly Stopwatch _timerStopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            _screenUpdateResolution = Settings.Default.ScreenUpdateResolution;
            _samplingResolution = Settings.Default.SamplingResolution;

            _taskbarProgress = Settings.Default.TaskbarProgress;

            _computer = new Computer();
            _computer.Open();
            _computer.IsCpuEnabled = true;
            _computer.Accept(new SensorVisitor(s => s.ValuesTimeWindow = TimeSpan.FromMinutes(5)));

            // Most reliable way to get base frequency
            var registryKeyCpu = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", false);
            var baseClockMhz = registryKeyCpu?.GetValue("~MHz").ToString();
            if (baseClockMhz != null)
            {
                _baseClock = int.Parse(baseClockMhz) / 1000.0f;
            }

            var cpu = (GenericCpu) _computer.Hardware.First();
            var cpuId = cpu.CpuId[0][0];

            _cpuVendor = cpuId.Vendor;

            // Cases not handled by LHM's CPUID
            var nameBuilder = new StringBuilder();
            nameBuilder.Append(cpuId.Name);
            nameBuilder.Replace("4-Core Processor", string.Empty);
            nameBuilder.Replace("6-Core Processor", string.Empty);
            nameBuilder.Replace("8-Core Processor", string.Empty);
            nameBuilder.Replace("12-Core Processor", string.Empty);
            nameBuilder.Replace("16-Core Processor", string.Empty);
            nameBuilder.Replace("24-Core Processor", string.Empty);
            nameBuilder.Replace("32-Core Processor", string.Empty);

            var name = nameBuilder.ToString();
            if (name.Contains("with Radeon"))
            {
                name = name.Remove(name.LastIndexOf("with Radeon", StringComparison.Ordinal));
            }

            Title += $" - {name.Trim()}";

            // 0x16: Processor Frequency Information Leaf - EBX: Maximum Frequency (in MHz)
            // Only available if Hypervisor bit is not set
            if (cpuId.Data.GetLength(0) >= 0x16 && ((cpuId.Data[0x1,2] >> 31) & 1) == 0)
            {
                var freqData = cpuId.Data[0x16,1];
                _turboClock = (float) Math.Ceiling((freqData + 100) / 1000.0);
            }

            _timer.Interval = _samplingResolution;
            _timer.Elapsed += TimerOnElapsed;
            _timer.AutoReset = false;

            _clockMin = float.MaxValue;
            _clockMax = float.MinValue;
            _clockAverage = new SimpleAverage(128);
            _utilAverage = new SimpleAverage(128);
            _tempAverage = new SimpleAverage(128);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Topmost = Settings.Default.AlwaysOnTop;
            AlwaysOnTopMenuItem.IsChecked = Topmost;

            _taskbarProgress = Settings.Default.TaskbarProgress;
            TaskbarProgressMenuItem.IsChecked = _taskbarProgress;

            var sensors = UpdateSensors();
            var powerLimit = sensors.FirstOrDefault(s => s.Identifier.ToString().Contains("power") && s.Name.Contains("PL1"))?.Value;

            var powerMax = 45;
            if (powerLimit.HasValue)
            {
                powerMax = (int) (Math.Ceiling((powerLimit.Value * 1.25f + 5) / 10.0f) * 10);
            }

            _powerLimitGraph = new SimpleGraph
            {
                Canvas = PowerCanvas,
                AxisCanvas = PowerAxis,
                Thin = true,
                Min = 0,
                Max = powerMax
            };
            _pkgPowerGraph = new SimpleGraph
            {
                Primary = _powerLimitGraph,
                Canvas = PowerCanvas
            };
            _corePowerGraph = new SimpleGraph
            {
                Primary = _powerLimitGraph,
                Canvas = PowerCanvas
            };
            _dramPowerGraph = new SimpleGraph
            {
                Primary = _powerLimitGraph,
                Canvas = PowerCanvas
            };
            _powerLimitGraph.Init();
            _pkgPowerGraph.Init();
            _corePowerGraph.Init();
            _dramPowerGraph.Init();

            _avgFreqGraph = new SimpleGraph
            {
                Canvas = FreqCanvas,
                AxisCanvas = FreqAxis,
                Min = 0.0f,
                Max = _turboClock,
                NoScale =  true
            };
            _baseFreqGraph = new SimpleGraph
            {
                Primary = _avgFreqGraph,
                Canvas = FreqCanvas,
                Thin = true,
                DefaultValue = _baseClock
            };
            _minFreqGraph = new SimpleGraph
            {
                Primary = _avgFreqGraph,
                Canvas = FreqCanvas
            };
            _maxFreqGraph = new SimpleGraph
            {
                Primary = _avgFreqGraph,
                Canvas = FreqCanvas,
            };
            _avgFreqGraph.Init();
            _baseFreqGraph.Init();
            _minFreqGraph.Init();
            _maxFreqGraph.Init();

            _pkgTempGraph = new SimpleGraph
            {
                Canvas = TempCanvas,
                AxisCanvas = TempAxis,
                Min = 30,
                Max = 100
            };
            _pkgTempGraph.Init();

            _coreUtilGraph = new SimpleGraph
            {
                Canvas = UtilCanvas,
                AxisCanvas = UtilAxis,
                Min = 0,
                Max = 100
            };
            _coreUtilGraph.Init();

            PkgPowerPath.Data = _pkgPowerGraph.Geometry;
            PowerLimitPath.Data = _powerLimitGraph.Geometry;
            CorePowerPath.Data = _corePowerGraph.Geometry;
            DramPowerPath.Data = _dramPowerGraph.Geometry;
            BaseFreqPath.Data = _baseFreqGraph.Geometry;
            AvgFreqPath.Data = _avgFreqGraph.Geometry;
            MinFreqPath.Data = _minFreqGraph.Geometry;
            MaxFreqPath.Data = _maxFreqGraph.Geometry;
            PkgTempPath.Data = _pkgTempGraph.Geometry;
            CoreUtilPath.Data = _coreUtilGraph.Geometry;

            TimerOnElapsed(this, null);
        }

        private List<ISensor> UpdateSensors()
        {
            _computer.Accept(_updateVisitor);

            var sensors = new List<ISensor>();
            var visitor = new SensorVisitor(s => { if (s.Identifier.ToString().Contains("cpu")) sensors.Add(s); });
            visitor.VisitComputer(_computer);

            return sensors;
        }

        private void ScheduleTimer()
        {
            var elapsed = _timerStopwatch.ElapsedMilliseconds;
            while (elapsed >= _samplingResolution)
            {
                elapsed -= (long)_samplingResolution;
            }

            _timer.Interval = _samplingResolution - elapsed;
            _timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _timerStopwatch.Restart();

            var sensors = UpdateSensors();

            if (sensors.Count == 0)
            {
                ScheduleTimer();
                return;
            }

            var coreFrequencies = _cpuVendor == Vendor.AMD
                ? sensors.Where(s => s.Identifier.ToString().Contains("clock") && s.Name.Contains("Core"))
                : sensors.Where(s => s.Identifier.ToString().Contains("clock") && s.Name.Contains("CPU"));
            var clockAvg = 0.0f;
            foreach (var sensor in coreFrequencies)
            {
                var value = sensor.Value;
                
                if (!value.HasValue) continue;
                
                var freq = value.Value;
                if (freq < _clockMin)
                {
                    _clockMin = freq;
                }

                if (freq > _clockMax)
                {
                    _clockMax = freq;
                }
                clockAvg = _clockAverage.Add(value.Value);
            }
            var coreUtil = sensors.FirstOrDefault(s => s.Identifier.ToString().Contains("load") && s.Name.Contains("Total"))?.Value;
            float? utilAverage = null;
            if (coreUtil != null)
            {
                utilAverage = _utilAverage.Add(coreUtil.Value);
            }

            if (_taskbarProgress)
            {
                SetTaskBarStatus((utilAverage ?? 0) / 100.0f);
            }

            foreach (var sensor in sensors)
            {
                if (sensor.Identifier.ToString().Contains("temperature"))
                {
                    Debug.WriteLine(sensor.Name.ToString() + ": " + sensor.Value);
                }
            }

            var pkgTemp = _cpuVendor == Vendor.AMD 
                ? sensors.FirstOrDefault(s => s.Identifier.ToString().Contains("temperature") && (s.Name.Contains("Core #1 - ") || s.Name.Contains("Tdie")))?.Value
                : sensors.FirstOrDefault(s => s.Identifier.ToString().Contains("temperature") && s.Name.Contains("Package"))?.Value;
            float? tempAverage = null;
            if (pkgTemp != null)
            {
                tempAverage = _tempAverage.Add(pkgTemp.Value);
            }

            if (DateTime.Now - _lastGraphUpdate < TimeSpan.FromMilliseconds(_screenUpdateResolution))
            {
                ScheduleTimer();
                return;
            }

            if (_lastGraphUpdate > DateTime.MinValue && DateTime.Now - _lastGraphUpdate > TimeSpan.FromSeconds(5))
            {
                _lastGraphUpdate = DateTime.MinValue;
                ScheduleTimer();
                return;
            }

            _lastGraphUpdate = DateTime.Now;

            var powers = sensors.Where(s => s.Identifier.ToString().Contains("power")).ToList();

            var pkgPower = powers.FirstOrDefault(s => s.Name.Contains("Package"))?.Value;
            var powerLimit = sensors.FirstOrDefault(s => s.Identifier.ToString().Contains("power") && s.Name.Contains("PL1"))?.Value;
            var corePower = _cpuVendor == Vendor.AMD
                ? powers.Where(s => s.Name.Contains("Core")).Select(s => s.Value ?? 0.0f).Sum() 
                : powers.FirstOrDefault(s => s.Name.Contains("Core"))?.Value;
            var dramPower = powers.FirstOrDefault(s => s.Name.Contains("Memory"))?.Value;

            Dispatcher?.Invoke(() =>
            {
                UpdateUi(clockAvg, pkgPower, corePower, dramPower, tempAverage, utilAverage);

                _powerLimitGraph.Update(powerLimit, _pkgPowerGraph, pkgPower);
                _pkgPowerGraph.Update(pkgPower);
                _corePowerGraph.Update(corePower);
                _dramPowerGraph.Update(dramPower);
                _avgFreqGraph.Update(clockAvg / 1000);
                _minFreqGraph.Update(_clockMin / 1000);
                _maxFreqGraph.Update(_clockMax / 1000);
                _pkgTempGraph.Update(tempAverage);
                _coreUtilGraph.Update(utilAverage);
            });

            _clockMin = float.MaxValue;
            _clockMax = float.MinValue;
            _clockAverage.Reset();
            _utilAverage.Reset();
            _tempAverage.Reset();

            ScheduleTimer();
        }

        public void SetTaskBarStatus(float value)
        {
            if (value < 0.05) 
            {
                // Even at 1% the progress bar is around 8% thick on the taskbar
                // So only show anything if the CPU usage is at or above 5%
                value = 0;
            }
            else if (value > 1)
            {
                value = 1;
            }

            var state = TaskbarItemProgressState.Normal;

            if (value > 0.8)
            {
                state = value < 0.9 ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Error;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                TaskbarItemInfo.ProgressState = state;
                TaskbarItemInfo.ProgressValue = value;
            }));
        }

        private void UpdateUi(float clockAvg, float? pkgPower, float? corePower, float? dramPower, float? pkgTemp, float? coreUtil)
        {
            MinFreq.Text = $"{_clockMin / 1000:F2} GHz";
            MaxFreq.Text = $"{_clockMax / 1000:F2} GHz";
            AvgFreq.Text = $"{clockAvg / 1000:F2} GHz";

            var powerElements = 0;

            if (pkgPower.HasValue)
            {
                PkgPower.Text = $"{pkgPower.Value:F2} W";
                PkgPowerGrid.Visibility = Visibility.Visible;
                powerElements++;
            }
            else
            {
                PkgPowerGrid.Visibility = Visibility.Collapsed;
            }

            if (corePower.HasValue)
            {
                CorePower.Text = $"{corePower.Value:F2} W";
                CorePowerGrid.Visibility = Visibility.Visible;
                powerElements++;
            }
            else
            {
                CorePowerGrid.Visibility = Visibility.Collapsed;
            }

            if (dramPower.HasValue)
            {
                DramPower.Text = $"{dramPower.Value:F2} W";
                DramPowerGrid.Visibility = Visibility.Visible;
                powerElements++;
            }
            else
            {
                DramPowerGrid.Visibility = Visibility.Collapsed;
            }

            PowerPanel.Visibility = powerElements == 0 ? Visibility.Collapsed : Visibility.Visible;

            PkgTemp.Text = pkgTemp.HasValue ? $"{pkgTemp.Value:F2} °C" : "?";

            CoreUtil.Text = coreUtil.HasValue ? $"{coreUtil.Value:F2} %" : "?";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.SetPlacement(Settings.Default.WindowPlacement);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _timer.Stop();
            _computer.Close();
            Settings.Default.WindowPlacement = this.GetPlacement();
            Settings.Default.Save();
        }

        private void OnAlwaysOnTopClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) sender;
            Topmost = menuItem.IsChecked;
            Settings.Default.AlwaysOnTop = menuItem.IsChecked;
            Settings.Default.Save();
        }

        private void OnTaskbarProgressClick(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem) sender;
            _taskbarProgress = menuItem.IsChecked;

            if (!_taskbarProgress)
            {
                SetTaskBarStatus(0);
            }

            Settings.Default.TaskbarProgress = menuItem.IsChecked;
            Settings.Default.Save();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow {Owner = this};
            aboutWindow.ShowDialog();
        }

        private void OnPreferencesClick(object sender, RoutedEventArgs e)
        {
            var preferencesWindow = new PreferencesWindow
            {
                Owner = this,
                ScreenUpdateResolution = _screenUpdateResolution,
                SamplingResolution = _samplingResolution
            };
            
            if (preferencesWindow.ShowDialog() != true) return;

            _screenUpdateResolution = preferencesWindow.ScreenUpdateResolution;
            _samplingResolution = preferencesWindow.SamplingResolution;
            Settings.Default.ScreenUpdateResolution = _screenUpdateResolution;
            Settings.Default.SamplingResolution = _samplingResolution;
            Settings.Default.Save();
        }
    }
}
