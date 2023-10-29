using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Net.WebRequestMethods;
using System.Diagnostics;
using System.Security.Cryptography;
using Accord.Video.FFMPEG;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using System.Timers;
using Accord.IO;
using System.Windows.Threading;
using System.Globalization;
using System.Net;
using Renci.SshNet;

namespace Timelapse_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer LogTimer;
        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();

            LogTimer = new System.Timers.Timer(500);
            LogTimer.Elapsed += LogTimer_Elapsed;
            LogTimer.Start();

            Log("Started");
        }

        #region Settings
        /// <summary>
        /// Load settings and apply them to the controls.
        /// </summary>
        private void LoadSettings()
        {
            TBSourceFolder.Text = Properties.Settings.Default.SourceFolder;
            TBFTPServer.Text = Properties.Settings.Default.FTPServer;
            TBFTPBasepath.Text = Properties.Settings.Default.FTPBasePath;
            TBFTPUser.Text = Properties.Settings.Default.FTPUser;
            TBPreprocessEveryNthImage.Text = Properties.Settings.Default.PreprocessEveryNthImage.ToString();
            TBPreprocessBrightThreshold.Text = Properties.Settings.Default.PreprocessBrightThreshold.ToString();

            TBTimelapseEveryNthImage.Text = Properties.Settings.Default.TimelapseEveryNthImage.ToString();
            TBTimelapseResolutionX.Text = Properties.Settings.Default.TimelapseResolutionX.ToString();
            TBTimelapseResolutionY.Text = Properties.Settings.Default.TimelapseResolutionY.ToString();
            TBTimelapseFPS.Text = Properties.Settings.Default.TimelapseFPS.ToString();
        }
        /// <summary>
        /// Save settings from the controls to the settings.
        /// </summary>
        private void SaveFTPSettings()
        {
            Properties.Settings.Default.SourceFolder = TBSourceFolder.Text;
            Properties.Settings.Default.FTPServer = TBFTPServer.Text;
            Properties.Settings.Default.FTPBasePath = TBFTPBasepath.Text;
            Properties.Settings.Default.FTPUser = TBFTPUser.Text;

            Properties.Settings.Default.Save();
        }
        /// <summary>
        /// Save settings from the controls to the settings.
        /// </summary>
        private void SavePreprocessSettings()
        {
            Properties.Settings.Default.SourceFolder = TBSourceFolder.Text;
            Properties.Settings.Default.PreprocessEveryNthImage = Convert.ToInt32(TBPreprocessEveryNthImage.Text);
            Properties.Settings.Default.PreprocessBrightThreshold = double.Parse(TBPreprocessBrightThreshold.Text.Replace(",", "."), CultureInfo.InvariantCulture);

            Properties.Settings.Default.Save();
        }
        /// <summary>
        /// Save settings from the controls to the settings.
        /// </summary>
        private void SaveTimelapseSettings()
        {
            Properties.Settings.Default.SourceFolder = TBSourceFolder.Text;
            Properties.Settings.Default.TimelapseEveryNthImage = Convert.ToInt32(TBTimelapseEveryNthImage.Text);
            Properties.Settings.Default.TimelapseResolutionX = Convert.ToInt32(TBTimelapseResolutionX.Text);
            Properties.Settings.Default.TimelapseResolutionY = Convert.ToInt32(TBTimelapseResolutionY.Text);
            Properties.Settings.Default.TimelapseFPS = Convert.ToInt32(TBTimelapseFPS.Text);

            Properties.Settings.Default.Save();
        }
        #endregion

        #region Logging
        private static List<string> logs = new List<string>();
        /// <summary>
        /// Log message to console with timestamp.
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
        {
            msg = DateTime.Now.ToLongTimeString() + " - " + msg;
            logs.Add(msg);
            Console.WriteLine(msg);
        }
        /// <summary>
        /// Updates the log.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TBLog.Dispatcher.CheckAccess())
            {
                // The calling thread owns the dispatcher, and hence the UI element
                if (logs.Count == 0)
                    return;
                var logsToWrite = logs.DeepClone();
                logs = new List<string>();
                TBLog.AppendText(Environment.NewLine + string.Join(Environment.NewLine, logsToWrite));
                TBLog.ScrollToEnd();
            }
            else
            {
                // Invokation required
                TBLog.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LogTimer_Elapsed(sender, e)));
            }
        }
        #endregion

        #region FTP
        private void BTPreprocessGetFTPImages_Click(object sender, RoutedEventArgs e)
        {
            string FTPServer = TBFTPServer.Text;
            string FTPUser = TBFTPUser.Text;
            string FTPPass = TBFTPPass.Text;
            string FTPBasepath = TBFTPBasepath.Text;
            string SourceFolder = TBSourceFolder.Text;

            Thread t = new Thread(() =>
                DownloadFTPFiles(
                    FTPServer,
                    FTPUser,
                    FTPPass,
                    FTPBasepath,
                    SourceFolder
                    ));
            t.Start();

            SaveFTPSettings();
        }

        private void DownloadFTPFiles(string sftpHost, string sftpUser, string sftpPassword, string remoteBasePath, string localBasePath)
        {
            DownloadFTPFilesRecursively(sftpHost, sftpUser, sftpPassword, remoteBasePath, localBasePath);
            Log($"FTP finished from {remoteBasePath}");
        }
        private void DownloadFTPFilesRecursively(string sftpHost, string sftpUser, string sftpPassword, string remoteBasePath, string localBasePath)
        {
            try
            {
                using (var client = new SftpClient(sftpHost, sftpUser, sftpPassword))
                {
                    client.Connect();

                    Log($"Reading files from {remoteBasePath}");
                    var files = client.ListDirectory(remoteBasePath);
                    foreach (var file in files)
                    {
                        if (file.Name == "." || file.Name == "..")
                        {
                            continue;
                        }

                        if (file.IsDirectory)
                        {
                            // Create the local directory if it doesn't exist
                            Directory.CreateDirectory(System.IO.Path.Combine(localBasePath, file.Name));

                            // Recursively get files in the directory
                            DownloadFTPFilesRecursively(sftpHost, sftpUser, sftpPassword, file.FullName, System.IO.Path.Combine(localBasePath, file.Name));
                        }
                        else
                        {
                            string from = file.FullName;
                            string to = System.IO.Path.Combine(localBasePath, file.Name);

                            // Download the file
                            using (var stream = System.IO.File.Create(to))
                            {
                                client.DownloadFile(from, stream);
                            }

                            // Delete the file on the SFTP server
                            client.DeleteFile(from);

                            Log($"Moved file {file.FullName}");
                        }
                    }
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error getting FTP images.\r\n" + ex.Message, "FTP Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log("Error getting FTP images.\r\n" + ex.Message);
            }
        }
        #endregion

        #region Preprocess
        private void TBPreprocessSourceFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TBWorkingFolder == null)
                return;
            TBWorkingFolder.Text = "";
            TBPreprocessInfoFile.Text = "";
            TBTimelapse.Text = "";
            var folder = TBSourceFolder.Text;
            if (folder == "" || !new DirectoryInfo(folder).Exists)
                return;

            string workingFolder = new DirectoryInfo(folder).FullName.Trim('\\') + "_working\\";
            string timelapseFile = System.IO.Path.Combine(workingFolder, "Timelapse.mp4");
            string infoFile = System.IO.Path.Combine(workingFolder, "Info.csv");
            TBWorkingFolder.Text = workingFolder;
            TBPreprocessInfoFile.Text = infoFile;
            TBTimelapse.Text = timelapseFile;
        }

        private void BTBrowseSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var cofd = new CommonOpenFileDialog();
            cofd.IsFolderPicker = true;
            cofd.Multiselect = false;
            cofd.EnsurePathExists = true;
            cofd.RestoreDirectory = false;
            if (Properties.Settings.Default.SourceFolder != "")
                cofd.DefaultDirectory = Properties.Settings.Default.SourceFolder;
            else
                cofd.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TBSourceFolder.Text = cofd.FileName;
            }
        }
        private void BTPreprocessPreprocess_Click(object sender, RoutedEventArgs e)
        {
            string SourceFolder = TBSourceFolder.Text;
            string WorkingFolder = TBWorkingFolder.Text;
            string PreprocessInfoFile = TBPreprocessInfoFile.Text;
            int EveryNthImage = int.Parse(TBPreprocessEveryNthImage.Text);
            double BrightThreshold = double.Parse(TBPreprocessBrightThreshold.Text.Replace(",", "."), CultureInfo.InvariantCulture);

            Thread t = new Thread(() =>
                Preprocessor.Preprocess(
                    SourceFolder,
                    WorkingFolder,
                    PreprocessInfoFile,
                    EveryNthImage,
                    BrightThreshold
                    ));
            t.Start();

            SavePreprocessSettings();
        }
        #endregion

        #region Timelapse
        private void BTTimelapseCreateTimelapse_Click(object sender, RoutedEventArgs e)
        {
            string WorkingFolder = TBWorkingFolder.Text;
            string TimelapseFile = TBTimelapse.Text;
            int EveryNthImage = int.Parse(TBTimelapseEveryNthImage.Text);
            int ResolutionX = int.Parse(TBTimelapseResolutionX.Text);
            int ResolutionY = int.Parse(TBTimelapseResolutionY.Text);
            int FPS = int.Parse(TBTimelapseFPS.Text);

            Thread t = new Thread(() =>
                TimelapseCreator.CreateTimelapse(
                    WorkingFolder,
                    TimelapseFile,
                    EveryNthImage,
                    ResolutionX,
                    ResolutionY,
                    FPS
                    ));
            t.Start();

            SaveTimelapseSettings();
        }

        private void BTTimelapseOpenTimelapse_Click(object sender, RoutedEventArgs e)
        {
            var file = TBTimelapse.Text;
            if (new FileInfo(file).Exists)
                Process.Start(file);
            else
                Log("Timelapse file not existing");
        }
        private void TBPreprocessOpenInfoFile_Click(object sender, RoutedEventArgs e)
        {
            var file = TBPreprocessInfoFile.Text;
            if (new FileInfo(file).Exists)
                Process.Start(file);
            else
                Log("Info file not existing");
        }
        #endregion

        #region Helper
        private void BTOpenWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = TBWorkingFolder.Text;
            if (new DirectoryInfo(folder).Exists)
                Process.Start(folder);
            else
                Log("Working folder not existing");
        }
        /// <summary>
        /// https://www.dotnetperls.com/every-nth-element
        /// </summary>
        public static List<string> EveryNthElement(List<string> list, int n)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < list.Count; i++)
                if ((i % n) == 0)
                    result.Add(list[i]);
            return result;
        }
        private void TextBox_Int_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out int value);//only allow int
        }
        private void TextBox_Float_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("^[,][0-9]+$|^[0-9]*[,]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));

        }
        #endregion
    }
}
