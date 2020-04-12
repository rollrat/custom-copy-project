﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using HtmlAgilityPack;
using custom_copy_backend.CL;
using custom_copy_backend.Network;
using custom_copy_backend.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using static custom_copy_backend.Extractor.IExtractorOption;

namespace custom_copy_backend.Extractor
{
    public class TwitterExtractorOption : IExtractorOption
    {
        [CommandLine("--limit-posts", CommandType.ARGUMENTS, Info = "Limit read posts count.", Help = "use --limit-posts <Number of post>")]
        public string[] LimitPosts;

        public override void CLParse(ref IExtractorOption model, string[] args)
        {
            model = CommandLineParser.Parse(model as TwitterExtractorOption, args);
        }
    }

    public class TwitterExtractor : ExtractorModel
    {
        public TwitterExtractor()
        {
            HostName = new Regex(@"twitter\.com");
            ValidUrl = new Regex(@"^https?://twitter\.com/(?<id>hashtag|.*?)(/(?<search>.*))?$");
            ExtractorInfo = "Twitter extactor info\r\n" +
                "   user:             Full-name.\r\n" +
                "   account:          User-name";
            IsForbidden = true;
        }

        public override IExtractorOption RecommendOption(string url)
        {
            return new TwitterExtractorOption { Type = ExtractorType.Images };
        }

        public override string RecommendFormat(IExtractorOption option)
        {
            return "%(extractor)s/%(user)s (%(account)s)/%(file)s.%(ext)s";
        }

        public override (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option = null)
        {
            if (option == null)
                option = RecommendOption(url);

            var match = ValidUrl.Match(url).Groups;

            var limit = int.MaxValue;

            if ((option as TwitterExtractorOption).LimitPosts != null)
                limit = (option as TwitterExtractorOption).LimitPosts[0].ToInt();

            if (match["id"].Value == "hashtag")
            {
#if DEBUG && false
                var html = NetTools.DownloadString(url);
                var search = HttpUtility.UrlDecode(match["search"].Value);
                var position = Regex.Match(html, @"data-max-position""(.*?)""").Groups[1].Value;

                var document = new HtmlDocument();
                document.LoadHtml(html);
                var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[2]/div[1]/div[2]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[2]/ol[1]");
                var tweets = node.SelectNodes("./li[@data-item-type='tweet']");
                var urls = new List<string>();

                foreach (var tweet in tweets)
                    urls.AddRange(parse_tweet_hashtag(option as TwitterExtractorOption, tweet));

                while (true)
                {
                    try
                    {
                        var next = seach_query(option as TwitterExtractorOption, search, position);
                        position = JToken.Parse(next)["min_position"].ToString();
                        var html2 = JToken.Parse(next)["items_html"].ToString();
                        var document2 = new HtmlDocument();
                        document2.LoadHtml(html2);
                        var tweets2 = node.SelectNodes("./li[@data-item-type='tweet']");
                        foreach (var tweet in tweets2)
                            urls.AddRange(parse_tweet_hashtag(option as TwitterExtractorOption, tweet));
                    }
                    catch
                    {
                        break;
                    }
                }

                var result = new List<NetTask>();
                foreach (var surl in urls)
                {
                    var task = NetTask.MakeDefault(surl);
                    task.SaveFile = true;

                    var fn = surl.Split('/').Last();
                    task.Filename = fn;
                    task.Format = new ExtractorFileNameFormat
                    {
                        FilenameWithoutExtension = Path.GetFileNameWithoutExtension(fn),
                        Extension = Path.GetExtension(fn).Replace(".", ""),
                        User = search
                    };

                    result.Add(task);
                }
                return new Tuple<List<NetTask>, object>(result, null);
#endif
                throw new ExtractorException("'hashtag' is not support yet!");
            }
            else
            {
                var name = match["id"].Value;
                var html = NetTools.DownloadString($"https://twitter.com/{name}/media");
                var min_position = Regex.Match(html, @"data-min-position=""(.*?)""").Groups[1].Value;
                var node = html.ToHtmlNode();
                var tweets = node.SelectNodes("./html[1]/body[1]/div[1]/div[2]/div[1]/div[2]/div[1]/div[1]/div[2]/div[1]/div[2]/div[2]/div[1]/div[2]/ol[1]/li[@data-item-type='tweet']");
                var urls = new List<string>();
                var user = node.SelectSingleNode("/html[1]/body[1]/div[1]/div[2]/div[1]/div[2]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/div[1]/h1[1]/a[1]").InnerText;
                var videos = new List<(string, List<string>)>();
                var post_count = tweets.Count;
                var last_url_count = 0;

                option.SimpleInfoCallback?.Invoke($"{user} ({name})");

                foreach (var tweet in tweets)
                    urls.AddRange(parse_tweet_hashtag(option as TwitterExtractorOption, tweet, videos));

                while (post_count < limit)
                {
                    var next = profile_query(option as TwitterExtractorOption, name, min_position);
                    var html2 = JToken.Parse(next)["items_html"].ToString();
                    var tweets2 = html2.ToHtmlNode().SelectNodes("./li[@data-item-type='tweet']");
                    if (tweets2 == null)
                        break;
                    foreach (var tweet in tweets2)
                        urls.AddRange(parse_tweet_hashtag(option as TwitterExtractorOption, tweet, videos));
                    option.PostStatus?.Invoke(urls.Count - last_url_count);
                    last_url_count = urls.Count;
                    post_count += tweets2.Count;
                    min_position = JToken.Parse(next)["min_position"].ToString();
                    if (!(bool)JToken.Parse(next)["has_more_items"])
                        break;
                    Thread.Sleep(3000);
                }

                var result = new List<NetTask>();
                foreach (var surl in urls)
                {
                    var task = NetTask.MakeDefault(surl);
                    task.SaveFile = true;

                    var fn = surl.Split('/').Last();
                    task.Filename = fn;
                    task.Format = new ExtractorFileNameFormat
                    {
                        FilenameWithoutExtension = Path.GetFileNameWithoutExtension(fn),
                        Extension = Path.GetExtension(fn).Replace(".", ""),
                        Account = name,
                        User = user,
                    };

                    result.Add(task);
                }

                foreach (var video in videos)
                {
                    var count = 0;
                    foreach (var ts in video.Item2)
                    {
                        var task = NetTask.MakeDefault(ts);
                        task.SaveFile = true;

                        var fn = ts.Split('/').Last();
                        task.Filename = fn;
                        task.Format = new ExtractorFileNameFormat
                        {
                            FilenameWithoutExtension = video.Item1 + "/" + count++.ToString("000"),
                            Extension = Path.GetExtension(fn).Replace(".", ""),
                            Account = name,
                            User = user,
                        };

                        result.Add(task);
                    }
                }

                result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
                return (result, new ExtractedInfo { Type = ExtractedInfo.ExtractedType.UserArtist });
            }
        }

