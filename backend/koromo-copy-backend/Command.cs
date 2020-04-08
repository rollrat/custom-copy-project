// This source code is a part of DCInside Archive Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.CL;
using koromo_copy_backend.Component.Hitomi;
using koromo_copy_backend.Extractor;
using koromo_copy_backend.Log;
using koromo_copy_backend.Network;
using koromo_copy_backend.Setting;
using koromo_copy_backend.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace koromo_copy_backend
{
    public class Options : IConsoleOption
    {
        [CommandLine("--help", CommandType.OPTION)]
        public bool Help;
        [CommandLine("--version", CommandType.OPTION, ShortOption = "-v", Info = "Show version information.")]
        public bool Version;

        /// <summary>
        /// Atomic Options
        /// </summary>

        [CommandLine("--recover-settings", CommandType.OPTION, Info = "Recover settings.json")]
        public bool RecoverSettings;

        /// <summary>
        /// Build require datas.
        /// </summary>
        
        [CommandLine("--build-data", CommandType.OPTION, Info = "Build require datas.")]
        public bool BuildData;

        /// <summary>
        /// User Option
        /// </summary>
        
        [CommandLine("--starts-with-client", CommandType.OPTION, Default = true,
            Info = "Starts Koromo Copy server with client.", Help = "use --starts-with-client")]
        public bool StartsWithClient;

        /// <summary>
        /// Extractor Options
        /// </summary>

        [CommandLine("--list-extractor", CommandType.OPTION, Info = "Enumerate all implemented extractor.")]
        public bool ListExtractor;

        [CommandLine("--url", CommandType.ARGUMENTS, ArgumentsCount = 1,
            Info = "Set extracting target.", Help = "use --url <URL>")]
        public string[] Url;
        [CommandLine("--path-format", CommandType.ARGUMENTS, ShortOption = "-o", ArgumentsCount = 1,
            Info = "Set extracting file name format.", Help = "use -o <Output Format>")]
        public string[] PathFormat;

        [CommandLine("--extract-info", CommandType.OPTION, ShortOption = "-i", Info = "Extract information of url.", Help = "use -i")]
        public bool ExtractInformation;
        [CommandLine("--extract-link", CommandType.OPTION, ShortOption = "-l", Info = "Extract just links.", Help = "use -l")]
        public bool ExtractLinks;
        [CommandLine("--print-process", CommandType.OPTION, ShortOption = "-p", Info = "Print download processing.", Help = "use -p")]
        public bool PrintProcess;

        [CommandLine("--disable-download-progress", CommandType.OPTION, Info = "Disable download progress.", Help = "use --disable-download-progress")]
        public bool DisableDownloadProgress;

        [CommandLine("--page-start", CommandType.OPTION, Info = "Specify a start page when crawling a multi-page bulletin board.", Help = "use --page-start <Number>")]
        public string[] PageStart;
        [CommandLine("--page-end", CommandType.OPTION, Info = "Specify a end page when crawling a multi-page bulletin board.", Help = "use --page-end <Number>")]
        public string[] PageEnd;

    }

    public class Command
    {
        public static void Start(string[] arguments)
        {
            arguments = CommandLineUtil.SplitCombinedOptions(arguments);
            arguments = CommandLineUtil.InsertWeirdArguments<Options>(arguments, true, "--url");
            var option = CommandLineParser.Parse<Options>(arguments);

            //
            //  Single Commands
            //
            if (option.Help)
            {
                PrintHelp();
            }
            else if (option.Version)
            {
                PrintVersion();
            }
            else if (option.RecoverSettings)
            {
                Settings.Instance.Recover();
                Settings.Instance.Save();
            }
            else if (option.BuildData)
            {
                ProcessBuildData();
            }
            else if (option.StartsWithClient)
            {
                ProcessStartsWithClient();
            }
            else if (option.ListExtractor)
            {
                foreach (var extractor in ExtractorManager.Extractors)
                {
                    Console.WriteLine($"[{extractor.GetType().Name}]");
                    Console.WriteLine($"[HostName] {extractor.HostName}");
                    Console.WriteLine($"[Checker] {extractor.ValidUrl}");
                    Console.WriteLine($"[Information] {extractor.ExtractorInfo}");
                    var builder = new StringBuilder();
                    CommandLineParser.GetFields(extractor.RecommendOption("").GetType()).ToList().ForEach(
                        x =>
                        {
                            var key = x.Key;
                            if (!key.StartsWith("--"))
                                return;
                            if (!string.IsNullOrEmpty(x.Value.Item2.ShortOption))
                                key = $"{x.Value.Item2.ShortOption}, " + key;
                            var help = "";
                            if (!string.IsNullOrEmpty(x.Value.Item2.Help))
                                help = $"[{x.Value.Item2.Help}]";
                            if (!string.IsNullOrEmpty(x.Value.Item2.Info))
                                builder.Append($"   {key}".PadRight(30) + $" {x.Value.Item2.Info} {help}\r\n");
                            else
                                builder.Append($"   {key}".PadRight(30) + $" {help}\r\n");
                        });
                    if (builder.ToString() != "")
                    {
                        Console.WriteLine($"[Options]");
                        Console.Write(builder.ToString());
                    }
                    Console.WriteLine($"-------------------------------------------------------------");
                }
            }
            else if (option.Url != null)
            {
                if (!(option.Url[0].StartsWith("https://") || option.Url[0].StartsWith("http://")))
                {
                    Console.WriteLine($"'{option.Url[0]}' is not correct url format or not supported scheme.");
                }

                var weird = CommandLineUtil.GetWeirdArguments<Options>(arguments);
                var n_args = new List<string>();

                weird.ForEach(x => n_args.Add(arguments[x]));

                ProcessExtract(option.Url[0], n_args.ToArray(), option.PathFormat, option.ExtractInformation, option.ExtractLinks, option.PrintProcess, option.DisableDownloadProgress);
            }
            else if (option.Error)
            {
                Console.WriteLine(option.ErrorMessage);
                if (option.HelpMessage != null)
                    Console.WriteLine(option.HelpMessage);
                return;
            }
            else
            {
                Console.WriteLine("Nothing to work on.");
                Console.WriteLine("Enter './koromo-copy-backend --help' to get more information");
            }

            return;
        }

        static byte[] art_console = {
            0x8D, 0x54, 0x3D, 0x6F, 0xDB, 0x30, 0x10, 0xDD, 0x0D, 0xF8, 0x3F, 0x5C, 0xB8, 0x48, 0x01, 0x74, 0xE4, 0x18, 0xC0, 0xB0, 0xBC,
            0x0B, 0x59, 0xB3, 0x18, 0x50, 0x45, 0x23, 0xD9, 0x0A, 0x01, 0xDD, 0x3A, 0x34, 0x2E, 0x7F, 0x7B, 0xDF, 0xBB, 0x93, 0x14, 0xA7,
            0x92, 0xDB, 0xD0, 0x24, 0x41, 0x1F, 0xDF, 0xDD, 0xBD, 0xFB, 0xA0, 0x44, 0xBE, 0x38, 0xD2, 0x8B, 0xA4, 0x6E, 0xBF, 0xFB, 0x0F,
            0x48, 0xAE, 0x98, 0x12, 0xB5, 0xA4, 0x7F, 0x41, 0x5F, 0x7A, 0x39, 0x8B, 0x74, 0x42, 0x34, 0x74, 0x24, 0xDF, 0x80, 0xE1, 0xE7,
            0xF3, 0xB8, 0x4A, 0x4F, 0xA4, 0xE1, 0xCF, 0x9F, 0x2D, 0x77, 0x32, 0x52, 0xA3, 0xFB, 0x30, 0x0B, 0x18, 0x26, 0xA4, 0xD8, 0x61,
            0x68, 0xC6, 0xFA, 0x0D, 0xBD, 0x8E, 0x93, 0x07, 0xB7, 0x4A, 0xF5, 0x5E, 0x2E, 0x3C, 0x9C, 0x01, 0xCD, 0xD9, 0x2E, 0x4C, 0x8A,
            0x0D, 0x88, 0x11, 0xB2, 0x71, 0xC6, 0x09, 0x91, 0x39, 0xCA, 0x15, 0xD0, 0x5E, 0x8A, 0x42, 0x7A, 0x31, 0x29, 0x36, 0x4E, 0xC8,
            0xFC, 0x24, 0x97, 0xC8, 0x1C, 0xD0, 0x0D, 0x09, 0x50, 0x52, 0x34, 0x4A, 0xC0, 0xA2, 0x09, 0xFC, 0x1F, 0x62, 0xC6, 0x72, 0x49,
            0x72, 0x04, 0xA0, 0x51, 0x15, 0x38, 0x90, 0x28, 0xEA, 0xBE, 0x78, 0xCA, 0x51, 0x01, 0x0B, 0x02, 0x79, 0xC2, 0xE2, 0x89, 0x61,
            0x9D, 0x94, 0xBA, 0x34, 0x2B, 0xBC, 0x92, 0x72, 0xD2, 0xC0, 0x50, 0x03, 0x68, 0x88, 0x3C, 0x4D, 0xEB, 0xDB, 0x7E, 0x07, 0x57,
            0x39, 0x97, 0x00, 0x38, 0x50, 0xD4, 0x78, 0x17, 0x23, 0x47, 0x2E, 0xCC, 0x48, 0x24, 0x01, 0x67, 0x7A, 0x44, 0x02, 0x80, 0xA4,
            0xDD, 0xB9, 0x1A, 0x99, 0x97, 0xB4, 0xC8, 0x74, 0xA1, 0x68, 0x72, 0xF0, 0x98, 0x64, 0x80, 0x3D, 0x33, 0x38, 0x8D, 0x52, 0x2F,
            0xD0, 0x53, 0xCC, 0x07, 0x4B, 0xF1, 0x08, 0xCF, 0x18, 0x4B, 0xC1, 0x06, 0x90, 0x68, 0x20, 0x80, 0xFB, 0x30, 0xDB, 0x7E, 0x00,
            0x0D, 0x8D, 0xE4, 0x37, 0x22, 0x40, 0x37, 0x05, 0x0A, 0x7F, 0xB7, 0x0F, 0xAD, 0x93, 0x57, 0x4D, 0x52, 0x95, 0x89, 0x02, 0x95,
            0x1A, 0x72, 0xD2, 0xF6, 0x15, 0xA4, 0xF3, 0xE3, 0xAA, 0xE7, 0x26, 0x2D, 0x90, 0x22, 0xA1, 0xA5, 0xC5, 0xAC, 0x9E, 0x18, 0x6F,
            0xA1, 0xFC, 0x90, 0x7E, 0xDD, 0xA9, 0xBD, 0x13, 0x41, 0x15, 0x99, 0x5C, 0x13, 0xC5, 0x81, 0x72, 0x63, 0x5E, 0x2C, 0xF3, 0x6B,
            0x67, 0x8B, 0x3B, 0x96, 0xCE, 0x23, 0xF1, 0xDD, 0x82, 0xB4, 0xF1, 0xB8, 0xF9, 0x2C, 0x12, 0x7E, 0x68, 0x2B, 0x91, 0xCA, 0x8A,
            0x59, 0x4D, 0x5C, 0x67, 0x65, 0xA9, 0xB6, 0x94, 0x2C, 0xDF, 0x71, 0x86, 0x65, 0x73, 0x1A, 0xF5, 0x78, 0xFB, 0x94, 0x6E, 0x5D,
            0x18, 0xB8, 0x62, 0xE1, 0x83, 0xC5, 0x95, 0xAC, 0x63, 0x3F, 0x00, 0xCD, 0xAF, 0x76, 0x95, 0xF3, 0xD9, 0x91, 0xB9, 0x40, 0xCE,
            0xBD, 0xA8, 0xCF, 0xD8, 0x51, 0x20, 0x36, 0xAA, 0x8D, 0x74, 0xF7, 0xA9, 0x07, 0x6D, 0x2C, 0x79, 0xCC, 0x76, 0x07, 0x87, 0xD6,
            0x2E, 0x39, 0xBF, 0xAB, 0x2A, 0x5A, 0xA4, 0x6E, 0xEF, 0x79, 0x24, 0xDF, 0xC4, 0x42, 0x1B, 0xC3, 0xA3, 0x91, 0x40, 0xB1, 0xA7,
            0x8B, 0x7B, 0x3A, 0xE8, 0x8A, 0xE4, 0x01, 0x2D, 0x91, 0x35, 0x3F, 0x5B, 0x10, 0xA8, 0xEB, 0x6D, 0x95, 0x88, 0x07, 0x88, 0xD4,
            0x3B, 0x14, 0xD6, 0x7F, 0xA3, 0x9F, 0x53, 0x6A, 0xDB, 0x96, 0x8F, 0x6F, 0x53, 0x85, 0x85, 0xAA, 0x98, 0x09, 0xC4, 0x8F, 0x3E,
            0x16, 0x46, 0x82, 0x30, 0x74, 0x03, 0x8C, 0x7E, 0xF1, 0x2E, 0xB5, 0xB4, 0xE1, 0x8B, 0x63, 0xFC, 0xC7, 0x71, 0x0D, 0xB5, 0x2A,
            0xDA, 0xC4, 0xD3, 0x92, 0xC3, 0xDC, 0x2A, 0xF8, 0x9C, 0x6C, 0xB6, 0xB3, 0x15, 0xE3, 0x8A, 0xDF, 0x77, 0x7F, 0xEF, 0x3E, 0xCA,
            0xB0, 0xC1, 0xA1, 0xA0, 0x1D, 0xEA, 0x1C, 0x07, 0xD4, 0x7C, 0xBF, 0xFB, 0x03,
        };

        static void PrintHelp()
        {
            PrintVersion();
            Console.WriteLine(Encoding.UTF8.GetString(CompressUtils.Decompress(art_console)));
            Console.WriteLine($"Copyright (C) 2020. Koromo Copy Project.");
            Console.WriteLine($"E-Mail: rollrat.cse@gmail.com");
            Console.WriteLine($"Source-code: https://github.com/rollrat/koromo-copy-project");
            Console.WriteLine($"");
            Console.WriteLine("Usage: ./koromo-copy-backend [OPTIONS...]");

            var builder = new StringBuilder();
            CommandLineParser.GetFields(typeof(Options)).ToList().ForEach(
                x =>
                {
                    var key = x.Key;
                    if (!key.StartsWith("--"))
                        return;
                    if (!string.IsNullOrEmpty(x.Value.Item2.ShortOption))
                        key = $"{x.Value.Item2.ShortOption}, " + key;
                    var help = "";
                    if (!string.IsNullOrEmpty(x.Value.Item2.Help))
                        help = $"[{x.Value.Item2.Help}]";
                    if (!string.IsNullOrEmpty(x.Value.Item2.Info))
                        builder.Append($"   {key}".PadRight(30) + $" {x.Value.Item2.Info} {help}\r\n");
                    else
                        builder.Append($"   {key}".PadRight(30) + $" {help}\r\n");
                });
            Console.Write(builder.ToString());
        }

        public static void PrintVersion()
        {
            Console.WriteLine($"{Version.Name} {Version.Text}");
            Console.WriteLine($"Build Date: " + Internals.GetBuildDate().ToLongDateString());
        }

        static void ProcessBuildData()
        {

        }

        static void ProcessStartsWithClient()
        {
            Console.Clear();
            Console.Title = "Koromo Copy Server";

            Console.WriteLine(@" _  __                                                _____                         ");
            Console.WriteLine(@"| |/ /                                               / ____|                        ");
            Console.WriteLine(@"| ' /    ___    _ __    ___    _ __ ___     ___     | |        ___    _ __    _   _ ");
            Console.WriteLine(@"|  <    / _ \  | '__|  / _ \  | '_ ` _ \   / _ \    | |       / _ \  | '_ \  | | | |");
            Console.WriteLine(@"| . \  | (_) | | |    | (_) | | | | | | | | (_) |   | |____  | (_) | | |_) | | |_| |");
            Console.WriteLine(@"|_|\_\  \___/  |_|     \___/  |_| |_| |_|  \___/     \_____|  \___/  | .__/   \__, |");
            Console.WriteLine(@"                                                                     | |       __/ |");
            Console.WriteLine(@"                                                                     |_|      |___/ ");

            Console.WriteLine($"Copyright (C) 2020. Koromo Copy Project.");
            Console.WriteLine($"E-Mail: rollrat.cse@gmail.com");
            Console.WriteLine($"Source-code: https://github.com/rollrat/koromo-copy-project");
            Console.WriteLine($"Version: {Version.Text} (Build: {Internals.GetBuildDate().ToLongDateString()})");
            Console.WriteLine("");

            // Check Required Datas

            if (!HitomiData.Instance.CheckMetadataExist())
            {
                const string index_metadata_url = @"https://github.com/rollrat/koromo-copy-project/releases/download/database/index-metadata.compress";
                const string original_title_url = @"https://github.com/rollrat/koromo-copy-project/releases/download/database/origin-title.compress";

                Logs.Instance.Push("Welcome to Koromo Copy!\r\n\tDownload the necessary data before running the program!");

                var file1 = download_data(index_metadata_url, "index-metadata.compress");
                Logs.Instance.Push("Unzip to index-metadata.json...");
                File.WriteAllBytes("index-metadata.json", file1.UnzipByte());

                var file2 = download_data(original_title_url, "original-title.compress");
                Logs.Instance.Push("Unzip to original-title.json...");
                File.WriteAllBytes("original-title.json", file2.UnzipByte());
            }

            Logs.Instance.Push("Load index-metdata.json...");
            HitomiData.Instance.Load();
            HitomiData.Instance.OptimizeMetadata();
            HitomiData.Instance.RebuildTagData();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            // Check Server Update
            Logs.Instance.Push("Checking koromo-copy-backend-server version...");
            // Check Client Update
            Logs.Instance.Push("Checking koromo-copy-client-ui version...");

            // Start Server
            Server.Server.Instance.Start();

            // Start Client

            while (true)
            {
                Thread.Sleep(1000 * 60 * 10);
            }
        }

        static byte[] download_data(string url, string filename)
        {
            Logs.Instance.Push($"Download {filename}...");
            var task = NetTask.MakeDefault(url);

            SingleFileProgressBar pb = null;
            long tsz = 0;
            task.SizeCallback = (sz) =>
            {
                Console.Write("Downloading ... ");
                pb = new SingleFileProgressBar();
                pb.Report(sz, 0);
                tsz = sz;
            };
            task.DownloadCallback = (sz) => pb.Report(tsz, sz);
            var ret = NetTools.DownloadData(task);
            pb.Dispose();
            Console.WriteLine("Complete!");
            return ret;
        }

        static void ProcessExtract(string url, string[] args, string[] PathFormat, bool ExtractInformation, bool ExtractLinks, bool PrintProcess, bool DisableDownloadProgress)
        {
            var extractor = ExtractorManager.Instance.GetExtractor(url);

            if (extractor == null)
            {
                extractor = ExtractorManager.Instance.GetExtractorFromHostName(url);

                if (extractor == null)
                {
                    Console.WriteLine($"[Error] Cannot find a suitable extractor for '{url}'.");
                    return;
                }
                else
                {
                    Console.WriteLine("[Warning] Found an extractor for that url, but the url is not in the proper format to continue.");
                    Console.WriteLine("[Warning] Please refer to the following for proper conversion.");
                    Console.WriteLine($"[Input URL] {url}");
                    Console.WriteLine($"[Extractor Name] {extractor.GetType().Name}");
                    Console.WriteLine(extractor.ExtractorInfo);
                    return;
                }
            }
            else
            {
                try
                {
                    Console.WriteLine("Extractor Selected: " + extractor.GetType().Name.Replace("Extractor", ""));

                    if (extractor.IsForbidden)
                    {
                        Console.WriteLine("Crawling is prohibited by subject of recommendation in robots.txt provided by that website.");
                        return;
                    }

                    Console.Write("Extracting urls... ");

                    WaitProgress wp = null;

                    if (!PrintProcess)
                    {
                        Logs.Instance.ClearLogNotify();
                        if (!DisableDownloadProgress)
                            wp = new WaitProgress();
                    }

                    var option = extractor.RecommendOption(url);
                    option.CLParse(ref option, args);

                    if (option.Error)
                    {
                        if (wp != null) wp.Dispose();
                        Console.WriteLine($"[Input URL] {url}");
                        Console.WriteLine($"[Extractor Name] {extractor.GetType().Name}");
                        Console.WriteLine(option.ErrorMessage);
                        if (option.HelpMessage != null)
                            Console.WriteLine(option.HelpMessage);
                        return;
                    }

                    long extracting_progress_max = 0;
                    ExtractingProgressBar epb = null;

                    option.ProgressMax = (count) =>
                    {
                        extracting_progress_max = count;
                        if (wp != null)
                        {
                            wp.Dispose();
                            wp = null;
                            epb = new ExtractingProgressBar();
                            epb.Report(extracting_progress_max, 0);
                        }
                    };

                    long extracting_cumulative_count = 0;

                    option.PostStatus = (count) =>
                    {
                        var val = Interlocked.Add(ref extracting_cumulative_count, count);
                        if (epb != null)
                            epb.Report(extracting_progress_max, extracting_cumulative_count);
                    };

                    var tasks = extractor.Extract(url, option);

                    if (epb != null)
                    {
                        epb.Dispose();
                        Console.WriteLine("Done.");
                    }

                    if (wp != null)
                    {
                        wp.Dispose();
                        Console.WriteLine("Done.");
                    }

                    if (ExtractLinks)
                    {
                        foreach (var uu in tasks.Item1)
                            Console.WriteLine(uu.Url);
                        return;
                    }

                    string format;

                    if (PathFormat != null)
                        format = PathFormat[0];
                    else
                        format = extractor.RecommendFormat(option);

                    if (ExtractInformation)
                    {
                        Console.WriteLine($"[Input URL] {url}");
                        Console.WriteLine($"[Extractor Name] {extractor.GetType().Name}");
                        Console.WriteLine($"[Information] {extractor.ExtractorInfo}");
                        Console.WriteLine($"[Format] {format}");
                        return;
                    }

                    if (tasks.Item1 == null)
                    {
                        if (tasks.Item2 == null)
                        {
                            Console.WriteLine($"[Input URL] {url}");
                            Console.WriteLine($"[Extractor Name] {extractor.GetType().Name}");
                            Console.WriteLine("Nothing to work on.");
                            return;
                        }

                        Console.WriteLine(Logs.SerializeObject(tasks.Item2));
                        return;
                    }

                    int download_count = 0;

                    ProgressBar pb = null;

                    if (!PrintProcess && !DisableDownloadProgress)
                    {
                        Console.Write("Download files... ");
                        pb = new ProgressBar();
                    }

                    tasks.Item1.ForEach(task => {
                        task.Filename = Path.Combine(Settings.Instance.Model.SuperPath, task.Format.Formatting(format));
                        if (!Directory.Exists(Path.GetDirectoryName(task.Filename)))
                            Directory.CreateDirectory(Path.GetDirectoryName(task.Filename));
                        if (!PrintProcess && !DisableDownloadProgress)
                        {
                            task.DownloadCallback = (sz) =>
                                pb.Report(tasks.Item1.Count, download_count, sz);
                            task.CompleteCallback = () =>
                                Interlocked.Increment(ref download_count);
                        }
                        AppProvider.Scheduler.Add(task);
                    });

                    while (AppProvider.Scheduler.busy_thread != 0)
                    {
                        Thread.Sleep(500);
                    }

                    if (pb != null)
                    {
                        pb.Dispose();
                        Console.WriteLine("Done.");
                    }

                    WaitPostprocessor wpp = null;

                    if (AppProvider.PPScheduler.busy_thread != 0 && !PrintProcess && !DisableDownloadProgress)
                    {
                        Console.Write("Wait postprocessor... ");
                        wpp = new WaitPostprocessor();
                    }

                    while (AppProvider.PPScheduler.busy_thread != 0)
                    {
                        if (wpp != null) wpp.Report(AppProvider.PPScheduler.busy_thread + AppProvider.PPScheduler.queue.Count);
                        Thread.Sleep(500);
                    }

                    if (wpp != null)
                    {
                        wpp.Dispose();
                        Console.WriteLine("Done.");
                    }
                }
                catch (Exception e)
                {
                    Logs.Instance.PushError("[Extractor] Unhandled Exception - " + e.Message + "\r\n" + e.StackTrace);
                }
            }
        }
    }
}
