using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Drawing;

namespace optimise_dl
{
    internal class Program
    {
        public static string version = "2021-03-28";
        public static string horizontalLine = "****************************************";
        public static string description = "This tool optimises your youtube-dl\ndownloads by merging artwork and audio\nfile. The optimiser also deletes all\ntemporary files so that only the correct\naudio file remains.\n\nFirst argument always contains input files or directories\nFor multiple inputs, use '|' as a seperator";

        public static string[] supportedFormats = new string[]
        {
            "mp3",
            //"wav",
            //"mp4",
            //"flac"
        };

        public static Dictionary<int, string> errors = new Dictionary<int, string>();

        public static void initErrors()
        {
            errors.Add(0, "No Error!");
            //errors.Add(-10, "Directory Not Specified"); // deprecated
            //errors.Add(-11, "Path Not Valid\n->1st Argument must be the target directory or target fiels"); // deprecated
            //errors.Add(-12, "Files Not Specified"); // deprecated (was replacing error -10)
            //errors.Add(-13, "Files Not Valid"); // deprecated (was replacing error -11)
            errors.Add(-14, "Input Not Specified");
            errors.Add(-15, "Input Not Valid");
            errors.Add(-20, "No Optimisable Files Detected");
            errors.Add(-30, "ffmpeg.exe Not Found");
        }

        private static void Main(string[] args)
        {
            initErrors();

            if (args.Contains("-h"))
            {
                displayHelp();
                exitWithError();
            }

            if (args.Length == 0)
            {
                exitWithError(-14);
            }


            /* get Files */
            string[] fileArgs = args[0].Split('|');

            List<FileInfo> files = new List<FileInfo>();

            if (args[0].Length == 0)
            {
                exitWithError(-14);
            }

            foreach (string fileArg in fileArgs)
            {
                if (!File.Exists(fileArg))
                {
                    if (!Directory.Exists(fileArg))
                    {
                        exitWithError(-15);
                    }

                    string[] fs = Directory.GetFiles(fileArg);

                    foreach (string f in fs)
                    {
                        files.Add(new FileInfo(f));
                    }
                }

                files.Add(new FileInfo(fileArg));
            }

            if (files[0].Length == 0)
            {
                exitWithError(-14);
            }

            //else
            Console.WriteLine($"optimise-dl v{version}");
            Console.WriteLine(horizontalLine);
            Console.WriteLine("");
            Console.WriteLine(description);
            Console.WriteLine(horizontalLine);
            Console.WriteLine("");

            int returnVal = 0;

            if (args.Contains("-merge"))
            {
                args[Array.IndexOf(args, "-merge")] = "-merge-all";
            }

            if (args.Contains("-optimise"))
            {
                returnVal = optimise(files.ToArray());
            }

            /* require text=true;cover=true */
            if (args.Contains("-merge-all") || args.Contains("-merge-tag") || args.Contains("-merge-cover"))
            {
                returnVal = merge(files.ToArray(), args.Contains("-merge-tags") || args.Contains("-merge-all"), args.Contains("-merge-cover") || args.Contains("-merge-all"));
            }

            if (args.Contains("-acrop"))
            {
                returnVal = crop(files.ToArray());
            }

            /* require text=true;cover=true */
            if (args.Contains("-extract"))
            {
                returnVal = extract(files.ToArray());
            }

            exitWithError(returnVal);
        }

        private static void displayHelp()
        {
            Console.WriteLine(horizontalLine);
            Console.WriteLine("");
            Console.WriteLine("-merge, -merge-all\tmerges thumbnail (.webp or .jpg), metadata / tags (.txt) and song file together. Files must have\n\t\t\tthe same name");
            Console.WriteLine("-merge-tags\t\tmerges metadata / tags (.txt) and song file together. Files must have the same name");
            Console.WriteLine("-merge-cover\t\tmerges thumbnail (.webp or .jpg) and song file together. Files must have the same name");
            Console.WriteLine("-acrop\t\t\tautomatically crops your thumbnail to a square");
            Console.WriteLine("-extract\t\textracts thumbnail (.jpg) and metadata (.txt) and saves them as files");
            Console.WriteLine("");
            Console.WriteLine(horizontalLine);
            Console.WriteLine("");
            Console.WriteLine("Errors:");
            Console.WriteLine("");
            foreach (KeyValuePair<int, string> error in errors)
            {
                Console.WriteLine($"{error.Value} ({error.Key})\n");
            }

            Console.WriteLine(horizontalLine);
            Console.WriteLine("");

            Console.ReadKey();
        }

