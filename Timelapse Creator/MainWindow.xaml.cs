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

namespace Timelapse_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                #region Settings
                string sourceFolder = @"D:\Timelapse für Axel\raw";//this is where the images lie. I assume that in this folder you have folders for each day with the images of this day (e.g. \2022-12-11\08-00.jpg
                //string sourceFolder = @"D:\Timelapse für Axel\testfolder";
                int everyNthImage = 20;//only every nth image is extracted to the working folder
                bool preprocessImages = false;//this will sort out the images to a working folder (sourceFolder_working). sometimes you only want to generate the output or the video and not both
                bool generateTimelapse = false;//this takes all images of the working folder and creates a timelapse
                int timelapseResolutionX = 1920;//resolution of the timelapse
                int timelapseResolutionY = 1080;
                int timelapseFPS = 60;//take an educated guess what this is
                #endregion

                string workingFolder = new DirectoryInfo(sourceFolder).FullName.Trim('\\') + "_working\\";//where the processed images go

                if (preprocessImages)
                {
                    if (!Directory.Exists(sourceFolder))
                        throw new Exception($"Directory {sourceFolder} does not exist.");
                    if (Directory.Exists(workingFolder))
                    {
                        var res = MessageBox.Show("Do you want to delete the working folder\r\n" + workingFolder, "Delete backup?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (res != MessageBoxResult.Yes)
                            return;
                        Directory.Delete(workingFolder, true);
                    }
                    Directory.CreateDirectory(workingFolder);

                    log("Getting files and calculating brightnesses.");
                    var dirInfoTXT = System.IO.Path.Combine(workingFolder, "dirinfo.csv");//statistics of the folders. here you can check how many good and bad images you have for each folder
                    List<string> dirInfos = new List<string> { "Directory;OK Images;Gray Images;Too Dark Images;" };//statistics of the folders

                    List<float> brights = new List<float>();//debug
                    List<string> directories = Directory.GetDirectories(sourceFolder).ToList();//these will be processed

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    int i = 0;

                    Parallel.ForEach(directories, new ParallelOptions { MaxDegreeOfParallelism = 5 }, dir =>
                    {
                        try
                        {
                            //Counter for statistics
                            int grayImages = 0;
                            int okImages = 0;
                            int tooDarkImages = 0;
                            List<string> okFiles = new List<string>();//filenames of good images
                            List<string> files = Directory.GetFiles(dir, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();//these will be processed
                            List<bool> BrightImageMarker = new List<bool> { false, false, false };//floating list telling us the status of the last images processed - true is a ok image, false a dark image
                            for (int j = 0; j < files.Count; j++)
                            {
                                var img = new Bitmap(files.ElementAt(j));
                                double bright = 0;
                                bright = CalculateAverageLightness(img);
                                img.Dispose();

                                brights.Add((float)bright);

                                if (bright > 0.495 && bright < 0.505)
                                    grayImages++; //gray image (may occur with motion eye os when the connection to the cam broke down)
                                else if (bright < 0.2)
                                {
                                    if (BrightImageMarker.All(x => x == true))//we already had some ok images in a row and now the first dark - this is probably in the afternoon -> skip the rest. disable this if you have one large folder with all images.
                                    {
                                        tooDarkImages += files.Count - j;//add the missing files to the counter
                                        break;
                                    }
                                    tooDarkImages++; //too dark
                                    BrightImageMarker.Add(false);
                                    BrightImageMarker.Remove(BrightImageMarker.First());
                                }
                                else
                                {
                                    okImages++;//bright image
                                    BrightImageMarker.Add(true);
                                    BrightImageMarker.Remove(BrightImageMarker.First());
                                    okFiles.Add(files.ElementAt(j));
                                }
                            }
                            Parallel.ForEach(EveryNthElement(okFiles, everyNthImage), file => //only every nth image
                            {
                                var FI = new FileInfo(file);
                                System.IO.File.Copy(file, System.IO.Path.Combine(workingFolder, FI.Name));
                            });

                            dirInfos.Add($"{dir};{okImages};{grayImages};{tooDarkImages};");
                            var elapsed = sw.ElapsedMilliseconds * directories.Count / (i + 1) - sw.ElapsedMilliseconds;
                            string dirInfo = $"Finished {dir}. {i} of {directories.Count} directories processed. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                            log(dirInfo);
                            i++;
                        }
                        catch (Exception ex)
                        {
                            log($"Exception {dir}:\r\n{ex.Message}");
                        }
                    });
                    System.IO.File.WriteAllText(dirInfoTXT, string.Join("\r\n ", dirInfos));//write statistics
                    log("Finished files and calculating brightnesses.");
                }

                if (generateTimelapse)
                {
                    log("Creating video.");

                    var outputfile = System.IO.Path.Combine(workingFolder, "Timelapse.mp4");//this is the timelapse. duh.
                    if (System.IO.File.Exists(outputfile))
                        System.IO.File.Delete(outputfile);

                    #region Works but sucks
                    //    List<string> timelapseFiles = Directory.GetFiles(workingFolder, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();
                    //    var timelapseFilesTXT = System.IO.Path.Combine(workingFolder, "images.txt");
                    //    if (System.IO.File.Exists(timelapseFilesTXT))
                    //        System.IO.File.Delete(timelapseFilesTXT);
                    //    System.IO.File.WriteAllText(timelapseFilesTXT, "file " + string.Join("\r\nfile ", timelapseFiles.Select(R => new FileInfo(R).Name)));
                    //    /*
                    //    Following format:
                    //    file '2022-11-23.06-25-00.jpg' 
                    //    file '2022-11-23.06-26-00.jpg' 
                    //    file '2022-11-23.06-27-00.jpg' 
                    //    file '2022-11-23.06-28-00.jpg' 
                    //    file '2022-11-23.06-29-00.jpg' 
                    //    file '2022-11-23.06-30-00.jpg' 
                    //    file '2022-11-23.06-31-00.jpg' 
                    //    file '2022-11-23.06-32-00.jpg' 
                    //    */

                    //    //https://medium.com/@sekhar.rahul/creating-a-time-lapse-video-on-the-command-line-with-ffmpeg-1a7566caf877
                    //    //ffmpeg -framerate 60 -pattern_type glob -i "folder-with-photos/*.JPG" -s:v 1920x1080 -c:v libx264 -crf 17 -pix_fmt yuv420p my-timelapse.mp4

                    //    using (Process p = new Process())
                    //    {
                    //        p.StartInfo.UseShellExecute = false;
                    //        p.StartInfo.CreateNoWindow = true;
                    //        p.StartInfo.RedirectStandardOutput = true;
                    //        p.StartInfo.FileName = "ffmpeg";
                    //        p.StartInfo.Arguments = $" -safe 0 -r 60 -f concat -i \"{timelapseFilesTXT}\" \"{outputfile}\"";
                    //        //p.StartInfo.Arguments = $"-framerate 60 -pattern_type glob -i \"{workingFolder}/*.JPG\" -s:v 1920x1080 -c:v libx264 -crf 17 -pix_fmt yuv420p my-timelapse.mp4";
                    //        p.Start();
                    //        p.WaitForExit();

                    //        var res = p.StandardOutput.ReadToEnd();
                    //    }
                    //    //string cmd = $"ffmpeg -safe 0 -f concat -i \"{timelapseFilesTXT}\" \"{outputfile}\"";
                    //    //Process.Start(cmd);
                    #endregion

                    //Probably needs https://ffmpeg.org/download.html installed
                    //Code snippets from https://www.youtube.com/watch?v=WDOupC5dyIQ&ab_channel=IrisClasson
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    using (var vw = new VideoFileWriter())//start writing video
                    {
                        vw.Open(outputfile, timelapseResolutionX, timelapseResolutionY, timelapseFPS, VideoCodec.MPEG4);
                        var files = Directory.GetFiles(workingFolder, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();//get all images
                        //files = EveryNthElement(files, 3);//if you want to skip some images here
                        for (int i = 0; i < files.Count; i++)
                        {
                            Bitmap image = Bitmap.FromFile(files.ElementAt(i)) as Bitmap;//read the original image
                            Bitmap resized = new Bitmap(image, new System.Drawing.Size(timelapseResolutionX, timelapseResolutionY));//resize it - ffmpeg just cuts it if you don't
                            vw.WriteVideoFrame(resized);//write it to the video
                            image.Dispose();//cleanup
                            resized.Dispose();
                            var elapsed = sw.ElapsedMilliseconds * files.Count / (i + 1) - sw.ElapsedMilliseconds;
                            log($"{i} of {files.Count} images processed. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min");//estimate duration
                        }
                        vw.Close();
                    }
                }

                log("Finished.");

                //No discussion. Just get it done.
                Process.GetCurrentProcess().Kill();
                Application.Current.Shutdown();
                this.Close();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Creating Timelapse\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Log message to console with timestamp.
        /// </summary>
        /// <param name="msg"></param>
        private void log(string msg)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " - " + msg);
        }
        /// <summary>
        /// https://www.dotnetperls.com/every-nth-element
        /// </summary>
        /// <param name="list"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        static List<string> EveryNthElement(List<string> list, int n)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                // Use a modulo expression.
                if ((i % n) == 0)
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Stolen from https://stackoverflow.com/questions/7964839/determine-image-overall-lightness
        /// </summary>
        /// <param name="bm"></param>
        /// <returns></returns>
        public static double CalculateAverageLightness(Bitmap bm)
        {
            try
            {
                double lum = 0;
                var tmpBmp = new Bitmap(bm);
                var width = bm.Width;
                var height = bm.Height;
                var bppModifier = bm.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb ? 3 : 4;

                var srcData = tmpBmp.LockBits(new System.Drawing.Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
                var stride = srcData.Stride;
                var scan0 = srcData.Scan0;

                //Luminance (standard, objective): (0.2126*R) + (0.7152*G) + (0.0722*B)
                //Luminance (perceived option 1): (0.299*R + 0.587*G + 0.114*B)
                //Luminance (perceived option 2, slower to calculate): sqrt( 0.299*R^2 + 0.587*G^2 + 0.114*B^2 )

                unsafe
                {
                    byte* p = (byte*)(void*)scan0;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = (y * stride) + x * bppModifier;
                            lum += (0.299 * p[idx + 2] + 0.587 * p[idx + 1] + 0.114 * p[idx]);
                        }
                    }
                }
                tmpBmp.UnlockBits(srcData);
                tmpBmp.Dispose();
                var avgLum = lum / (width * height);
                return avgLum / 255.0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        //private static void CopyFilesRecursively(string sourcePath, string targetPath)
        //{
        //    Directory.CreateDirectory(targetPath);
        //    //Now Create all of the directories
        //    foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        //    {
        //        Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        //    }

        //    //Copy all the files & Replaces any files with the same name
        //    foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        //    {
        //        File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        //    }
        //}
    }
}
