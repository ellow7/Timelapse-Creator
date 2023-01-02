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

            TBPreprocessSourceFolder.Text = Properties.Settings.Default.SourceFolder;

            LogTimer = new System.Timers.Timer(500);
            LogTimer.Elapsed += LogTimer_Elapsed;
            LogTimer.Start();

            Log("Started");
        }

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

        #region Preprocess
        private void TBPreprocessSourceFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TBPreprocessWorkingFolder == null)
                return;
            TBPreprocessWorkingFolder.Text = "";
            TBTimelapseWorkingFolder.Text = "";
            TBTimelapse.Text = "";
            var folder = TBPreprocessSourceFolder.Text;
            if (!new DirectoryInfo(folder).Exists)
                return;

            string workingFolder = new DirectoryInfo(folder).FullName.Trim('\\') + "_working\\";
            string timelapseFile = System.IO.Path.Combine(workingFolder, "Timelapse.mp4");
            TBPreprocessWorkingFolder.Text = workingFolder;
            TBTimelapseWorkingFolder.Text = workingFolder;
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
                Properties.Settings.Default.SourceFolder = cofd.FileName;
                Properties.Settings.Default.Save();
                TBPreprocessSourceFolder.Text = cofd.FileName;
            }
        }
        private void BTPreprocessPreprocess_Click(object sender, RoutedEventArgs e)
        {
            string SourceFolder = TBPreprocessSourceFolder.Text;
            string WorkingFolder = TBPreprocessWorkingFolder.Text;
            int EveryNthImage = int.Parse(TBPreprocessEveryNthImage.Text);
            float BrightTreshold = float.Parse(TBPreprocessBrightTreshold.Text, new CultureInfo("en-GB").NumberFormat);

            Thread t = new Thread(() =>
                Preprocessor.Preprocess(
                    SourceFolder,
                    WorkingFolder,
                    EveryNthImage,
                    BrightTreshold
                    ));
            t.Start();
        }
        #endregion

        #region Timelapse
        private void BTTimelapseCreateTimelapse_Click(object sender, RoutedEventArgs e)
        {
            string WorkingFolder = TBTimelapseWorkingFolder.Text;
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
        }

        private void BTTimelapseOpenTimelapse_Click(object sender, RoutedEventArgs e)
        {
            var file = TBTimelapse.Text;
            if (new FileInfo(file).Exists)
                Process.Start(file);
            else
                Log("Timelapse file not existing");
        }
        #endregion

        #region Helper
        private void BTOpenWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = TBPreprocessWorkingFolder.Text;
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
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));

        }
        #endregion
    }
}
