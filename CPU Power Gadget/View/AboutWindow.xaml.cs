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

        private void OnLegalClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var legalWindow = new LegalWindow {Owner = this};
            legalWindow.ShowDialog();
        }
    }
}