        /* works */
        private static int extract(FileInfo[] files)
        {
            List<FileInfo> todo = new List<FileInfo>();

            foreach (FileInfo file in files)
            {
                foreach (string supportedFormat in supportedFormats)
                {
                    if (file.FullName.Contains($".{supportedFormat}"))
                    {
                        todo.Add(file);
                    }
                }
            }

            if (todo.Count == 0)
            {
                return (-20);
            }

            Console.WriteLine("");
            Console.WriteLine("the following files are valid and\nwill be extracted from");
            Console.WriteLine("");

            foreach (FileInfo file in todo)
            {
                Console.WriteLine($"{todo.IndexOf(file) + 1}: {file.Name.Replace(".temp", "")}");
            }

            Console.WriteLine(horizontalLine);
            Console.WriteLine("");

            Thread.Sleep(1000);

            if (!File.Exists(AppContext.BaseDirectory + "ffmpeg.exe"))
            {
                exitWithError(-30);
            }

            string workingPictureFormat = "jpg";

            foreach (FileInfo file in todo)
            {
                string[] t = file.FullName.Split('.');

                string baseFile = file.FullName.Remove(file.FullName.Length - (t[t.Length - 1].Length + 1));

                /* extract thumbnail */

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "ffmpeg.exe";

                startInfo.Arguments = $"-i \"{file.FullName}\" \"{baseFile}.{workingPictureFormat}\" -f ffmetadata \"{baseFile}.txt\"";

                Process test = Process.Start(startInfo);

                Console.WriteLine($"\nextracting thumbnail of file #{todo.IndexOf(file) + 1}...");

                while (!test.HasExited)
                {
                    ;
                }
            }
            return 0;
        }

        /* works */
        private static int crop(FileInfo[] files)
        {
            int height, width, x_offset, y_offset, width_new;

            // 16 : 9

            /*
            // THIS VALS SHOULD BE SET
            height = 720;
            width = 1280;

            // THIS VALS SHOULD NOT BE SET
            width_new = height;
            y_offset = 0;
            x_offset = (width - height) / 2;*/


            List<FileInfo> todo = new List<FileInfo>();

            foreach (FileInfo file in files)
            {
                foreach (string supportedFormat in supportedFormats)
                {
                    if (file.FullName.Contains($".{supportedFormat}"))
                    {
                        todo.Add(file);
                    }
                }
            }

            if (todo.Count == 0)
            {
                return (-20);
            }

            Console.WriteLine("");
            Console.WriteLine("the following files are optimisable and\nwill be optimised");
            Console.WriteLine("");

            foreach (FileInfo file in todo)
            {
                Console.WriteLine($"{todo.IndexOf(file) + 1}: {file.Name.Replace(".temp", "")}");
            }

            Console.WriteLine(horizontalLine);
            Console.WriteLine("");

            Thread.Sleep(1000);

            if (!File.Exists(AppContext.BaseDirectory + "ffmpeg.exe"))
            {
                exitWithError(-30);
            }

            string workingPictureFormat = "jpg";

            foreach (FileInfo file in todo)
            {
                string[] t = file.FullName.Split('.');

                string baseFile = file.FullName.Remove(file.FullName.Length - (t[t.Length - 1].Length + 1));

                /* extract thumbnail */

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "ffmpeg.exe";

                startInfo.Arguments = $"-i \"{file.FullName}\" \"thumbnail.old.{workingPictureFormat}\"";

                Process test = Process.Start(startInfo);

                Console.WriteLine($"\nextracting thumbnail of file #{todo.IndexOf(file) + 1}...");

                while (!test.HasExited)
                {
                    ;
                }

                using (var img = new Bitmap("thumbnail.old." + workingPictureFormat))
                {

                    // THIS VALS SHOULD BE SET
                    height = img.Height;
                    width = img.Width;

                    // THIS VALS SHOULD NOT BE SET
                    width_new = height;
                    y_offset = 0;
                    x_offset = (width - height) / 2;
                }

                /* crop thumbnail */
                startInfo.Arguments = $"-i \"thumbnail.old.{workingPictureFormat}\" -filter:v \"crop={width_new}:{height}:{x_offset}:{y_offset}\" \"thumbnail.new.{workingPictureFormat}\"";

                test = Process.Start(startInfo);

                Console.WriteLine($"\ncropping thumbnail of file #{todo.IndexOf(file) + 1}...");

                while (!test.HasExited)
                {
                    ;
                }

                /* merge */
                startInfo.Arguments = $"-i \"{file.FullName}\" -i \"thumbnail.new.{workingPictureFormat}\" -map 0:0 -map 1:0 -c copy -id3v2_version 3 -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover (front)\" \"{baseFile}.temp.mp3\"";

                test = Process.Start(startInfo);

                Console.WriteLine($"\nremerging thumbnail of file #{todo.IndexOf(file) + 1}...");

                while (!test.HasExited)
                {
                    ;
                }

                Console.WriteLine($"\ncleaning up #{todo.IndexOf(file) + 1}...");

                File.Delete($"thumbnail.old.{workingPictureFormat}");
                File.Delete($"thumbnail.new.{workingPictureFormat}");

                if (File.Exists($"{baseFile}.temp.mp3"))
                {
                    File.Copy($"{baseFile}.temp.mp3", $"{baseFile}.mp3", true);
                    File.Delete($"{baseFile}.temp.mp3");
                }
            }

            return 0;
        }

