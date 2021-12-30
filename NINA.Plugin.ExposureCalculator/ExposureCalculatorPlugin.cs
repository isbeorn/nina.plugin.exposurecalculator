using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Plugin.ExposureCalculator {

    public class ExposureCalculatorMediator {

        private ExposureCalculatorMediator() { }

        private static readonly Lazy<ExposureCalculatorMediator> lazy = new Lazy<ExposureCalculatorMediator>(() => new ExposureCalculatorMediator());

        public static ExposureCalculatorMediator Instance { get => lazy.Value; }
        public void RegisterPlugin(ExposureCalculatorPlugin toolsPlugin) {
            this.ToolsPlugin = toolsPlugin;
        }

        public ExposureCalculatorPlugin ToolsPlugin { get; private set; }


    }

    [Export(typeof(IPluginManifest))]
    public class ExposureCalculatorPlugin : PluginBase {
        [ImportingConstructor]
        public ExposureCalculatorPlugin(IProfileService profileService) {
            this.profileService = profileService;
            ExposureCalculatorMediator.Instance.RegisterPlugin(this);


        }

        private IProfileService profileService;
    }
}
