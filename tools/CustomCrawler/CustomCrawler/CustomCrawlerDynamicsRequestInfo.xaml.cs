/***

   Copyright (C) 2020. rollrat. All Rights Reserved.
   
   Author: Custom Crawler Developer

***/

using MasterDevs.ChromeDevTools;
using MasterDevs.ChromeDevTools.Protocol.Chrome.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// CustomCrawlerDynamicsRequestInfo.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CustomCrawlerDynamicsRequestInfo : Window
    {
        public CustomCrawlerDynamicsRequestInfo(RequestWillBeSentEvent request)
        {
            InitializeComponent();

            var builder = new StringBuilder();

            builder.Append($"Request Type: {request.Type.ToString()}\r\n");
            builder.Append($"Request Body:\r\n");
            builder.Append(JsonConvert.SerializeObject(request.Request, Formatting.Indented));
            builder.Append($"\r\n");
            builder.Append($"\r\n");
            builder.Append($"Response:\r\n");

            var result = CustomCrawlerDynamics.ss.SendAsync(new GetResponseBodyCommand
            {
                RequestId = request.RequestId
            }).Result;

            if (result.Result.Base64Encoded)
            {
                string body;
                result.Result.Body.TryParseBase64(out body);
                builder.Append(body);
            }
            else
                builder.Append(result.Result.Body);

            Info.Text = builder.ToString();
        }
    }
}
