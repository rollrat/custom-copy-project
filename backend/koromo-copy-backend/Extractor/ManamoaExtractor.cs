﻿// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using koromo_copy_backend.Network;
using koromo_copy_backend.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static koromo_copy_backend.Extractor.IExtractorOption;

namespace koromo_copy_backend.Extractor
{
    public class ManamoaExtractorOption : IExtractorOption
    {
    }

    public class ManamoaExtractor : ExtractorModel
    {
        public string ManamoaPHPSESSID;

        public ManamoaExtractor()
        {
            HostName = new Regex(@"manamoa\d+\.net");
            ValidUrl = new Regex(@"^(?<host>https?://manamoa\d*\.net)/bbs/(?<type>page|board)\.php.*(manga_id|wr_id)=(?<code>\d+).*?$");
        }

        public class ComicInformation
        {
            public string Title;
            public string Author;
        }

        public override IExtractorOption RecommendOption(string url)
        {
            var match = ValidUrl.Match(url).Groups;

            if (match["type"].Value == "board")
                return new NaverExtractorOption { Type = ExtractorType.EpisodeImages };
            else
                return new NaverExtractorOption { Type = ExtractorType.Works };
        }

        public override string RecommendFormat(IExtractorOption option)
        {
            if (option.Type == ExtractorType.EpisodeImages)
                return "%(extractor)s/%(episode)s/%(file)s.%(ext)s";
            else
                return "%(extractor)s/%(title)s/%(episode)s/%(file)s.%(ext)s";
        }

        public override (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option = null)
        {
            if (option == null)
                option = RecommendOption(url);

            var html = NetTools.DownloadString(url);
            var match = ValidUrl.Match(url).Groups;

            var document = new HtmlDocument();
            document.LoadHtml(html);
            var node = document.DocumentNode;

            if (option.Type == ExtractorType.EpisodeImages)
            {
                var images = get_board_images(html);
                var title = node.SelectSingleNode("/html[1]/head[1]/title[1]").InnerText;

                var result = new List<NetTask>();
                int count = 1;
                foreach (var img in images)
                {
                    var task = NetTask.MakeDefault(img[0]);
                    task.SaveFile = true;
                    task.Filename = count.ToString("000") + Path.GetExtension(img[0].Split('/').Last());
                    task.Format = new ExtractorFileNameFormat
                    {
                        Episode = title,
                        FilenameWithoutExtension = count.ToString("000"),
                        Extension = Path.GetExtension(task.Filename).Replace(".", "")
                    };
                    task.FailUrls = img.Skip(1).ToList();
                    result.Add(task);
                    count++;
                }

                result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
                return (result, null);
            }
            else if (option.Type == ExtractorType.Works)
            {
                var title = node.SelectSingleNode("/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[2]/div[1]/div[1]").InnerText;
                var sub_urls = new List<string>();
                var sub_titles = new List<string>();

                option.SimpleInfoCallback?.Invoke($"{title}");

                option.ThumbnailCallback?.Invoke(NetTask.MakeDefault(
                    Regex.Match(node.SelectSingleNode("/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]").GetAttributeValue("style",""), @"(https?://.*?)\)").Groups[1].Value));

                foreach (var item in node.SelectNodes("/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[2]/div[2]/div[1]/div[1]/div"))
                {
                    sub_urls.Add(match["host"] + item.SelectSingleNode("./a[1]").GetAttributeValue("href", ""));
                    sub_titles.Add(item.SelectSingleNode("./a[1]/div[1]").MyText());
                }

                option.ProgressMax?.Invoke(sub_urls.Count);

                var htmls = NetTools.DownloadStrings(sub_urls, "PHPSESSID=" + ManamoaPHPSESSID, () =>
                {
                    option.PostStatus?.Invoke(1);
                });

                var result = new List<NetTask>();
                for (int i = 0; i < sub_urls.Count; i++)
                {
                    try
                    {
                        var images = get_board_images(htmls[i]);
                        int count = 1;
                        foreach (var img in images)
                        {
                            var task = NetTask.MakeDefault(img[0]);
                            task.SaveFile = true;
                            task.Filename = count.ToString("000") + Path.GetExtension(img[0].Split('/').Last());
                            task.Format = new ExtractorFileNameFormat
                            {
                                Title = title,
                                Episode = sub_titles[i],
                                FilenameWithoutExtension = count.ToString("000"),
                                Extension = Path.GetExtension(task.Filename).Replace(".", ""),
                            };
                            task.FailUrls = img.Skip(1).ToList();
                            result.Add(task);
                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        ;
                    }
                }

                result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
                return (result, new ExtractedInfo { Type = ExtractedInfo.ExtractedType.WorksComic });
            }

            return (null, null);
        }

        private List<List<string>> get_board_images(string html)
        {
            var result = new List<List<string>>();

            var list = JArray.Parse(Regex.Match(html, "var img_list = (.*);").Groups[1].Value);
            var list1 = JArray.Parse(Regex.Match(html, "var img_list1 = (.*);").Groups[1].Value);

            var cnt = Math.Max(list.Count, list1.Count);

            for (int i = 0; i < cnt; i++)
            {
                var rr = new List<string>();
                if (list.Count > 0)
                {
                    rr.Add(list[i].ToString());
                    rr.Add(list[i].ToString().Replace("img", "s3"));
                }
                if (list1.Count > 0)
                    rr.Add(list1[i].ToString());
                result.Add(rr);
            }

            return result;
        }
    }
}
