using System.Windows.Input;
using CpuPowerGadget.Utilities;

namespace CpuPowerGadget.View
{
    public partial class LegalWindow
    {
        public LegalWindow()
        {
            InitializeComponent();

            LegalBox.Text = ResourceProvider.GetEmbeddedResource("legal.txt");

            PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Key.Escape) Close();
            };
        }
    }
}
