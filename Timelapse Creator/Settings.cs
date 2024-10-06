using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using Timelapse_Creator.Properties;

namespace Timelapse_Creator
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        // Settings for the timelapse application with default values
        public string SourceFolder { get; set; } = "C:\\Timelapse";

        #region FTP
        public string FTPServer { get; set; } = "192.168.0.123";
        public string FTPBasePath { get; set; } = "/home/ftp/sdcard/Camera1";
        public string FTPUser { get; set; } = "admin";
        public string FTPPassword { get; set; } = "";
        #endregion

        #region Preprocess
        public double PreprocessBrightThreshold { get; set; } = 0.2;
        public int PreprocessEveryNthImage { get; set; } = 1;
        public bool PreprocessTimestampFromFormat { get; set; } = true;
        public bool PreprocessTimestampFromFileProperty { get { return !PreprocessTimestampFromFormat; } set { PreprocessTimestampFromFormat = !value; } }
        public string PreprocessTimestampFormat { get; set; } = "yyyy-MM-dd.HH-mm-ss";
        public string PreprocessTimes { get; set; } = "09:00;15:00";
        #endregion

        #region Timelapse
        public int TimelapseEveryNthImage { get; set; } = 100;
        public int TimelapseResolutionX { get; set; } = 1920;
        public int TimelapseResolutionY { get; set; } = 1080;
        public int TimelapseFPS { get; set; } = 60;
        #endregion

        // Path where presets are stored
        [XmlIgnore]
        private string PresetsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Timelapse Creator", "Presets");

        // Combines the preset name with the directory to get the full path
        private string GetPresetPath(string presetName) => Path.Combine(PresetsPath, presetName + ".xml");

        // Returns a list of saved preset names by scanning the preset directory
        public List<string> GetSavedPresets()
        {
            // Creates the directory if it doesn't exist
            if (!Directory.Exists(PresetsPath))
                Directory.CreateDirectory(PresetsPath);

            // Scans the directory for XML files, strips the path and extension, and returns the list
            return Directory.GetFiles(PresetsPath, "*.xml")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToList();
        }

        // Loads a preset by its name, deserializing the XML file into a Settings object
        public void Load(string preset)
        {
            // Get the full path of the preset file
            string presetPath = GetPresetPath(preset);

            // Throws an exception if the file does not exist
            if (!File.Exists(presetPath))
                return;//throw new FileNotFoundException($"Preset '{preset}' not found.");

            // Deserialize the XML into a Settings object
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (StreamReader reader = new StreamReader(presetPath))
                {
                    Settings loadedSettings = (Settings)serializer.Deserialize(reader);

                    // Copy the properties from the loadedSettings into the current instance using reflection
                    foreach (var property in typeof(Settings).GetProperties())
                    {
                        property.SetValue(this, property.GetValue(loadedSettings));
                    }
                }
            }
            catch { }
        }

        // Saves the current settings to an XML file with the specified preset name
        public void Save(string preset)
        {
            // Ensure the directory exists before saving
            if (!Directory.Exists(PresetsPath))
                Directory.CreateDirectory(PresetsPath);

            // Get the full path of the preset file
            string presetPath = GetPresetPath(preset);

            // Serialize the current settings to an XML file
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (StreamWriter writer = new StreamWriter(presetPath))
            {
                serializer.Serialize(writer, this);
            }
        }

        //Deletes the settings for a specified preset name
        public void Delete(string preset)
        {
            // Get the full path of the preset file
            string presetPath = GetPresetPath(preset);

            if (File.Exists(presetPath))
                File.Delete(presetPath);
        }
    }
}
