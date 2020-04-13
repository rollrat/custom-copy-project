/***

   Copyright (C) 2020. rollrat. All Rights Reserved.
   
   Author: Custom Crawler Developer

***/

using CefSharp;
using CefSharp.Wpf;
using CustomCrawler.chrome_devtools;
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

        public CustomCrawlerDynamics()
        {
            InitializeComponent();

            browser = new ChromiumWebBrowser(string.Empty);
            browserContainer.Content = browser;

            browser.LoadingStateChanged += Browser_LoadingStateChanged;

            Closed += CustomCrawlerDynamics_Closed;
        }

        private async void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading && ss != null)
            {
                var doc = await ss.SendAsync(new GetDocumentCommand { Depth = -1 });

                _ = Task.Run(async () =>
                {
                    await find_source(doc.Result.Root);
                });
            }
        }

        List<(long, StackTrace)> stacks = new List<(long, StackTrace)>();
        private async Task find_source(Node nn)
        {
            if (nn.Children != null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    URLText.Text = nn.NodeId.ToString();
                }));
                var st = await ss.SendAsync(new GetNodeStackTracesCommand { NodeId = nn.NodeId });
                if (st.Result != null && st.Result.Creation != null)
                {
                    stacks.Add((nn.NodeId, st.Result.Creation));
                }
                foreach (var child in nn.Children)
                {
                    await find_source(child);
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
    }
}
