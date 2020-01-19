using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace CpuPowerGadget.View
{
    public partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Key.Escape) Close();
            };
        }

        public static string Version
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                var version = fileVersionInfo.ProductVersion;
                return version;
            }
        }

        private void OnLegalClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var legalWindow = new LegalWindow {Owner = this};
            legalWindow.ShowDialog();
        }
    }
}
