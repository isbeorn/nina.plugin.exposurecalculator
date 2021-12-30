using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageData;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Plugin.ExposureCalculator {
    public interface IExposureCalculatorDock : IDockableVM {
        double BiasMedian { get; set; }
        ICommand CancelDetermineBiasCommand { get; }
        ICommand CancelDetermineExposureTimeCommand { get; }
        IAsyncCommand DetermineBiasCommand { get; }
        IAsyncCommand DetermineExposureTimeCommand { get; }
        double FullWellCapacity { get; set; }
        bool IsSharpCapSensorAnalysisEnabled { get; set; }
        string MySharpCapSensor { get; set; }
        double ReadNoise { get; set; }
        double RecommendedExposureTime { get; set; }
        ICommand ReloadSensorAnalysisCommand { get; }
        ObservableCollection<string> SharpCapSensorNames { get; set; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        AllImageStatistics Statistics { get; set; }

        ImmutableDictionary<string, SharpCapSensorAnalysisData> LoadSensorAnalysisData(string path);
    }
}
