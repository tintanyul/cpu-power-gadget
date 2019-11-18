using System.IO;
using System.Linq;
using System.Reflection;

namespace CpuPowerGadget.Utilities
{
    public static class ResourceProvider
    {
        public static string GetEmbeddedResource(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(s => s.EndsWith(fileName));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return "";
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd().Replace("\r\n", "\n");
                }
            }
        }
    }
}
