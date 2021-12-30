using NINA.Core.Locale;
using NINA.Core.Model.Equipment;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Model;
using NINA.Image.ImageData;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.Plugin.ExposureCalculator {
    [Export(typeof(IDockableVM))]
    public class ExposureCalculatorDock : DockableVM, IExposureCalculatorDock, ICameraConsumer {
        private double _recommendedExposureTime;
        private FilterInfo _snapFilter;
        private ISharpCapSensorAnalysisReader _sharpCapSensorAnalysisReader;
        private readonly ICameraMediator cameraMediator;
        private CancellationTokenSource _cts;
        private string _sharpCapSensorAnalysisDisabledValue;
        private ImmutableDictionary<string, SharpCapSensorAnalysisData> _sharpCapSensorAnalysisData;

        [ImportingConstructor]

        public ExposureCalculatorDock(IProfileService profileService, IImagingMediator imagingMediator, ICameraMediator cameraMediator) : this(profileService, imagingMediator, new DefaultSharpCapSensorAnalysisReader(), cameraMediator) { }

        public ExposureCalculatorDock(IProfileService profileService, IImagingMediator imagingMediator, ISharpCapSensorAnalysisReader sharpCapSensorAnalysisReader, ICameraMediator cameraMediator)
            : base(profileService) {



            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(ExposureCalculatorMediator.Instance.ToolsPlugin.Identifier));

            this._imagingMediator = imagingMediator;
            this.Title = Loc.Instance["LblExposureCalculator"];
            this._sharpCapSensorAnalysisReader = sharpCapSensorAnalysisReader;
            this.cameraMediator = cameraMediator;
            if (Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)Application.Current.Resources["CalculatorSVG"];
            }
            OpenSharpCapSensorAnalysisFolderDiagCommand = new RelayCommand(OpenSharpCapSensorAnalysisFolderDiag);

            cameraMediator.RegisterConsumer(this);

            DetermineExposureTimeCommand = new AsyncCommand<bool>(async (o) => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await DetermineExposureTime(o);
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this));
            CancelDetermineExposureTimeCommand = new RelayCommand(TriggerCancelToken);
            DetermineBiasCommand = new AsyncCommand<bool>(async (o) => {
                cameraMediator.RegisterCaptureBlock(this);
                try {
                    var result = await DetermineBias(o);
                    return result;
                } finally {
                    cameraMediator.ReleaseCaptureBlock(this);
                }
            }, (o) => cameraMediator.IsFreeToCapture(this));
            ReloadSensorAnalysisCommand = new AsyncCommand<bool>(ReloadSensorAnalysis);
            CancelDetermineBiasCommand = new RelayCommand(TriggerCancelToken);

            this._sharpCapSensorAnalysisDisabledValue = "(" + Loc.Instance["LblDisabled"] + ")";
            this._sharpCapSensorNames = new ObservableCollection<string>();
            var configuredPath = SharpCapSensorAnalysisFolder;
            if (String.IsNullOrEmpty(configuredPath)) {
                // Attempt load for default configuration only if the directory exists to avoid log spam
                if (Directory.Exists(SharpCapSensorAnalysisConstants.DEFAULT_SHARPCAP_SENSOR_ANALYSIS_PATH)) {
                    LoadSensorAnalysisData(SharpCapSensorAnalysisConstants.DEFAULT_SHARPCAP_SENSOR_ANALYSIS_PATH);
                }
            } else {
                LoadSensorAnalysisData(configuredPath);
            }
        }

        public ICommand OpenSharpCapSensorAnalysisFolderDiagCommand { get; private set; }

        private void OpenSharpCapSensorAnalysisFolderDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = SharpCapSensorAnalysisFolder;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    SharpCapSensorAnalysisFolder = diag.SelectedPath + "\\";

                   
                    var sensorAnalysisData = LoadSensorAnalysisData(SharpCapSensorAnalysisFolder);
                    Notification.ShowInformation(String.Format(Loc.Instance["LblSharpCapSensorAnalysisLoadedFormat"], sensorAnalysisData.Count));
                }
            }
        }

        public override bool IsTool => true;

        private static ImmutableDictionary<string, SharpCapSensorAnalysisData> ReadSensorAnalysisData(ISharpCapSensorAnalysisReader sharpCapSensorAnalysisReader, string path) {
            try {
                return sharpCapSensorAnalysisReader.Read(path);
            } catch (Exception ex) {
                Logger.Error(ex, "Failed to read SharpCap sensor analysis data");
                return ImmutableDictionary.Create<string, SharpCapSensorAnalysisData>();
            }
        }

        public ImmutableDictionary<string, SharpCapSensorAnalysisData> LoadSensorAnalysisData(string path) {
            SharpCapSensorNames.Clear();
            this._sharpCapSensorAnalysisData = ReadSensorAnalysisData(this._sharpCapSensorAnalysisReader, path);
            if (!this._sharpCapSensorAnalysisData.IsEmpty) {
                SharpCapSensorNames.Add(this._sharpCapSensorAnalysisDisabledValue);
                foreach (var key in this._sharpCapSensorAnalysisData.Keys.OrderBy(x => x)) {
                    SharpCapSensorNames.Add(key);
                }
            }
            RaisePropertyChanged("SharpCapSensorNames");
            return this._sharpCapSensorAnalysisData;
        }

        private void TriggerCancelToken(object obj) {
            _cts?.Cancel();
        }

        private async Task<AllImageStatistics> TakeExposure(double exposureDuration) {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var seq = new CaptureSequence(exposureDuration, CaptureSequence.ImageTypes.SNAPSHOT, SnapFilter, new BinningMode(1, 1), 1);
            seq.Gain = SnapGain;
            var prepareParameters = new PrepareImageParameters(autoStretch: true, detectStars: false);
            var capture = await _imagingMediator.CaptureAndPrepareImage(seq, prepareParameters, _cts.Token, null); //todo progress
            return AllImageStatistics.Create(capture.RawImageData);
        }

        private async Task<bool> DetermineExposureTime(object arg) {
            this.Statistics = null;
            this.Statistics = await TakeExposure(SnapExposureDuration);
            this.CalculateRecommendedExposureTime();
            return true;
        }

        private async Task<bool> DetermineBias(object arg) {
            MyMessageBox.Show(Loc.Instance["LblCoverScopeMsgBoxTitle"]);
            var imageStatistics = await TakeExposure(0);
            this.BiasMedian = (await imageStatistics.ImageStatistics).Median;
            return true;
        }

        private Task<bool> ReloadSensorAnalysis(object obj) {
            var path = String.IsNullOrEmpty(SharpCapSensorAnalysisFolder)
                ? SharpCapSensorAnalysisConstants.DEFAULT_SHARPCAP_SENSOR_ANALYSIS_PATH
                : SharpCapSensorAnalysisFolder;
            var sensorAnalysisData = LoadSensorAnalysisData(path);
            Notification.ShowInformation(String.Format(Loc.Instance["LblSharpCapSensorAnalysisLoadedFormat"], sensorAnalysisData.Count));
            return Task.FromResult(true);
        }

        public IAsyncCommand DetermineExposureTimeCommand { get; private set; }
        public ICommand CancelDetermineExposureTimeCommand { get; private set; }
        public IAsyncCommand DetermineBiasCommand { get; private set; }
        public ICommand CancelDetermineBiasCommand { get; private set; }
        public ICommand ReloadSensorAnalysisCommand { get; private set; }

        private ObservableCollection<string> _sharpCapSensorNames;

        public ObservableCollection<string> SharpCapSensorNames {
            get {
                if (_sharpCapSensorNames == null) {
                    _sharpCapSensorNames = new ObservableCollection<string>();
                }
                return _sharpCapSensorNames;
            }
            set {
                _sharpCapSensorNames = value;
            }
        }

        private void OnGainUpdated(int newValue) {
            if (this.IsSharpCapSensorAnalysisEnabled && newValue >= 0) {
                var analysisData = this._sharpCapSensorAnalysisData[this._mySharpCapSensor];
                var readNoiseEstimate = analysisData.EstimateReadNoise((double)newValue);
                var fullWellCapacityEstimate = analysisData.EstimateFullWellCapacity((double)newValue);
                this.ReadNoise = readNoiseEstimate.EstimatedValue;
                this.FullWellCapacity = fullWellCapacityEstimate.EstimatedValue;
            }
        }

        private void OnSharpCapSensorChanged(string newValue) {
            if (String.IsNullOrEmpty(newValue) || newValue == this._sharpCapSensorAnalysisDisabledValue) {
                this.IsSharpCapSensorAnalysisEnabled = false;
            } else {
                this.IsSharpCapSensorAnalysisEnabled = true;
                if (this.SnapGain < 0) {
                    // If gain isn't set yet, get the first gain value from the sensor analysis to initialize the UI
                    var analysisData = this._sharpCapSensorAnalysisData[this._mySharpCapSensor];
                    this.SnapGain = (int)analysisData.GainData[0].Gain;
                } else {
                    this.OnGainUpdated(this.SnapGain);
                }
            }
        }

        private bool _isSharpCapSensorAnalysisEnabled = false;

        public bool IsSharpCapSensorAnalysisEnabled {
            get => this._isSharpCapSensorAnalysisEnabled;
            set {
                this._isSharpCapSensorAnalysisEnabled = value;
                RaisePropertyChanged();
            }
        }

        private string _mySharpCapSensor;

        public string MySharpCapSensor {
            get => _mySharpCapSensor;

            set {
                _mySharpCapSensor = value;
                this.OnSharpCapSensorChanged(value);
                RaisePropertyChanged();
            }
        }

        public int SnapGain {
            get => pluginSettings.GetValueInt32(nameof(SnapGain), -1);

            set {
                pluginSettings.SetValueInt32(nameof(SnapGain), value);                
                this.OnGainUpdated(value);
                RaisePropertyChanged();
            }
        }

        public FilterInfo SnapFilter {
            get => _snapFilter;

            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        public string SharpCapSensorAnalysisFolder {
            get {
                var value = pluginSettings.GetValueString(nameof(SharpCapSensorAnalysisFolder), "");
                if(string.IsNullOrEmpty(value)) {

                    var scFolder = Environment.ExpandEnvironmentVariables(@"%APPDATA%\SharpCap\SensorCharacteristics\");
                    if (Directory.Exists(scFolder)) {
                        value = scFolder;
                    }
                }
                return value;
                       
             }

            set {
                pluginSettings.SetValueString(nameof(SharpCapSensorAnalysisFolder), value);
                RaisePropertyChanged();
            }
        }

        public double SnapExposureDuration {
            get => pluginSettings.GetValueDouble(nameof(SnapExposureDuration), 30);

            set {
                pluginSettings.SetValueDouble(nameof(SnapExposureDuration), value);
                RaisePropertyChanged();
            }
        }

        public double FullWellCapacity {
            get => pluginSettings.GetValueDouble(nameof(FullWellCapacity), 0);
            set {
                pluginSettings.SetValueDouble(nameof(FullWellCapacity), value);
                RaisePropertyChanged();
            }
        }

        public double ReadNoise {
            get => pluginSettings.GetValueDouble(nameof(ReadNoise), 0); 
            set {
                pluginSettings.SetValueDouble(nameof(ReadNoise), value);
                RaisePropertyChanged();
            }
        }

        public double BiasMedian {
            get => pluginSettings.GetValueDouble(nameof(BiasMedian), 0);
            set {
                pluginSettings.SetValueDouble(nameof(BiasMedian), value);
                RaisePropertyChanged();
            }
        }

        public double RecommendedExposureTime {
            get {
                return _recommendedExposureTime;
            }
            set {
                _recommendedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private AllImageStatistics _statistics;
        private IPluginOptionsAccessor pluginSettings;
        private IImagingMediator _imagingMediator;

        public AllImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        private void CalculateRecommendedExposureTime() {
            if (Statistics.ImageStatistics.Result.Median - BiasMedian < 0) {
                this.Statistics = null;
                Notification.ShowError(Loc.Instance["LblExposureCalculatorMeanLessThanOffset"]);
            } else {
                // Optimal exposure time is: 10 * ReadNoiseSquared / LightPollutionRate
                // Read noise units is electrons per ADU
                // Light pollution units is electrons per ADU per second, which we get by:
                //   1) Subtract the bias mean from the median of a snapshot (= skyglow in ADU)
                //   2) Convert to electrons by multiplying the electrons per ADU
                //   3) Divide by the snapshot exposure duration
                var maxAdu = 1 << Statistics.ImageProperties.BitDepth;
                var electronsPerAdu = FullWellCapacity / maxAdu;
                var skyglowFluxPerSecond = (Statistics.ImageStatistics.Result.Median - BiasMedian) * electronsPerAdu / SnapExposureDuration;
                var readNoiseSquared = ReadNoise * ReadNoise;
                RecommendedExposureTime = 10 * readNoiseSquared / skyglowFluxPerSecond;

                var debugMessage = $@"Recommended exposure calculation
Read Noise = {ReadNoise} electrons
Full Well Capacity = {FullWellCapacity} electrons
Bit Depth = {Statistics.ImageProperties.BitDepth}
Electrons per ADU = {electronsPerAdu}
Skyglow = Image Median ({Statistics.ImageStatistics.Result.Median}) - Bias Median ({BiasMedian}) = {Statistics.ImageStatistics.Result.Median - BiasMedian} ADU
Skyglow Flux per Second = {skyglowFluxPerSecond} electrons per pixel per second
Recommended exposure time = 10 * Read Noise ^ 2 = {RecommendedExposureTime} seconds";
                Logger.Debug(debugMessage);
            }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }
    }
}
