// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using custom_copy_backend.Log;
using custom_copy_backend.Network;
using custom_copy_backend.Postprocessor;
using custom_copy_backend.Setting;
using custom_copy_backend.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace custom_copy_backend
{
    public class AppProvider
    {
        public static string ApplicationPath = Directory.GetCurrentDirectory();
        public static string DefaultSuperPath = Directory.GetCurrentDirectory();

        public static Dictionary<string, object> Instance =>
            InstanceMonitor.Instances;

        public static NetScheduler Scheduler { get; set; }

        public static PostprocessorScheduler PPScheduler { get; set; }

        public static DateTime StartTime = DateTime.Now;

        public static void Initialize()
        {
            // Initialize logs instance
            GCLatencyMode oldMode = GCSettings.LatencyMode;
            RuntimeHelpers.PrepareConstrainedRegions();
            GCSettings.LatencyMode = GCLatencyMode.Batch;

            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            Scheduler = new NetScheduler(Settings.Instance.Model.ThreadCount);
            PPScheduler = new PostprocessorScheduler(Settings.Instance.Model.PostprocessorThreadCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        public static void Deinitialize()
        {
        }
    }
}
