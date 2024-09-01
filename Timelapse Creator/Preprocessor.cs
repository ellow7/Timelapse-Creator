using MetadataExtractor.Formats.Exif;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Timelapse_Creator
{
    public static class Preprocessor
    {
        public static void PreprocessCount(string SourceFolder, string WorkingFolder, string PreprocessInfoFile, int EveryNthImage = 1, double BrightThreshold = 0.2)
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
                        if (BrightThreshold == 0)//skip if we ignore the brightness setting
                        {
                            okFiles.AddRange(fileChunk);
                        }
                        else
                        {
                            foreach (var file in fileChunk)// (int k = 0; k < fileChunk.Count; k++)
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
                        }
                        Parallel.ForEach(MainWindow.EveryNthElement(okFiles, EveryNthImage), file => //only every nth image
                        {
                            string backPath = file.Replace(SourceFolder, "").Trim('/').Trim('\\');//C:/Timelapse/2023-01-01/08-00-00.jpg -> 2023-01-01/08-00-00.jpg
                            string workingFilename = backPath.Replace("/", "_").Replace("\\", "_");//2023-01-01/08-00.jpg -> 2023-01-01_08-00-00.jpg
                            File.Copy(file, Path.Combine(WorkingFolder, workingFilename), true);//C:/Timelapse_working/2023-01-01_08-00-00.jpg
                        });
                        j++;
                        var elapsed = sw.ElapsedMilliseconds * fileChunks.Count / j - sw.ElapsedMilliseconds;
                        string dirInfo = $"Finished {j} of {fileChunks.Count} file chunks. ETA: {String.Format("{0:0}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                        MainWindow.Log(dirInfo);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Log($"Exception file chunk index {fileChunks.IndexOf(fileChunk)}:\r\n{ex.Message}");
                    }
                });
                List<string> PreprocessInfoString = new List<string> { "Filename;Brightness;Threshold;Status;" };//statistics of the folders

                foreach (var PreprocessInfo in PreprocessInfos.OrderBy(R => R.Item1))//order by filename
                    PreprocessInfoString.Add($"{PreprocessInfo.Item1};{PreprocessInfo.Item2.ToString("0.000")};{BrightThreshold.ToString("0.000")};{PreprocessInfo.Item3.ToString()};");
                File.WriteAllText(PreprocessInfoFile, string.Join("\r\n ", PreprocessInfoString));//write statistics

                MainWindow.Log("Finished preprocessing.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preprocessing.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(ex.Message);
            }
        }

        public static void PreprocessTime(string SourceFolder, string WorkingFolder, bool PreprocessTimestampFromFormat, string PreprocessTimestampFormat, string PreprocessTimes)
        {
            try
            {
                MainWindow.Log("Preprocessing.");

                if (!Directory.Exists(SourceFolder))
                    throw new Exception($"Directory {SourceFolder} does not exist.");

                if (Directory.Exists(WorkingFolder))
                {
                    var res = MessageBox.Show("Do you want to delete the existing working folder?\r\n(No lets you keep your already processed images, existing images will still be overwritten)\r\n" + WorkingFolder, "Delete working folder?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                        Directory.Delete(WorkingFolder, true);
                }
                Directory.CreateDirectory(WorkingFolder);

                #region Manage preprocess Times
                List<Tuple<int, int>> preprocessTimes = new List<Tuple<int, int>>();//hours, minutes
                foreach (string time in PreprocessTimes.Split(';'))
                {
                    // Parse the time string into a DateTime object
                    DateTime parsedTime;
                    if (DateTime.TryParseExact(time, "HH:mm", null, DateTimeStyles.None, out parsedTime))
                    {
                        int hours = parsedTime.Hour;
                        int minutes = parsedTime.Minute;
                        Console.WriteLine($"Time: {time}, Hours: {hours}, Minutes: {minutes}");
                        preprocessTimes.Add(new Tuple<int, int>(hours, minutes));
                    }
                    else
                    {
                        Console.WriteLine($"Invalid time format: {time}");
                    }
                }
                if (preprocessTimes.Count == 0)
                    throw new Exception($"Could not parse Preprocess Times {PreprocessTimes} correctly.");
                #endregion

                MainWindow.Log("Getting files.");
                List<string> files = Directory.GetFiles(SourceFolder, "*.jpg", SearchOption.AllDirectories).OrderBy(R => R).ToList();//list of files we want to process
                files = files.OrderBy(R => R).ToList();

                MainWindow.Log($"Filtering images by time and copying of {files.Count} files.");

                List<Tuple<string, DateTime>> fileDateTimes = new List<Tuple<string, DateTime>>();//filename, datetime
                Regex r = new Regex(":");

                //progress information
                Stopwatch sw = new Stopwatch();
                sw.Start();
                #region Parse datetimes from filenames
                int i = 0;
                foreach (var file in files)
                {
                    try
                    {
                        DateTime parsedDate;
                        if (PreprocessTimestampFromFormat)
                        {//get timestamp from filename
                            string filename = Path.GetFileNameWithoutExtension(file);
                            bool success = DateTime.TryParseExact(filename, PreprocessTimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
                            if (!success)
                            {
                                MainWindow.Log($"Could not parse datetime of {file}");
                            }
                            else
                            {
                                fileDateTimes.Add(new Tuple<string, DateTime>(file, parsedDate));

                                i++;
                                if (i % 1000 == 0)
                                {
                                    var elapsed = sw.ElapsedMilliseconds * files.Count / i - sw.ElapsedMilliseconds;
                                    string dirInfo = $"[Step 1 of 2] Getting times from {i} of {files.Count} files. ETA: {String.Format("{0:0}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                                    MainWindow.Log(dirInfo);
                                }
                            }
                        }
                        else
                        {//get timestamp from file property
                            fileDateTimes.Add(new Tuple<string, DateTime>(file, GetDateTaken(file)));

                            i++;
                            if (i % 1000 == 0)
                            {
                                var elapsed = sw.ElapsedMilliseconds * files.Count / i - sw.ElapsedMilliseconds;
                                string dirInfo = $"[Step 1 of 2] Getting times from {i} of {files.Count} files. ETA: {String.Format("{0:0}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                                MainWindow.Log(dirInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Log($"Exception {file}:\r\n{ex.Message}");
                    }
                }
                #endregion

                #region Get correct files by time of day
                MainWindow.Log("Filtering by time of day");
                //get min and max days
                var dateTimes = fileDateTimes.Select(R => R.Item2);
                DateTime minDT = dateTimes.Min();
                DateTime maxDT = dateTimes.Max();

                List<Tuple<string, DateTime>> filesToCopy = new List<Tuple<string, DateTime>>();

                //iterate single days
                for (DateTime date = minDT; date <= maxDT; date = date.AddDays(1))
                {
                    //iterate search times from preprocessTimes, e.g. 09:00;15:00
                    foreach (var time in preprocessTimes)
                    {
                        DateTime seachTime = new DateTime(date.Year, date.Month, date.Day, time.Item1, time.Item2, 0);
                        var found = fileDateTimes.FirstOrDefault(R => R.Item2 > seachTime);
                        if (found != null)
                            filesToCopy.Add(found);
                    }

                    Console.WriteLine(date.ToString("yyyy-MM-dd"));
                }
                #endregion

                #region Copy found files
                //progress information
                sw.Restart();
                i = 0;
                MainWindow.Log($"Copying {filesToCopy.Count} files.");
                foreach (var file in filesToCopy)
                {
                    //string backPath = file.Replace(SourceFolder, "").Trim('/').Trim('\\');//C:/Timelapse/2023-01-01/08-00-00.jpg -> 2023-01-01/08-00-00.jpg
                    //string workingFilename = backPath.Replace("/", "_").Replace("\\", "_");//2023-01-01/08-00.jpg -> 2023-01-01_08-00-00.jpg
                    //File.Copy(file, Path.Combine(WorkingFolder, workingFilename), true);//C:/Timelapse_working/2023-01-01_08-00-00.jpg

                    File.Copy(file.Item1, Path.Combine(WorkingFolder, file.Item2.ToString("yyyy-MM-dd.HH-mm-ss") + ".jpg"), true);//C:/Timelapse_working/2023-01-01.08-00-00.jpg

                    i++;
                    if (i % 100 == 0)
                    {
                        var elapsed = sw.ElapsedMilliseconds * filesToCopy.Count / i - sw.ElapsedMilliseconds;
                        string dirInfo = $"[Step 2 of 2] Finished {i} of {filesToCopy.Count} files. ETA: {String.Format("{0:0}", elapsed / 1000 / 60.0, 2)} min";//estimate duration
                        MainWindow.Log(dirInfo);
                    }
                }
                #endregion

                MainWindow.Log("Finished preprocessing.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preprocessing.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Log(ex.Message);
            }
        }
        public static DateTime GetDateTaken(string file)
        {
            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(file);
            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory != null && MetadataExtractor.DirectoryExtensions.TryGetDateTime(subIfdDirectory, ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
            {
                return dateTaken;
            }
            throw new Exception("Date taken not found");

            //using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            //using (Image myImage = Image.FromStream(fs, false, false))
            //{
            //    //var test = myImage.PropertyItems;
            //    PropertyItem propItem = myImage.GetPropertyItem(36867);//306 should also work?
            //    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
            //    parsedDate = DateTime.Parse(dateTaken);
            //    fileDateTimes.Add(new Tuple<string, DateTime>(file, parsedDate));
            //}
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
