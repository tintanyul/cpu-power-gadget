using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;

namespace CpuPowerGadget.Utilities
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int x;
        public int y;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public int length;
        public int flags;
        public int showCmd;
        public Point minPosition;
        public Point maxPosition;
        public Rect normalPosition;
    }

    public static class PlacementHelper
    {
        private static readonly Encoding Encoding = new UTF8Encoding();
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(WindowPlacement));

        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

        private const int SwShownormal = 1;
        private const int SwShowminimized = 2;

        private static void SetPlacement(IntPtr windowHandle, string placementXml)
        {
            if (string.IsNullOrEmpty(placementXml))
            {
                return;
            }

            var xmlBytes = Encoding.GetBytes(placementXml);

            try
            {
                WindowPlacement placement;
                using (var memoryStream = new MemoryStream(xmlBytes))
                {
                    placement = (WindowPlacement)Serializer.Deserialize(memoryStream);
                }

                placement.length = Marshal.SizeOf(typeof(WindowPlacement));
                placement.flags = 0;
                placement.showCmd = placement.showCmd == SwShowminimized ? SwShownormal : placement.showCmd;
                SetWindowPlacement(windowHandle, ref placement);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
            }
        }

        private static string GetPlacement(IntPtr windowHandle)
        {
            GetWindowPlacement(windowHandle, out var placement);

            using (var memoryStream = new MemoryStream())
            {
                using (var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
                {
                    Serializer.Serialize(xmlTextWriter, placement);
                    var xmlBytes = memoryStream.ToArray();
                    return Encoding.GetString(xmlBytes);
                }
            }
        }

        public static void SetPlacement(this Window window, string placementXml)
        {
            SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
        }

        public static string GetPlacement(this Window window)
        {
            return GetPlacement(new WindowInteropHelper(window).Handle);
        }
    }
}
