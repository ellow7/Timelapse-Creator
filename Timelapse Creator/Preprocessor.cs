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
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Timelapse_Creator
{
    public static class Preprocessor
    {
        public static void Preprocess(string SourceFolder, string WorkingFolder, string PreprocessInfoFile, int EveryNthImage = 1, double BrightThreshold = 0.2)
        {
            try
            {
                MainWindow.Log("Preprocessing.");

                if (File.Exists(PreprocessInfoFile))
                    File.Delete(PreprocessInfoFile);

                if (!Directory.Exists(SourceFolder))
                    throw new Exception($"Directory {SourceFolder} does not exist.");
                if (Directory.Exists(WorkingFolder))
                {
                    var res = MessageBox.Show("Do you want to delete the existing working folder?\r\n(No lets you keep your already processed images, existing images will still be overwritten)\r\n" + WorkingFolder, "Delete working folder?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                        Directory.Delete(WorkingFolder, true);
                }
                Directory.CreateDirectory(WorkingFolder);

                MainWindow.Log("Getting files.");
                List<Tuple<string, double, ImageStatus>> PreprocessInfos = new List<Tuple<string, double, ImageStatus>>();//filename, brightness, status


                List<string> files = Directory.GetFiles(SourceFolder, "*.jpg", SearchOption.AllDirectories).OrderBy(R => R).ToList();//list of files we want to process

                //stolen from https://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
                //I seperate the files to smaller parts of files to optimize parallelism
                List<List<string>> fileChunks = files
                     .Select((x, i) => new { Index = i, Value = x })
                     .GroupBy(x => x.Index / 100)
                     .Select(x => x.Select(v => v.Value).ToList())
                     .ToList();

                //progress information
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int j = 0;
                MainWindow.Log($"Calculating brightness and copying of {files.Count} files in {fileChunks.Count} chunks.");

                //iterate the chunks in parallel
                Parallel.ForEach(fileChunks, new ParallelOptions { MaxDegreeOfParallelism = 5 }, fileChunk =>
                {
                    try
                    {
                        List<string> okFiles = new List<string>();//filenames of good images which we want to copy later parallelized
                        foreach(var file in fileChunk)// (int k = 0; k < fileChunk.Count; k++)
                        {
                            try
                            {
                                var img = new Bitmap(file);
                                double bright = 0;
                                bright = CalculateAverageLightness(img);
                                img.Dispose();

                                if (bright > 0.495 && bright < 0.505)//completely gray image (may be a special case from motioneye os)
                                    PreprocessInfos.Add(new Tuple<string, double, ImageStatus>(file, bright, ImageStatus.Gray));
                                else if (bright < BrightThreshold)//too dark image
                                    PreprocessInfos.Add(new Tuple<string, double, ImageStatus>(file, bright, ImageStatus.TooDark));
                                else//ok image
                                {
                                    PreprocessInfos.Add(new Tuple<string, double, ImageStatus>(file, bright, ImageStatus.OK));
                                    okFiles.Add(file);
                                }
                            }
                            catch (Exception ex)
                            {
                                MainWindow.Log($"Exception {file}:\r\n{ex.Message}");
                                PreprocessInfos.Add(new Tuple<string, double, ImageStatus>(file, 0, ImageStatus.Error));
                            }
                        }
                        Parallel.ForEach(MainWindow.EveryNthElement(okFiles, EveryNthImage), file => //only every nth image
                        {
                            string backPath = file.Replace(SourceFolder, "").Trim('/').Trim('\\');//C:/Timelapse/2023-01-01/08-00-00.jpg -> 2023-01-01/08-00-00.jpg
                            string workingFilename = backPath.Replace("/", "_").Replace("\\", "_");//2023-01-01/08-00.jpg -> 2023-01-01_08-00-00.jpg
                            File.Copy(file, Path.Combine(WorkingFolder, workingFilename), true);//C:/Timelapse_working/2023-01-01_08-00-00.jpg
                        });
                        j++;
                        var elapsed = sw.ElapsedMilliseconds * fileChunks.Count / j - sw.ElapsedMilliseconds;
                        string dirInfo = $"Finished {j} of {fileChunks.Count} file chunks. ETA: {String.Format("{0:0.00}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                        MainWindow.Log(dirInfo);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Log($"Exception file chunk index {fileChunks.IndexOf(fileChunk)}:\r\n{ex.Message}");
                    }
                });
                List<string> PreprocessInfoString = new List<string> { "Filename;Brightness;Threshold;Status;" };//statistics of the folders

                foreach(var PreprocessInfo in PreprocessInfos.OrderBy(R => R.Item1))//order by filename
                    PreprocessInfoString.Add($"{PreprocessInfo.Item1};{PreprocessInfo.Item2.ToString("0.000")};{BrightThreshold.ToString("0.000")};{PreprocessInfo.Item3.ToString()};");
                File.WriteAllText(PreprocessInfoFile, string.Join("\r\n ", PreprocessInfoString));//write statistics

                MainWindow.Log("Finished preprocessing.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating timelapse.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(ex.Message);
            }
        }
        /// <summary>
        /// How the image got characterized.
        /// </summary>
        public enum ImageStatus
        {
            OK = 0,
            Gray = 1,
            TooDark = 2,
            Error = 666
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
