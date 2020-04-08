// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.Network;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace koromo_copy_backend.Component.DCInside
{
    public class DCInsideUtils
    {
        public static async Task<DCInsideComment> GetComments(DCInsideArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", article.OriginalGalleryName },
                { "no", article.Id },
                { "cmt_id", article.OriginalGalleryName },
                { "cmt_no", article.Id },
                { "e_s_n_o", article.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(DCInsideGallery g, DCInsidePageArticle article, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", g.id },
                { "no", article.no },
                { "cmt_id", g.id },
                { "cmt_no", article.no },
                { "e_s_n_o", g.esno },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<DCInsideComment> GetComments(string gall_id, string article_id, string page)
        {
            var nt = NetTask.MakeDefault("https://gall.dcinside.com/board/comment/");
            nt.Headers = new Dictionary<string, string>() { { "X-Requested-With", "XMLHttpRequest" } };
            nt.Query = new Dictionary<string, string>()
            {
                { "id", gall_id },
                { "no", article_id },
                { "cmt_id", gall_id },
                { "cmt_no", article_id },
                { "e_s_n_o", DCInsideManager.ESNO },
                { "comment_page", page }
            };
            return JsonConvert.DeserializeObject<DCInsideComment>(await NetTools.DownloadStringAsync(nt));
        }

        public static async Task<List<DCInsideCommentElement>> GetAllComments(string gall_id, string article_id)
        {
            var first = await GetComments(gall_id, article_id, "1");
            TidyComments(ref first);
            if (first.comments.Count == first.total_cnt)
                return first.comments.ToList();
            var cur = first.comments.Count;
            int iter = 2;
            while (cur < first.total_cnt)
            {
                var ll = await GetComments(gall_id, article_id, iter++.ToString());
                TidyComments(ref ll);
                cur += ll.comments.Count;
                first.comments.AddRange(ll.comments);
            }
            return first.comments;
        }

        public static void TidyComments(ref DCInsideComment comment)
        {
            for (int i = 0; i < comment.comments.Count; i++)
                if (comment.comments[i].nicktype == "COMMENT_BOY")
                    comment.comments.RemoveAt(i--);
        }

        public static SortedDictionary<string, string> GetGalleryList()
        {
            var dic = new SortedDictionary<string, string>();
            var src = NetTools.DownloadString("http://wstatic.dcinside.com/gallery/gallindex_iframe_new_gallery.html");

            var parse = new List<Match>();
            parse.AddRange(Regex.Matches(src, @"onmouseover=""gallery_view\('(\w+)'\);""\>[\s\S]*?\<.*?\>([\w\s]+)\<").Cast<Match>().ToList());
            parse.AddRange(Regex.Matches(src, @"onmouseover\=""gallery_view\('(\w+)'\);""\>\s*([\w\s]+)\<").Cast<Match>().ToList());
            foreach (var match in parse)
            {
                var identification = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();

                if (!string.IsNullOrEmpty(name))
                {
                    if (name[0] == '-')
                        name = name.Remove(0, 1).Trim();
                    if (!dic.ContainsKey(name))
                        dic.Add(name, identification);
                }
            }

            return dic;
        }

        public static SortedDictionary<string, string> GetMinorGalleryList()
        {
            return JsonConvert.DeserializeObject<SortedDictionary<string, string>>(NetTools.DownloadString("https://raw.githubusercontent.com/dc-koromo/koromo-copy-windows/master/dcinside-minor-gallery.json"));
        }

        public static SortedDictionary<string, string> GetMinorGalleryListRaw()
        {
            var dic = new SortedDictionary<string, string>();
            var html = NetTools.DownloadString("https://gall.dcinside.com/m");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            foreach (var a in document.DocumentNode.SelectNodes("//a[@onmouseout='thumb_hide();']"))
                dic.Add(a.InnerText.Trim(), a.GetAttributeValue("href", "").Split('=').Last());

            var under_name = new List<string>();
            foreach (var b in document.DocumentNode.SelectNodes("//button[@class='btn_cate_more']"))
                under_name.Add(b.GetAttributeValue("data-lyr", ""));

            var htmls = NetTools.DownloadStrings(under_name.Select(un => $"https://wstatic.dcinside.com/gallery/mgallindex_underground/{un}_new.html").ToList());
            foreach (var subhtml in htmls)
            {
                HtmlDocument document2 = new HtmlDocument();
                document2.LoadHtml(subhtml);
                foreach (var c in document2.DocumentNode.SelectNodes("//a[@class='list_title']"))
                    if (!dic.ContainsKey(c.InnerText.Trim()))
                        dic.Add(c.InnerText.Trim(), c.GetAttributeValue("href", "").Split('=').Last());
            }

            return dic;
        }
    }
}
