﻿// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Core;
using koromo_copy_backend.Crypto;
using koromo_copy_backend.Html;
using koromo_copy_backend.Network;
using koromo_copy_backend.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace koromo_copy_backend.Script
{
    /// <summary>
    /// Interpreter support for control and extension based on javascript(jint)
    /// </summary>
    public class KoromoScriptInstance
    {
        public class _Network
        {
            public static string get_string(string address) =>
                NetTools.DownloadString(address);
            public static _Html get_html(string address) =>
                new _Html(NetTools.DownloadString(address));
        }

        public class _Url
        {
            public static string get_parameter(string url, string param)
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query.Get(param);
            }

            public static string append_parameter(string url, string param, string value)
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                query.Set(param, value);
                return uri.AbsoluteUri.Split('?').FirstOrDefault() + '?' + query.ToString();
            }

            Uri uri;
            public _Url(string url) { uri = new Uri(url); }
            public string host { get { return uri.Host; } }
        }

        public class _Html
        {
            HtmlNode node;

            public _Html(string html) { node = html.ToHtmlNode(); }
            public _Html(HtmlNode node) { this.node = node; }
            public static _Html to_node(string html) => new _Html(html);

            public _Html select_single(string path) {
                var result = node.SelectSingleNode(path);
                if (result == null) return null;
                return new _Html(result);
            }
            public _Html[] select(string path) {
                var result = node.SelectNodes(path);
                if (result == null) return null;
                return result.Select(x => new _Html(x)).ToArray(); 
            }

            public string inner_text { get { return node.InnerText; } }
            public string inner_html { get { return node.InnerHtml; } }
            public string raw { get { return node.OuterHtml; } }
            public string text { get { return node.MyText(); } }

            public string attr(string what) { return node.GetAttributeValue(what, ""); }
            public string[] cal(string pattern) => HtmlCAL.Calculate(pattern, node).ToArray();
        }

        public class _Version
        {
            public string name { get; set; }
            public string version { get; set; }
            public string author { get; set; }
            public string id { get; set; }

            public _Version(string name, string author, string version, string id)
            { this.name = name; this.version = version; this.author = author; this.id = id; }
        }

        public class _File
        {
            public static string read(string filename)
            {
                if (filename.Contains('/') || filename.Contains('\\'))
                    throw new Exception("Prohibited operation executed.");
                return File.ReadAllText("script/" + filename);
            }

            public static void write(string filename, string content)
            {
                if (filename.Contains('/') || filename.Contains('\\') || filename.Contains(".js"))
                    throw new Exception("Prohibited operation executed.");
                File.WriteAllText("script/" + filename, content);
            }
        }

        static bool init = false;
        static void configure()
        {
            if (init) return;
            init = true;
            var engineSwitcher = JsEngineSwitcher.Current;
            engineSwitcher.EngineFactories.Add(new ChakraCoreJsEngineFactory());
            engineSwitcher.DefaultEngineName = ChakraCoreJsEngine.EngineName;
        }

        public static KoromoScriptInstance CreateNewInstance()
        {
            configure();
            var engine = JsEngineSwitcher.Current.CreateEngine(ChakraCoreJsEngine.EngineName);

            engine.EmbedHostType("net", typeof(_Network));
            engine.EmbedHostType("url", typeof(_Url));
            engine.EmbedHostType("html", typeof(_Html));
            engine.EmbedHostType("file", typeof(_File));

            return new KoromoScriptInstance(engine);
        }

        public _Version Version { get; private set; }
        IJsEngine engine;

        KoromoScriptInstance(IJsEngine engine)
        {
            this.engine = engine;

            engine.EmbedHostObject("debug", new Action<object>(debug));
            engine.EmbedHostObject("info", new Action<object>(push));
            engine.EmbedHostObject("error", new Action<object>(push_error));
            engine.EmbedHostObject("warning", new Action<object>(push_warning));
            engine.EmbedHostObject("register", new Action<string, string, string, string>(register));
        }

        public void Load(string js)
        {
            try
            {
                engine.Execute(js);
            }
            catch (Exception e)
            {
                Log.Logs.Instance.PushError($"[Script-{Version.id}] " + e);
            }
        }

        public object Invoke(string jsv, params object[] pp)
        {
            try
            {
                return engine.CallFunction(jsv, pp);
            }
            catch (Exception e)
            {
                Log.Logs.Instance.PushError($"[Script-{Version.id}] " + e);
            }

            return null;
        }

        void register(string name, string author, string version, string id) 
        {
            Version = new _Version(name, author, version, id);
            //engine.SetVariableValue("version", Version);
        }

        #region Logging
        void debug(object obj)
        {
#if DEBUG
            CultureInfo en = new CultureInfo("en-US");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("debug: ");
            Console.ResetColor();
            if (obj is string)
                Console.WriteLine($"[{DateTime.Now.ToString(en)}] {obj as string}");
            else
                Console.WriteLine($"[{DateTime.Now.ToString(en)}] {Log.Logs.SerializeObject(obj)}");
#endif
        }
        void push(object obj)
        {
            if (obj is string)
                if (Version != null)
                    Log.Logs.Instance.Push($"[Script-{Version.id}] " + (obj as string));
                else
                {
                    Log.Logs.Instance.Push($"[Script/unknown] " + (obj as string));
                    Log.Logs.Instance.PushWarning("Execute the register function before call the message output function.\r\n\t" +
                        "If you do not write the script specifications, registration may be cancelled.");
                }
            else
                Log.Logs.Instance.Push(obj);
        }
        void push_error(object obj)
        {
            if (obj is string)
                if (Version != null)
                    Log.Logs.Instance.PushError($"[Script-{Version.id}] " + (obj as string));
                else
                {
                    Log.Logs.Instance.PushError($"[Script/unknown] " + (obj as string));
                    Log.Logs.Instance.PushWarning("Execute the register function before call the message output function.\r\n\t" +
                        "If you do not write the script specifications, registration may be cancelled.");
                }
            else
                Log.Logs.Instance.PushError(obj);
        }
        void push_warning(object obj)
        {
            if (obj is string)
                if (Version != null)
                    Log.Logs.Instance.PushWarning($"[Script-{Version.id}] " + (obj as string));
                else
                {
                    Log.Logs.Instance.PushWarning($"[Script/unknown] " + (obj as string));
                    Log.Logs.Instance.PushWarning("Execute the register function before call the message output function.\r\n\t" +
                        "If you do not write the script specifications, registration may be cancelled.");
                }
            else
                Log.Logs.Instance.PushWarning(obj);
        }
        #endregion
    }
}