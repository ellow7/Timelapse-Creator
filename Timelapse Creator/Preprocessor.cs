using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Timelapse_Creator
{
    public static class Preprocessor
    {
        public static void Preprocess(string SourceFolder, string WorkingFolder, int EveryNthImage = 1, double BrightTreshold = 0.2)
        {
            try
            {
                MainWindow.Log("Preprocessing.");
                if (!Directory.Exists(SourceFolder))
                    throw new Exception($"Directory {SourceFolder} does not exist.");
                if (Directory.Exists(WorkingFolder))
                {
                    var res = MessageBox.Show("Do you want to delete the existing working folder\r\n" + WorkingFolder, "Delete working folder?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                        Directory.Delete(WorkingFolder, true);
                }
                Directory.CreateDirectory(WorkingFolder);

                MainWindow.Log("Getting folders.");
                var dirInfoTXT = Path.Combine(WorkingFolder, "dirinfo.csv");//statistics of the folders. here you can check how many good and bad images you have for each folder
                List<string> dirInfos = new List<string> { "Directory;OK Images;Gray Images;Too Dark Images;" };//statistics of the folders

                List<float> brights = new List<float>();//debug
                List<string> directories = Directory.GetDirectories(SourceFolder).ToList();//these will be processed

                Stopwatch sw = new Stopwatch();
                sw.Start();
                int i = 0;
                MainWindow.Log($"Getting files and calculating brightnesses of {directories.Count} folders.");

                Parallel.ForEach(directories, new ParallelOptions { MaxDegreeOfParallelism = 5 }, dir =>
                {
                    i++;
                    try
                    {
                        //Counter for statistics
                        int grayImages = 0;
                        int okImages = 0;
                        int tooDarkImages = 0;
                        List<string> okFiles = new List<string>();//filenames of good images
                        List<string> files = Directory.GetFiles(dir, "*.jpg", SearchOption.TopDirectoryOnly).OrderBy(R => R).ToList();//these will be processed
                        //List<bool> BrightImageMarker = new List<bool> { false, false, false };//floating list telling us the status of the last images processed - true is a ok image, false a dark image
                        for (int j = 0; j < files.Count; j++)
                        {
                            try
                            {
                                var img = new Bitmap(files.ElementAt(j));
                                double bright = 0;
                                bright = CalculateAverageLightness(img);
                                img.Dispose();

                                brights.Add((float)bright);

                                if (bright > 0.495 && bright < 0.505)
                                    grayImages++; //gray image (may occur with motion eye os when the connection to the cam broke down)
                                else if (bright < BrightTreshold)
                                {
                                    //if (BrightImageMarker.All(x => x == true))//we already had some ok images in a row and now the first dark - this is probably in the afternoon -> skip the rest. disable this if you have one large folder with all images.
                                    //{
                                    //    tooDarkImages += files.Count - j;//add the missing files to the counter
                                    //    break;
                                    //}
                                    tooDarkImages++; //too dark
                                    //BrightImageMarker.Add(false);
                                    //BrightImageMarker.Remove(BrightImageMarker.First());
                                }
                                else
                                {
                                    okImages++;//bright image
                                    //BrightImageMarker.Add(true);
                                    //BrightImageMarker.Remove(BrightImageMarker.First());
                                    okFiles.Add(files.ElementAt(j));
                                }
                            }
                            catch (Exception ex)
                            {
                                MainWindow.Log($"Exception {files.ElementAt(j)}:\r\n{ex.Message}");
                            }
                        }
                        Parallel.ForEach(MainWindow.EveryNthElement(okFiles, EveryNthImage), file => //only every nth image
                        {
                            var FI = new FileInfo(file);
                            File.Copy(file, Path.Combine(WorkingFolder, FI.Name), true);
                        });

                        dirInfos.Add($"{dir};{okImages};{grayImages};{tooDarkImages};");
                        var elapsed = sw.ElapsedMilliseconds * directories.Count / i - sw.ElapsedMilliseconds;
                        //string dirInfo = $"Finished {dir}. {i} of {directories.Count} directories processed. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                        string dirInfo = $"Finished {i} of {directories.Count} directories. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                        MainWindow.Log(dirInfo);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Log($"Exception {dir}:\r\n{ex.Message}");
                    }
                });
                File.WriteAllText(dirInfoTXT, string.Join("\r\n ", dirInfos));//write statistics
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating timelapse.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(ex.Message);
            }
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
    }
}
