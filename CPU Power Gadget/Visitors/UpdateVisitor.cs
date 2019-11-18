using System;
using LibreHardwareMonitor.Hardware;

namespace CpuPowerGadget.Visitors
{
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            try
            {
                computer.Traverse(this);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
