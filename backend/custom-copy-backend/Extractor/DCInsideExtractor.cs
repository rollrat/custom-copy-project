﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlAgilityPack;
using custom_copy_backend.CL;
using custom_copy_backend.Network;
using Newtonsoft.Json;
using static custom_copy_backend.Extractor.IExtractorOption;

namespace custom_copy_backend.Extractor
{
    #region DCInside Models

    public class DCArticle
    {
        public string Id { get; set; }
        public string GalleryName { get; set; }
        public string OriginalGalleryName { get; set; }
        public string Thumbnail { get; set; }
        public string Class { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public List<string> ImagesLink { get; set; }
        public List<string> FilesName { get; set; }
        public string Archive { get; set; }
        public string ESNO { get; set; }
    }

    public class DCPageArticle
    {
        public string no;
        public string classify;
        public string type;
        public string title;
        public string replay_num;
        public string nick;
        public string uid;
        public string ip;
        public bool islogined;
        public bool isfixed;
        public DateTime date;
        public string count;
        public string recommend;
    }

    public class DCGallery
    {
        public string id;
        public string name;
        public string esno;
        public string cur_page;
        public string max_page;
        public DCPageArticle[] articles;
    }

    public class DCCommentElement
    {
        public string no;
        public string parent;
        public string user_id;
        public string name;
        public string ip;
        public string reg_date;
        public string nicktype;
        public string t_ch1;
        public string t_ch2;
        public string vr_type;
        public string voice;
        public string rcnt;
        public string c_no;
        public int depth;
        public string del_yn;
        public string is_delete;
        public string memo;
        public string my_cmt;
        public string del_btn;
        public string mod_btn;
        public string a_my_cmt;
        public string reply_w;
        public string gallog_icon;
        public bool vr_player;
        public string vr_player_tag;
        public int next_type;
    }

    public class DCComment
    {
        public int total_cnt;
        public int comment_cnt;
        public DCCommentElement[] comments;
    }

    public class DCGalleryModel
    {
        public bool is_minor_gallery;
        public string gallery_id;
        public DCPageArticle[] articles;
    }

    #endregion

    public class DCInsideExtractorOption : IExtractorOption
    {
        [CommandLine("--gaenyum", CommandType.OPTION, Info = "Extract only gaenyum articles.")]
        public bool OnlyRecommend;

        public override void CLParse(ref IExtractorOption model, string[] args)
        {
            model = CommandLineParser.Parse(model as DCInsideExtractorOption, args);
        }
    }

    public class DCInsideExtractor : ExtractorModel
    {
        public DCInsideExtractor()
        {
            HostName = new Regex(@"(gall|m)\.dcinside\.com");
            ValidUrl = new Regex(@"^https?://(gall|m)\.dcinside\.com/(mgallery/)?board/(lists|view)/?\?(.*?)$");
        }

        public override IExtractorOption RecommendOption(string url)
        {
            var match = ValidUrl.Match(url).Groups;

            if (match[1].Value == "gall")
            {
                if (match[3].Value == "view")
                {
                    return new DCInsideExtractorOption { Type = ExtractorType.Images };
                }
                else if (match[3].Value == "lists")
                {
                    return new DCInsideExtractorOption { Type = ExtractorType.ArticleInformation, ExtractInformation = true };
                }
            }

            return new DCInsideExtractorOption { Type = ExtractorType.Images };
        }

        public override string RecommendFormat(IExtractorOption option)
        {
            return "%(extractor)s/%(gallery)s/%(title)s/%(file)s.%(ext)s";
        }

