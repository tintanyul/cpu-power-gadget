using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace CpuPowerGadget
{
    public class Program
    {
        private static readonly Dictionary<string, Assembly> AssemblyDictionary = new Dictionary<string, Assembly>();

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            App.Main();
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = new AssemblyName(args.Name);
            var path = $"{assemblyName.Name}.dll";

            if (assemblyName.CultureInfo?.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = $@"{assemblyName.CultureInfo}\{path}";
            }

            if (AssemblyDictionary.ContainsKey(path)) 
                return AssemblyDictionary[path];

            using (var stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null) 
                    return null;

                var assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                var assembly = Assembly.Load(assemblyRawBytes);
                AssemblyDictionary[path] = assembly;
                return assembly;
            }
        }
    }
}
