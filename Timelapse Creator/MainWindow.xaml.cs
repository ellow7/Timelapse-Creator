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
                string sourceFolder = @"D:\Timelapse für Axel\raw";
                //string sourceFolder = @"D:\Timelapse für Axel\testfolder";
                int everyNthImage = 20;//every nth image is extracted to the working folder
                bool generateWorkingFolder = false;
                bool generateVideo = true;

                string workingFolder = new DirectoryInfo(sourceFolder).FullName.Trim('\\') + "_working\\";

                if (generateWorkingFolder)
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
                    var dirInfoTXT = System.IO.Path.Combine(workingFolder, "dirinfo.csv");
                    if (System.IO.File.Exists(dirInfoTXT))
                        System.IO.File.Delete(dirInfoTXT);

                    List<float> brights = new List<float>();//debug

                    List<string> directories = Directory.GetDirectories(sourceFolder).ToList();
                    List<string> dirInfos = new List<string> { "Directory;OK Images;Gray Images;Too Dark Images;" };

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    int i = 0;
                    Parallel.ForEach(directories, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                        dir =>
                    {
                        try
                        {
                            int grayImages = 0;
                            int okImages = 0;
                            int tooDarkImages = 0;
                            List<string> okFiles = new List<string>();
                            List<string> files = Directory.GetFiles(dir, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();
                            List<bool> BrightImages = new List<bool> { false, false, false };
                            for (int j = 0; j < files.Count; j++)
                            {
                                var img = new Bitmap(files.ElementAt(j));
                                double bright = 0;
                                bright = CalculateAverageLightness(img);
                                img.Dispose();

                                brights.Add((float)bright);

                                if (bright > 0.495 && bright < 0.505)
                                    grayImages++; //gray image
                                else if (bright < 0.2)
                                {
                                    if (BrightImages.All(x => x == true))//we already had some ok images in a row and now the first dark - this is probably in the afternoon -> skip the rest
                                    {
                                        tooDarkImages += files.Count - j;
                                        break;
                                    }
                                    tooDarkImages++; //too dark
                                    BrightImages.Add(false);
                                    BrightImages.Remove(BrightImages.First());
                                }
                                else
                                {
                                    BrightImages.Add(true);
                                    BrightImages.Remove(BrightImages.First());
                                    okImages++;
                                    okFiles.Add(files.ElementAt(j));
                                }
                            }
                            Parallel.ForEach(EveryNthElement(okFiles, everyNthImage), file => //every nth image
                            {
                                var FI = new FileInfo(file);
                                System.IO.File.Copy(file, System.IO.Path.Combine(workingFolder, FI.Name));
                            });

                            var elapsed = sw.ElapsedMilliseconds * directories.Count / (i + 1) - sw.ElapsedMilliseconds;
                            string dirInfo = $"Finished {dir}. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min";
                            dirInfos.Add($"{dir};{okImages};{grayImages};{tooDarkImages};");
                            i++;

                            log(dirInfo);
                        }
                        catch (Exception ex)
                        {
                            log($"Exception {dir}:\r\n{ex.Message}");
                        }
                    });
                    System.IO.File.WriteAllText(dirInfoTXT, string.Join("\r\n ", dirInfos));
                    log("Finished files and calculating brightnesses.");
                }

                if (generateVideo)
                {
                    log("Creating video.");

                    var outputfile = System.IO.Path.Combine(workingFolder, "Timelapse.mp4");
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

                    //https://ffmpeg.org/download.html
                    //https://www.youtube.com/watch?v=WDOupC5dyIQ&ab_channel=IrisClasson
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    using (var vw = new VideoFileWriter())
                    {
                        //vw.Open(outputfile, 1920, 1080, 60, VideoCodec.MPEG4);
                        vw.Open(outputfile, 1280, 720, 60, VideoCodec.MPEG4);
                        var files = Directory.GetFiles(workingFolder, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();
                        files = EveryNthElement(files, 3);
                        //foreach (var file in files)
                        for (int i = 0; i < files.Count; i++)
                        {
                            var elapsed = sw.ElapsedMilliseconds * files.Count / (i + 1) - sw.ElapsedMilliseconds;
                            log($"{i} of {files.Count} images processed. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min");
                            Bitmap image = Bitmap.FromFile(files.ElementAt(i)) as Bitmap;

                            Bitmap resized = new Bitmap(image, new System.Drawing.Size(1280, 720));

                            vw.WriteVideoFrame(resized);
                            image.Dispose();
                            resized.Dispose();
                            //if (i > 500)//debug
                            //    break;
                        }
                        vw.Close();
                    }
                }

                log("Finished.");

                ;







            }
            catch (Exception ex)
            {
                MessageBox.Show("Creating Timelapse\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }




        }
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
