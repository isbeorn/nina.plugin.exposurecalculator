using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Exposure Calculator")]
[assembly: AssemblyDescription("A tool to recommend an exposure time based on read noise and sky glow.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Stefan Berg")]
[assembly: AssemblyProduct("NINA.Plugin")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2b4b2fd6-46ce-4f34-b184-4a8b3058dc86")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]

//The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "2.0.0.2020")]

//Your plugin homepage - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://www.patreon.com/stefanberg/")]
//The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
//The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
//The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/Isbeorn/nina.plugin.exposurecalculator/")]

[assembly: AssemblyMetadata("ChangelogURL", "")]

//Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "")]

//The featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "")]
//An example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//An additional example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", @"
This tool will suggest a recommended exposure time based on the read noise from the camera sensor and the average skyglow.
If SharpCap is installed and a Sensor Analysis is available for the current camera, RN and FW are derived from the sensor analysis.

1. Exposure time. This is only used to measure the average skyglow when clicking on (8)
2. Filter: this menu lets you choose the filter for the calculation
    > Filters affect the  wavelength bandpass of incident light and therefore the average skyglow. The analysis should be repeated for each filter to determine an optimal exposures set.
3. Gain: select the gain for the exposure analysis. 
    > Camera parameters vary significantly with gain values, the analysis should be repeated for the different gain values used for imaging. A guideline to determine the optimal gain values for your imaging conditions can be found [here](https://www.youtube.com/watch?v=ub1HjvlCJ5Y&list=PLhIb8N-jSR_rNKxCFGzbd87TfmyQS4U4X&index=14)
4. The drop-down menu lets you select available sensor analysis files from SharpCap
   > SharpCap must be installed and you must first perform a Sensor Analysis in SharpCap following the instructions [here](http://docs.sharpcap.co.uk/3.2/19_SensorAnalysis.htm). Sensor analysis files are saved in %APPDATA%\Roaming\SharpCap\SensorCharacteristics
5. Full Well Capacity in electrons: if known this value can be entered manually for the specified gain or retrieved automatically from the Sharpcap Sensor Analysis
6. Read Noise in electrons: if known this value can be entered manually for the specified gain or retrieved automatically from the Sharpcap Sensor Analysis
7. BIAS median value (in 16bit): median ADU value of a bias frame (scald to 16bit), can be entered manually or calculated automatically by covering the scope and clicking on the 'Calculate Bias' button
8.Click here to perform the exposure for the analysis
9.The recommended exposure times are displayed in this section

Recommended exposure time is calculated according to the following[formula](https://forums.sharpcap.co.uk/viewtopic.php?t=456):

`Recommended Exposure Time = 10 * read noise squared / light pollution rate`

where light pollution rate is defined as:

`(median ADU of a subframe - median of the bias) * electrons per ADU / the length of the exposure`

Further details on the theory for the optimal exposure calculation can be found [here](https://www.youtube.com/watch?v=3RH93UvP358)

 > The analysis will use whatever gain is specified and linear interpolate between the values calculated by sharpcap. for example, if you have read noise in the analysis for gains 100 and 150 but specify 125, the tool will set the read noise exactly between the two.

 > Remember to cool down your camera to the desired temperature before using the tool, high Dark Current values may affect the results.")]