        /* works */
        private static int optimise(FileInfo[] files)
        {
            Console.WriteLine(horizontalLine);
            Console.WriteLine($"Optimising Files:\n\nMerge Tags:\tFalse\nMerge Cover:\tTrue\nDelete Temp Files:\tTrue");
            Console.WriteLine(horizontalLine);
            Thread.Sleep(1000);

            List<FileInfo> todo = new List<FileInfo>();

            foreach (FileInfo file in files)
            {
                foreach (string supportedFormat in supportedFormats)
                {
                    if (file.FullName.Contains($".{supportedFormat}"))
                    {
                        todo.Add(file);
                    }
                }
            }

            if (todo.Count == 0)
            {
                return (-20);
            }

            Console.WriteLine("");
            Console.WriteLine("the following files files are valid:");
            Console.WriteLine("");

            foreach (FileInfo file in todo)
            {
                Console.WriteLine($"{todo.IndexOf(file) + 1}: {file.Name}");
            }

            Thread.Sleep(1000);

            if (!File.Exists(AppContext.BaseDirectory + "ffmpeg.exe"))
            {
                exitWithError(-30);
            }

            int r = merge(files, false, true);

            foreach (FileInfo file in todo)
            {
                string baseFile = Path.Combine(Path.GetDirectoryName(file.FullName), Path.GetFileNameWithoutExtension(file.FullName));

                if (File.Exists(baseFile + ".temp" + file.Extension))
                {
                    File.Delete(baseFile + ".temp" + file.Extension);
                }

                if (File.Exists(baseFile + ".opt" + file.Extension))
                {
                    File.Delete(baseFile + ".opt" + file.Extension);
                }

                if (File.Exists(baseFile + ".txt"))
                {
                    File.Delete(baseFile + ".txt");
                }

                if (File.Exists(baseFile + ".jpg"))
                {
                    File.Delete(baseFile + ".jpg");
                }

                if (File.Exists(baseFile + ".webp"))
                {
                    File.Delete(baseFile + ".webp");
                }

                if (File.Exists(baseFile + ".png"))
                {
                    File.Delete(baseFile + ".png");
                }

                Console.WriteLine("Delete " + baseFile + ".*");
            }

            return r;
        }

