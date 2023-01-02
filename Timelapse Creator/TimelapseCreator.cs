using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Timelapse_Creator
{
    public static class TimelapseCreator
    {
        public static void CreateTimelapse(string WorkingFolder, string TimelapseFile, int EveryNthImage = 1, int timelapseResolutionX = 1920, int timelapseResolutionY = 1080, int timelapseFPS = 60)
        {
            try
            {
                MainWindow.Log("Creating timelapse.");

                if (File.Exists(TimelapseFile))
                    File.Delete(TimelapseFile);

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
                    vw.Open(TimelapseFile, timelapseResolutionX, timelapseResolutionY, timelapseFPS, VideoCodec.MPEG4);
                    var files = Directory.GetFiles(WorkingFolder, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();//get all images
                    files = MainWindow.EveryNthElement(files, EveryNthImage);//if you want to skip some images here
                    for (int i = 0; i < files.Count; i++)
                    {
                        Bitmap image = Bitmap.FromFile(files.ElementAt(i)) as Bitmap;//read the original image
                        Bitmap resized = new Bitmap(image, new System.Drawing.Size(timelapseResolutionX, timelapseResolutionY));//resize it - ffmpeg just cuts it if you don't
                        vw.WriteVideoFrame(resized);//write it to the video
                        image.Dispose();//cleanup
                        resized.Dispose();
                        var elapsed = sw.ElapsedMilliseconds * files.Count / (i + 1) - sw.ElapsedMilliseconds;
                        MainWindow.Log($"{i} of {files.Count} images processed. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min");//estimate duration
                    }
                    vw.Close();
                }
                MainWindow.Log("Finished timeline.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating timelapse.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(ex.Message);
            }
        }
    }
}
