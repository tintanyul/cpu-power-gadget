using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CpuPowerGadget.View
{
    public partial class PreferencesWindow : INotifyPropertyChanged
    {
        private double _screenUpdateResolution;
        private double _samplingResolution;

        public double ScreenUpdateResolution
        {
            get => _screenUpdateResolution;
            set
            {
                _screenUpdateResolution = value;
                OnPropertyChanged(nameof(ScreenUpdateResolution));
                if (_screenUpdateResolution < _samplingResolution)
                {
                    SamplingResolution = _screenUpdateResolution;
                }
            }
        }

        public double SamplingResolution
        {
            get => _samplingResolution;
            set
            {
                _samplingResolution = value;
                OnPropertyChanged(nameof(SamplingResolution));
                if (_samplingResolution > _screenUpdateResolution)
                {
                    ScreenUpdateResolution = _samplingResolution;
                }
            }
        }

        public PreferencesWindow()
        {
            InitializeComponent();
        }

        private void OnOkClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
