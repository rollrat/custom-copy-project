﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using custom_copy_backend.CL;
using custom_copy_backend.Network;
using custom_copy_backend.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using static custom_copy_backend.Extractor.IExtractorOption;

namespace custom_copy_backend.Extractor
{
    public class GelbooruExtractorOption : IExtractorOption
    {
        [CommandLine("--start-page", CommandType.ARGUMENTS, Info = "Set start page.", Help = "use --start-page <Start Page Number>")]
        public new string[] StartPage;
        [CommandLine("--end-page", CommandType.ARGUMENTS, Info = "Set end page.", Help = "use --start-page <End Page Number>")]
        public new string[] EndPage;

        public override void CLParse(ref IExtractorOption model, string[] args)
        {
            model = CommandLineParser.Parse(model as GelbooruExtractorOption, args);
        }
    }

    public class GelbooruExtractor : ExtractorModel
    {
        public GelbooruExtractor()
        {
            HostName = new Regex(@"gelbooru\.com");
            ValidUrl = new Regex(@"^https?://gelbooru\.com/index\.php\?.*?tags\=(.*?)(\&.*?)?$");
        }

        public override IExtractorOption RecommendOption(string url)
        {
            return new GelbooruExtractorOption { Type = ExtractorType.Images };
        }

        public override string RecommendFormat(IExtractorOption option)
        {
            return "%(extractor)s/%(search)s/%(file)s.%(ext)s";
        }

        public override (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option = null)
        {
            var match = ValidUrl.Match(url).Groups;

            if (option == null)
                option = new GelbooruExtractorOption { Type = ExtractorType.Images };

            var tags = match[1].Value;
            var result = new List<NetTask>();
            var page = 0;
            if ((option as GelbooruExtractorOption).StartPage != null)
                page = (option as GelbooruExtractorOption).StartPage[0].ToInt();

            var end_page = int.MaxValue;
            if ((option as GelbooruExtractorOption).EndPage != null)
                end_page = (option as GelbooruExtractorOption).EndPage[0].ToInt();

            option.SimpleInfoCallback?.Invoke($"{HttpUtility.UrlDecode(tags)}");

            var post_thumbnail = false;
            while (true)
            {
                var durl = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags=" + tags + "&pid=" + page.ToString();

                option.PageReadCallback?.Invoke(durl);

                var data = NetTools.DownloadString(durl);

                var document = new HtmlDocument();
                document.LoadHtml(data);
                var nodes = document.DocumentNode.SelectNodes("/posts[1]/post");

                if (nodes == null || nodes.Count == 0)
                    break;

                foreach (var node in nodes)
                {
                    var imgurl = node.GetAttributeValue("file_url", "");
                    var task = NetTask.MakeDefault(imgurl);
                    task.SaveFile = true;
                    task.Filename = imgurl.Split('/').Last();
                    task.Format = new ExtractorFileNameFormat
                    {
                        Search = HttpUtility.UrlDecode(tags),
                        FilenameWithoutExtension = Path.GetFileNameWithoutExtension(imgurl.Split('/').Last()),
                        Extension = Path.GetExtension(imgurl.Split('/').Last()).Replace(".", "")
                    };
                    result.Add(task);
                }

                if (!post_thumbnail)
                {
                    option.ThumbnailCallback?.Invoke(result[0]);
                    post_thumbnail = true;
                }

                option.PostStatus?.Invoke(nodes.Count);

                page += 1;

                if (page > end_page)
                    break;
            }

            result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
            return (result, new ExtractedInfo { Type = ExtractedInfo.ExtractedType.Search });
        }
    }
}
