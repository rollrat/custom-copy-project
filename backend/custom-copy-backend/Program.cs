﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.
// Custom Copy Backend Server Source Code

using custom_copy_backend.Log;
using System;
using System.Globalization;
using System.Text;

namespace custom_copy_backend
{
    public class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AppProvider.Initialize();

            Logs.Instance.AddLogNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("info: ");
                Console.ResetColor();
                Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logs.Instance.AddLogErrorNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("error: ");
                Console.ResetColor();
                Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logs.Instance.AddLogWarningNotify((s, e) => {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.Write("warning: ");
                Console.ResetColor();
                Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logs.Instance.PushError("unhandled: " + (e.ExceptionObject as Exception).ToString());
            };

            try
            {
                Command.Start(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured! " + e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Please, check log.txt file.");
            }

            AppProvider.Deinitialize();

            Environment.Exit(0);
        }
    }
}
