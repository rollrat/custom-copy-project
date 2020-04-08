﻿// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using koromo_copy_backend.Network;
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
    public class HiyobiExtractorOption : IExtractorOption
    {
    }

    class HiyobiExtractor : ExtractorModel
    {
        public HiyobiExtractor()
        {
            HostName = new Regex(@"xn--9w3b15m8vo\.asia|hiyobi\.me");
            ValidUrl = new Regex(@"^https?://(?<host>xn--9w3b15m8vo\.asia|hiyobi\.me|히요비\.asia)/(?<type>reader|search)/(?<id>\d*)(?<token>.*)$");
        }

        public override IExtractorOption RecommendOption(string url)
        {
            var match = ValidUrl.Match(url).Groups;
            if (match["type"].Value == "reader")
                return new HiyobiExtractorOption { Type = ExtractorType.Images };
            else if (match["type"].Value == "search")
                return new HiyobiExtractorOption { Type = ExtractorType.ComicIndex };
            return new HiyobiExtractorOption { Type = ExtractorType.Images };
        }

        public override string RecommendFormat(IExtractorOption option)
        {
            return "%(extractor)s/%(artist)s/[%(id)s] %(title)s/%(file)s.%(ext)s";
        }

        public override (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option = null)
        {
            var match = ValidUrl.Match(url).Groups;

            if (option == null)
                option = RecommendOption(url);

            if (match["type"].Value == "reader")
            {
                var id = match["id"].Value;
                var article_info_url = $"https://hiyobi.me/info/{id}";
                option.PageReadCallback?.Invoke(article_info_url);
                var info_html = NetTools.DownloadString(article_info_url);
                var data = parse_info(info_html);

                var img_file_json_url = $"https://xn--9w3b15m8vo.asia/data/json/{id}_list.json";
                option.PageReadCallback?.Invoke(img_file_json_url);
                var cookie = "__cfduid=d53c18b351d4a54007ac583a96f4436381568466715";
                var img_file_json_task = NetTask.MakeDefault(img_file_json_url, cookie);
                var img_file_json = NetTools.DownloadString(img_file_json_task);
                var img_urls = JArray.Parse(img_file_json).Select(x => $"https://xn--9w3b15m8vo.asia/data/{id}/{x["name"].ToString()}").ToList();

                option.SimpleInfoCallback?.Invoke($"{data.Title}");

                var result = new List<NetTask>();
                var count = 1;
                foreach (var img in img_urls)
                {
                    var task = NetTask.MakeDefault(img);
                    task.SaveFile = true;
                    task.Filename = img.Split('/').Last();
                    task.Cookie = cookie;
                    task.Format = new ExtractorFileNameFormat
                    {
                        Id = id,
                        Title = data.Title,
                        Artist = data.artist != null ? data.artist[0] : "N/A",
                        Group = data.artist != null ? data.artist[0] : "N/A",
                        FilenameWithoutExtension = count++.ToString("000"),
                        Extension = Path.GetExtension(task.Filename).Replace(".", "")
                    };
                    result.Add(task);
                }
                option.ThumbnailCallback?.Invoke(result[0]);
                result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
                return (result, new ExtractedInfo { Type = ExtractedInfo.ExtractedType.WorksComic });
            }
            else
            {
                throw new ExtractorException("'search' page not supports yet!");
            }
        }
        
        private static HitomiArticle parse_info(string html)
        {
            HitomiArticle article = new HitomiArticle();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//main/div")[0];

            article.Thumbnail = "https://hiyobi.me" + node.SelectSingleNode(".//img").GetAttributeValue("src", "");

            var span = node.SelectSingleNode("./span");
            article.Title = span.SelectSingleNode("./h5/a/b").InnerText;

            var table = span.SelectNodes("./table/tr");
            var table_dic = new Dictionary<string, HtmlNode>();
            table.ToList().ForEach(x => table_dic.Add(x.SelectSingleNode("./td[1]").InnerText.Remove(2), x));

            if (table_dic.ContainsKey("작가")) try {
                    var group = new List<string>();
                    article.artist = table_dic["작가"].SelectNodes("./td[2]/a").ToList().Select(x =>
                    {
                        if (x.InnerText.Contains("("))
                            group.Add(x.InnerText.Split('(')[1].Split(')')[0].Trim());
                        return x.InnerText.Split('(')[0].Trim();
                    }).ToArray();
                    if (group.Count != 0)
                        article.group = group.ToArray();
                } catch { }
            if (table_dic.ContainsKey("원작")) try { article.parody = table_dic["원작"].SelectNodes("./td[2]/a").ToList().Select(x => $"{x.GetAttributeValue("data-original", "")}|{x.InnerText}").ToArray(); } catch { }
            if (table_dic.ContainsKey("종류")) try { article.Type = table_dic["종류"].SelectSingleNode("./td[2]/a").InnerText; } catch { }
            if (table_dic.ContainsKey("태그")) try { article.Tags = table_dic["태그"].SelectNodes("./td[2]/a").ToList().Select(x => $"{x.GetAttributeValue("data-original", "")}|{x.InnerText}").ToArray(); } catch { }
            if (table_dic.ContainsKey("캐릭")) try { article.character = table_dic["캐릭"].SelectNodes("./td[2]/a").ToList().Select(x => $"{x.GetAttributeValue("data-original", "")}|{x.InnerText}").ToArray(); } catch { }

            return article;
        }
    }
}
