﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Timelapse_Creator.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.8.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:/Timelapse")]
        public string SourceFolder {
            get {
                return ((string)(this["SourceFolder"]));
            }
            set {
                this["SourceFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int PreprocessEveryNthImage {
            get {
                return ((int)(this["PreprocessEveryNthImage"]));
            }
            set {
                this["PreprocessEveryNthImage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.2")]
        public double PreprocessBrightThreshold {
            get {
                return ((double)(this["PreprocessBrightThreshold"]));
            }
            set {
                this["PreprocessBrightThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int TimelapseEveryNthImage {
            get {
                return ((int)(this["TimelapseEveryNthImage"]));
            }
            set {
                this["TimelapseEveryNthImage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1920")]
        public int TimelapseResolutionX {
            get {
                return ((int)(this["TimelapseResolutionX"]));
            }
            set {
                this["TimelapseResolutionX"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1080")]
        public int TimelapseResolutionY {
            get {
                return ((int)(this["TimelapseResolutionY"]));
            }
            set {
                this["TimelapseResolutionY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("60")]
        public int TimelapseFPS {
            get {
                return ((int)(this["TimelapseFPS"]));
            }
            set {
                this["TimelapseFPS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("192.168.0.123")]
        public string FTPServer {
            get {
                return ((string)(this["FTPServer"]));
            }
            set {
                this["FTPServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/home/ftp/sdcard/Camera1")]
        public string FTPBasePath {
            get {
                return ((string)(this["FTPBasePath"]));
            }
            set {
                this["FTPBasePath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("admin")]
        public string FTPUser {
            get {
                return ((string)(this["FTPUser"]));
            }
            set {
                this["FTPUser"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FTPPassword {
            get {
                return ((string)(this["FTPPassword"]));
            }
            set {
                this["FTPPassword"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("yyyy-MM-dd.HH-mm-ss")]
        public string PreprocessTimestampFormat {
            get {
                return ((string)(this["PreprocessTimestampFormat"]));
            }
            set {
                this["PreprocessTimestampFormat"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("09:00;15:00")]
        public string PreprocessTimes {
            get {
                return ((string)(this["PreprocessTimes"]));
            }
            set {
                this["PreprocessTimes"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PreprocessTimestampFromFormat {
            get {
                return ((bool)(this["PreprocessTimestampFromFormat"]));
            }
            set {
                this["PreprocessTimestampFromFormat"] = value;
            }
        }
    }
}