        public override (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option = null)
        {
            if (option == null)
                option = new DCInsideExtractorOption { Type = ExtractorType.Images };

            if ((option as DCInsideExtractorOption).OnlyRecommend)
                url += "&exception_mode=recommend";

            var match = ValidUrl.Match(url).Groups;
            var result = new List<NetTask>();
            var html = NetTools.DownloadString(url);

            if (html == null)
                return (result, null);

            if (match[1].Value == "gall")
            {
                try
                {
                    //
                    //  Parse article
                    //

                    if (match[3].Value == "view")
                    {
                        var article = ParseBoardView(html, match[2].Value != "");

                        if (option.Type == ExtractorType.Images && option.ExtractInformation == false)
                        {
                            if (article.ImagesLink == null || article.ImagesLink.Count == 0)
                            {
                                throw new Exception("Nothing to download!");
                            }

                            option.SimpleInfoCallback?.Invoke($"{article.Title}");

                            for (int i = 0; i < article.ImagesLink.Count; i++)
                            {
                                var task = NetTask.MakeDefault(article.ImagesLink[i]);
                                task.Filename = article.FilesName[i];
                                task.SaveFile = true;
                                task.Referer = url;
                                task.Format = new ExtractorFileNameFormat
                                {
                                    Id = article.Id,
                                    Gallery = article.GalleryName,
                                    Title = article.Title,
                                    FilenameWithoutExtension = (i+1).ToString("000"),
                                    Extension = Path.GetExtension(article.FilesName[i]).Replace(".", ""),
                                };
                                result.Add(task);
                            }

                            result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
                            return (result, null/*article*/);
                        }
                        else if (option.Type == ExtractorType.ArticleInformation || option.ExtractInformation == true)
                        {
                            return (null, null/*article*/);
                        }
                        else if (option.Type == ExtractorType.Comments)
                        {
                            var cc = new List<DCComment>();
                            var comments = GetComments(article, "1");
                            cc.Add(comments);

                            //
                            //  To avoid server blocks
                            //

                            Thread.Sleep(2000);

                            int tcount = comments.total_cnt;
                            int count = 100;

                            for (int i = 2; count < tcount; count += 100)
                            {
                                comments = GetComments(article, i.ToString());
                                if (comments.comment_cnt == 0)
                                    break;
                                count += comments.comment_cnt;
                                cc.Add(comments);
                                Thread.Sleep(2000);
                            }

                            return (null, null/*GetComments(article, "0")*/);
                        }
                        else
                        {
                            throw new Exception("You cannot do that with this URL. " + url);
                        }
                    }

                    //
                    //  Parse Articles List
                    //

                    else if (match[3].Value == "lists")
                    {
                        DCGallery gallery;

                        if (match[2].Value == "")
                            gallery = ParseGallery(html);
                        else
                            gallery = ParseMinorGallery(html);

                        if (option.Type == ExtractorType.GalleryInformation || option.ExtractInformation == true)
                        {
                            return (null, null/*gallery*/);
                        }
                        else
                        {
                            throw new Exception("You cannot do that with this URL." + url);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Logs.Instance.PushError("[DCInsideExtractor] Extract error - " + option.Type.ToString() + " - " + e.Message + "\r\n" + e.StackTrace);
                }
            }
            else
            {
                // Not support mobile page.
                throw new ExtractorException("[DCInside Extractor] Not support mobile page yet.");
            }

            result.ForEach(task => task.Format.Extractor = GetType().Name.Replace("Extractor", ""));
            return (result, new ExtractedInfo { Type = ExtractedInfo.ExtractedType.Community });
        }

        #region Parse for DCInside Web Site

        public static DCArticle ParseBoardView(string html, bool is_minor = false)
        {
            DCArticle article = new DCArticle();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//div[@class='view_content_wrap']")[0];

            article.Id = Regex.Match(html, @"name=""gallery_no"" value=""(\d+)""").Groups[1].Value;
            article.GalleryName = Regex.Match(html, @"<h4 class=""block_gallname"">\[(.*?) ").Groups[1].Value;
            article.OriginalGalleryName = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            if (is_minor)
                article.Class = node.SelectSingleNode("//span[@class='title_headtext']").InnerText;
            article.Contents = node.SelectSingleNode("//div[@class='writing_view_box']").InnerHtml;
            article.Title = node.SelectSingleNode("//span[@class='title_subject']").InnerText;
            try
            {
                article.ImagesLink = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").GetAttributeValue("href", "")).ToList();
                article.FilesName = node.SelectNodes("//ul[@class='appending_file']/li").Select(x => x.SelectSingleNode("./a").InnerText).ToList();
            }
            catch { }
            article.ESNO = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");

            return article;
        }

        public static DCGallery ParseGallery(string html)
        {
            DCGallery gall = new DCGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            List<DCPageArticle> pas = new List<DCPageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                int v;
                if (!int.TryParse(gall_num, out v)) continue;

                var pa = new DCPageArticle();
                pa.no = gall_num;
                pa.type = tr.SelectSingleNode("./td[2]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                pa.title = tr.SelectSingleNode("./td[2]/a").InnerText;
                try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                pa.nick = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-nick", "");
                pa.uid = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-uid", "");
                pa.ip = tr.SelectSingleNode("./td[3]").GetAttributeValue("data-ip", "");
                if (pa.ip == "")
                {
                    pa.islogined = true;
                    if (tr.SelectSingleNode("./td[3]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                        pa.isfixed = true;
                }
                pa.date = DateTime.Parse(tr.SelectSingleNode("./td[4]").GetAttributeValue("title", ""));
                pa.count = tr.SelectSingleNode("./td[5]").InnerText;
                pa.recommend = tr.SelectSingleNode("./td[6]").InnerText;

                pas.Add(pa);
            }

            gall.articles = pas.ToArray();

            return gall;
        }

        public static DCGallery ParseMinorGallery(string html)
        {
            DCGallery gall = new DCGallery();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectNodes("//tbody")[0];

            gall.id = document.DocumentNode.SelectSingleNode("//input[@id='gallery_id']").GetAttributeValue("value", "");
            gall.name = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']").GetAttributeValue("content", "");
            gall.esno = document.DocumentNode.SelectSingleNode("//input[@id='e_s_n_o']").GetAttributeValue("value", "");
            gall.cur_page = document.DocumentNode.SelectSingleNode("//div[@class='bottom_paging_box']/em").InnerText;
            try { gall.max_page = document.DocumentNode.SelectSingleNode("//a[@class='page_end']").GetAttributeValue("href", "").Split('=').Last(); } catch { }

            List<DCPageArticle> pas = new List<DCPageArticle>();

            foreach (var tr in node.SelectNodes("./tr"))
            {
                try
                {
                    var gall_num = tr.SelectSingleNode("./td[1]").InnerText;
                    int v;
                    if (!int.TryParse(gall_num, out v)) continue;

                    var pa = new DCPageArticle();
                    pa.no = gall_num;
                    pa.classify = tr.SelectSingleNode("./td[2]").InnerText;
                    pa.type = tr.SelectSingleNode("./td[3]/a/em").GetAttributeValue("class", "").Split(' ')[1];
                    pa.title = tr.SelectSingleNode("./td[3]/a").InnerText;
                    try { pa.replay_num = tr.SelectSingleNode(".//span[@class='reply_num']").InnerText; } catch { }
                    pa.nick = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-nick", "");
                    pa.uid = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-uid", "");
                    pa.ip = tr.SelectSingleNode("./td[4]").GetAttributeValue("data-ip", "");
                    if (pa.ip == "")
                    {
                        pa.islogined = true;
                        if (tr.SelectSingleNode("./td[4]/a/img").GetAttributeValue("src", "").Contains("fix_nik.gif"))
                            pa.isfixed = true;
                    }
                    pa.date = DateTime.Parse(tr.SelectSingleNode("./td[5]").GetAttributeValue("title", ""));
                    pa.count = tr.SelectSingleNode("./td[6]").InnerText;
                    pa.recommend = tr.SelectSingleNode("./td[7]").InnerText;

                    pas.Add(pa);
                }
                catch { }
            }

            gall.articles = pas.ToArray();

            return gall;
        }

        public static DCComment GetComments(DCArticle article, string page)
        {
            var wc = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            wc.Headers = new Dictionary<string, string>();
            wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
            wc.Query = new Dictionary<string, string>();
            wc.Query.Add("id", article.OriginalGalleryName);
            wc.Query.Add("no", article.Id);
            wc.Query.Add("cmt_id", article.OriginalGalleryName);
            wc.Query.Add("cmt_no", article.Id);
            wc.Query.Add("e_s_n_o", article.ESNO);
            wc.Query.Add("comment_page", page);
            wc.Query.Add("sort", "");
            return JsonConvert.DeserializeObject<DCComment>(NetTools.DownloadStringAsync(wc).Result);
        }

        public static DCComment GetComments(DCGallery g, DCPageArticle article, string page)
        {
            var wc = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            wc.Headers = new Dictionary<string, string>();
            wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
            wc.Query = new Dictionary<string, string>();
            wc.Query.Add("id", g.id);
            wc.Query.Add("no", article.no);
            wc.Query.Add("cmt_id", g.id);
            wc.Query.Add("cmt_no", article.no);
            wc.Query.Add("e_s_n_o", g.esno);
            wc.Query.Add("comment_page", page);
            return JsonConvert.DeserializeObject<DCComment>(NetTools.DownloadStringAsync(wc).Result);
        }

        #endregion
    }
}
