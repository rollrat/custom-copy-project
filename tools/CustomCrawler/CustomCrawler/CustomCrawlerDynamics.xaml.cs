/***

   Copyright (C) 2020. rollrat. All Rights Reserved.
   
   Author: Custom Crawler Developer

***/

using CefSharp;
using CefSharp.Wpf;
using CustomCrawler.chrome_devtools;
using HtmlAgilityPack;
using MasterDevs.ChromeDevTools;
using MasterDevs.ChromeDevTools.Protocol.Chrome.DOM;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CustomCrawler
{
    /// <summary>
    /// CustomCrawlerDynamics.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CustomCrawlerDynamics : Window
    {
        ChromiumWebBrowser browser;
        CallbackCCW cbccw;

        public CustomCrawlerDynamics()
        {
            InitializeComponent();

            browser = new ChromiumWebBrowser(string.Empty);
            browserContainer.Content = browser;

            browser.LoadingStateChanged += Browser_LoadingStateChanged;

            CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            browser.JavascriptObjectRepository.Register("ccw", cbccw = new CallbackCCW(this), isAsync: true);

            Closed += CustomCrawlerDynamics_Closed;

            KeyDown += CustomCrawlerDynamics_KeyDown;
        }

        private void CustomCrawlerDynamics_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                locking = !locking;
            }
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading && ss != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    Build.IsEnabled = true;
                }));
            }
        }

        #region Cef Callback

        bool locking = false;
        public class CallbackCCW
        {
            CustomCrawlerDynamics instance;
            string before = "";
            public string before_border = "";
            string latest_elem = "";
            public HtmlNode selected_node;
            public CallbackCCW(CustomCrawlerDynamics instance)
            {
                this.instance = instance;
            }
            public static bool ignore_js(string url)
            {
                var filename = url.Split('?')[0].Split('/').Last();

                if (filename.StartsWith("jquery"))
                    return true;

                //switch (filename.ToLower())
                //{
                //    case "js.cookie.js":
                //    case "html5shiv.min.js":
                //    case "yall.js":
                //    case "moment.js":
                //    case "yall.min.js":
                //    case "underscore.js":
                //    case "partial.js":
                //        return true;
                //}

                return false;
            }
            public void hoverelem(string elem, bool adjust = false)
            {
                if (instance.locking) return;
                Application.Current.Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    try
                    {
                        instance.browser.GetMainFrame().EvaluateScriptAsync($"document.querySelector('[{before}]').style.border = '{before_border}';").Wait();
                        before = $"ccw_tag={elem}";
                        before_border = instance.browser.GetMainFrame().EvaluateScriptAsync($"document.querySelector('[{before}]').style.border").Result.Result.ToString();
                        instance.browser.GetMainFrame().EvaluateScriptAsync($"document.querySelector('[{before}]').style.border = '0.2em solid red';").Wait();

                        if (instance.stacks.ContainsKey(elem))
                        {
                            var stack = instance.stacks[elem];
                            var builder = new StringBuilder();

                        NEXT:
                            {
                                if (!string.IsNullOrEmpty(stack.Description))
                                    builder.Append("Description: " + stack.Description + "\r\n");

                                foreach (var frame in stack.CallFrames)
                                {
                                    if (ignore_js(frame.Url))
                                        continue;
                                    //if (!frame.Url.Contains("comment"))
                                    //    continue;
                                    builder.Append($"{frame.Url}:<{frame.FunctionName}>:{frame.LineNumber + 1}:{frame.ColumnNumber + 1}\r\n");

                                    // Currently not support html built-in script
                                    JsManager.Instance.FindByLocation(frame.Url, (int)frame.LineNumber + 1, (int)frame.ColumnNumber + 1);
                                }

                                if (stack.Parent != null)
                                {
                                    stack = stack.Parent;
                                    goto NEXT;
                                }
                            }

                            instance.Info.Text = builder.ToString();
                        }
                        else
                        {
                            instance.Info.Text = "Static Node";
                        }
                    }
                    catch (Exception e)
                    {
                        ;
                    }
                }));
            }
            public void adjust()
            {
                hoverelem(latest_elem, true);
            }
        }

        #endregion

        Dictionary<string, StackTrace> stacks = new Dictionary<string, StackTrace>();
        private async Task find_source(Node nn)
        {
            //if (nn.Children != null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    URLText.Text = nn.NodeId.ToString();
                }));
                var st = await ss.SendAsync(new GetNodeStackTracesCommand { NodeId = nn.NodeId });
                if (st.Result != null && st.Result.Creation != null)
                {
                    stacks.Add("ccw_" + nn.NodeId, st.Result.Creation);
                }
                if (nn.NodeType == 1)
                {
                    await ss.SendAsync(new SetAttributeValueCommand
                    {
                        NodeId = nn.NodeId,
                        Name = "ccw_tag",
                        Value = "ccw_" + nn.NodeId
                    });
                    await ss.SendAsync(new SetAttributeValueCommand
                    {
                        NodeId = nn.NodeId,
                        Name = "onmouseenter",
                        Value = $"ccw.hoverelem('ccw_{nn.NodeId}')"
                    });
                    await ss.SendAsync(new SetAttributeValueCommand
                    {
                        NodeId = nn.NodeId,
                        Name = "onmouseleave",
                        Value = $"ccw.hoverelem('ccw_{nn.NodeId}')"
                    });
                }
                if (nn.Children != null)
                {
                    foreach (var child in nn.Children)
                    {
                        await find_source(child);
                    }
                }
            }
        }

        private void CustomCrawlerDynamics_Closed(object sender, EventArgs e)
        {
           ChromeDevTools.Dispose();
        }

        IChromeSession ss;
        bool init = false;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!init)
            {
                var token = new Random().Next();
                browser.LoadHtml(token.ToString());
                init = true;

                ss = await ChromeDevTools.Create();
                new CustomCrawlerDynamicsRequest(ss).Show();
            }

            browser.Load(URLText.Text);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await Application.Current.Dispatcher.BeginInvoke(new Action(
            delegate
            {
                Build.IsEnabled = false;
            }));

            var doc = await ss.SendAsync(new GetDocumentCommand { Depth = -1 });

            stacks = new Dictionary<string, StackTrace>();
            await find_source(doc.Result.Root);

            await Application.Current.Dispatcher.BeginInvoke(new Action(
            delegate
            {
                Build.IsEnabled = true;
            }));
        }
    }
}