        /* works */
        private static int merge(FileInfo[] files, bool text, bool cover)
        {
            Console.WriteLine(horizontalLine);
            Console.WriteLine($"Merge Files:\n\nMerge Tags:\t{text.ToString()}\nMerge Cover:\t{cover.ToString()}");
            Console.WriteLine(horizontalLine);
            Thread.Sleep(1000);

            List<FileInfo> todo = new List<FileInfo>();

            foreach (FileInfo file in files)
            {
                foreach (string supportedFormat in supportedFormats)
                {
                    if (file.FullName.Contains($".{supportedFormat}"))
                    {
                        todo.Add(file);
                    }
                }
            }

            if (todo.Count == 0)
            {
                return (-20);
            }

            Console.WriteLine("");
            Console.WriteLine("the following files files are valid:");
            Console.WriteLine("");

            foreach (FileInfo file in todo)
            {
                Console.WriteLine($"{todo.IndexOf(file) + 1}: {file.Name.Replace(".temp", "")}");
            }

            Thread.Sleep(1000);

            if (!File.Exists(AppContext.BaseDirectory + "ffmpeg.exe"))
            {
                exitWithError(-30);
            }

            foreach (FileInfo file in todo)
            {
                string baseFile = Path.Combine(Path.GetDirectoryName(file.FullName), Path.GetFileNameWithoutExtension(file.FullName));

                /* merge */
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "ffmpeg.exe";

                if (cover)
                {
                    Console.WriteLine("\n" + horizontalLine);
                    Console.WriteLine("Merge Cover");
                    Console.WriteLine(horizontalLine);

                    string fileInput2 = $"{baseFile}.jpg";

                    if (!File.Exists(fileInput2))
                    {
                        Console.WriteLine($"\"{fileInput2}\" not found - using '.webp' instead of '.jpg'");

                        startInfo.Arguments = $"-i \"{baseFile}.webp\" \"{fileInput2}\"";

                        Process t = Process.Start(startInfo);

                        Console.WriteLine($"\nconverting covers #{todo.IndexOf(file) + 1}...");

                        while (!t.HasExited)
                        {
                            ;
                        }
                    }

                    startInfo.Arguments = $"-i \"{baseFile}.mp3\" -i \"{fileInput2}\" -map 0:0 -map 1:0 -c copy -id3v2_version 3 -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover(front)\" \"{baseFile}.opt.mp3\"";

                    Process test = Process.Start(startInfo);

                    Console.WriteLine($"\nmerging files #{todo.IndexOf(file) + 1}...");

                    while (!test.HasExited)
                    {
                        ;
                    }

                    Console.WriteLine($"\nsaving output file #{todo.IndexOf(file) + 1}...");

                    try
                    {
                        File.Copy($"{baseFile}.opt.mp3", $"{baseFile}.mp3", true);
                        File.Delete($"{baseFile}.opt.mp3");
                    }
                    catch
                    {
                        Console.WriteLine($"[ERR]: \"{baseFile}\"could not be merged");
                    }

                    Console.WriteLine($"\nCompleted file #{todo.IndexOf(file) + 1}...");
                }

                if (text)
                {
                    Console.WriteLine("\n" + horizontalLine);
                    Console.WriteLine("Merge Tags");
                    Console.WriteLine(horizontalLine);

                    startInfo.Arguments = $"-i \"{baseFile}.mp3\" -i \"{baseFile}.txt\" -map_metadata 1 -c:a copy -id3v2_version 3 \"{baseFile}.opt.mp3\"";

                    Process test = Process.Start(startInfo);

                    Console.WriteLine($"\nmerging files #{todo.IndexOf(file) + 1}...");

                    while (!test.HasExited)
                    {
                        ;
                    }

                    Console.WriteLine($"\nsaving output file #{todo.IndexOf(file) + 1}...");

                    File.Copy($"{baseFile}.opt.mp3", $"{baseFile}.mp3", true);
                    File.Delete($"{baseFile}.opt.mp3");

                    Console.WriteLine($"\nCompleted file #{todo.IndexOf(file) + 1}...");
                }
            }

            return 0;
        }

        public static void exitWithError(int exitCode = 0)
        {
            Console.WriteLine($"Error: {errors[exitCode]} ({exitCode})\n");
            Thread.Sleep(5000);
            Environment.Exit(exitCode);
        }
    }
}