        private List<string> parse_tweet_hashtag(TwitterExtractorOption option, HtmlNode tweet, List<(string, List<string>)> video)
        {
            var result = new List<string>();
            var media_img = tweet.SelectNodes("./div[1]/div[2]/div[@class='AdaptiveMediaOuterContainer']//img");
            var media_video = tweet.SelectNodes("./div[1]/div[2]/div[@class='AdaptiveMediaOuterContainer']//div[@class='AdaptiveMedia-video']");

            if (media_img != null)
            {
                foreach (var img in media_img)
                    result.Add(img.GetAttributeValue("src", ""));
            }

            if (media_video != null && false)
            {
                var id = tweet.SelectSingleNode("./div[1]").GetAttributeValue("data-tweet-id", "");
                var url = $"https://api.twitter.com/1.1/videos/tweet/config/{id}.json";

                option.PageReadCallback?.Invoke(url);

                var task = NetTask.MakeDefault(url);
                task.Headers = new Dictionary<string, string>();
                task.Headers.Add("authorization", "Bearer AAAAAAAAAAAAAAAAAAAAAPYXBAAAAAAACLXUNDekMxqa8h%2F40K4moUkGsoc%3DTYfbDKbT3jJPCEVnMYqilB28NHfOPqkca3qaAxGfsyKCs0wRbw");
                task.RetryCallback = (count) => Thread.Sleep(5000);
                var data = NetTools.DownloadString(task);

                if (JToken.Parse(data)["track"]["contentType"].ToString() == "gif")
                    result.Add(JToken.Parse(data)["track"]["playbackUrl"].ToString());
                else
                {
                    // m3u8
                    var m3u8m3u8_url = JToken.Parse(data)["track"]["playbackUrl"].ToString();
                    var m3u8_url = NetTools.DownloadString(m3u8m3u8_url).Trim().Split('\n').Last().Trim();
                    var m3u8 = NetTools.DownloadString("https://video.twimg.com" + m3u8_url);

                    var ts_urls = new List<string>();
                    foreach (var line in m3u8.Split('\n'))
                        if (!line.Trim().StartsWith("#") && line.Contains("ext_tw_video"))
                            ts_urls.Add("https://video.twimg.com" + line);

                    video.Add((m3u8_url.Split('/').Last().Split('.')[0], ts_urls));
                }
            }

            return result;
        }

        private static string seach_query(TwitterExtractorOption option, string search, string position)
        {
            var url = $"https://twitter.com/i/search/timeline?";
            url += "verical=default&";
            url += "q=" + HttpUtility.UrlEncode("#" + search) + "&";
            url += "include_available_features=1&";
            url += "include_entities=1&";
            url += "max_position=" + HttpUtility.UrlEncode(position) + "&";
            url += "reset_error_state=false";
            option.PageReadCallback?.Invoke(url);
            return NetTools.DownloadString(url);
        }

        private static string profile_query(TwitterExtractorOption option, string name, string position)
        {
            var url = $"https://twitter.com/i/profiles/show/{name}/media_timeline?";
            url += "include_available_features=1&";
            url += "include_entities=1&";
            url += $"max_position={position}&";
            url += "reset_error_state=false";
            option.PageReadCallback?.Invoke(url);
            var task = NetTask.MakeDefault(url);
            task.RetryCallback = (count) => Thread.Sleep(5000);
            return NetTools.DownloadString(task);
        }
    }
}
