﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using custom_copy_backend.CL;
using custom_copy_backend.Network;
using custom_copy_backend.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace custom_copy_backend.Extractor
{
    public class ExtractorException : Exception
    {
        public ExtractorException(string msg)
            : base(msg) { }
    }

    public class IExtractorOption : IConsoleOption
    {
        public enum ExtractorType
        {
            Images = 0, // Default
            Comments = 1,
            GalleryInformation = 2,
            ArticleInformation = 3,
            EpisodeImages = 4,
            ComicIndex = 5,
            Works = 6,
        }

        public ExtractorType Type;

        public bool ExtractInformation { get; set; }
        public Action<string> PageReadCallback;

        public Action<long> ProgressMax { get; set; }
        public Action<long> PostStatus { get; set; }

        public Action<string> SimpleInfoCallback { get; set; }
        public Action<NetTask> ThumbnailCallback { get; set; }

        public int StartPage = 0;
        public int EndPage = int.MaxValue;

        public virtual void CLParse(ref IExtractorOption model, string[] args) { }
    }

    public class DownloadInfo
    {
        public DateTime DownloadStarts;
        public DateTime DownloadEnds;
        public bool DownloadAborted;
    }

    /// <summary>
    /// Reference: https://github.com/dc-custom/custom-copy/blob/master/Document/General.md
    /// </summary>
    public class ExtractedInfo
    {
        public enum ExtractedType
        {
            Search,
            UserArtist,
            Group,
            WorksComic,
            Community
        }

        public abstract class IInfo
        {
            public string URL;
            public NetTask Thumbnail;
            public string ShortInfo;
        }

        public class Search : IInfo
        {
            public string SearchToken;
            public string ResultCount;
        }

        public class UserArtist : IInfo
        {
            public string RealName;
            public string NickName;
            public string Id;
            public string UploadCount;
            public string FirstAccessTime;
            public string LastAccessTime;
            public UserArtist[] Connecitons;
            public UserArtist[] Follows;
            public string[] SNSLinks;
            public string[] Tags;
        }

        public class Group : IInfo
        {
            public string GroupName;
            public UserArtist[] Members;
        }

        public class WorksComic : IInfo
        {
            public string Title;
            public string[] Author;
            public string[] AuthorGroup;
            public bool IsOmnibus;
            public string FirstPublished;
            public bool IsCompletion;
            public string LastPublished;
            public string[] Tags;
            public string[] Parodies;
            public string[] Characters;
            public string Language;
            public class Episode
            {
                public string URL;
                public string EpisodeNumber;
                public string Title;
            }
            public Episode[] Episodes;
        }

        public class Community : IInfo
        {
            public class Board
            {
                public string URL;
                public string BoardName;
                public string BoardId;
                public string[] Tags;

                public class Article
                {
                    public string URL;
                    public string Title;
                    public string Writer;
                    public string WriteTime;
                    public string Views;
                    public string UpVote;
                    public string DownVote;
                    public string[] Tags;
                }
                public Article[] Articles;
            }
            public Board[] BoardInfo;
        }

        public ExtractedType Type;
        public IInfo Info;
    }

    public abstract class ExtractorModel
    {
        public Regex HostName { get; protected set; }
        public Regex ValidUrl { get; protected set; }
        public string ExtractorInfo { get; protected set; }
        public bool IsForbidden { get; protected set; }

        public abstract IExtractorOption RecommendOption(string url);
        public abstract string RecommendFormat(IExtractorOption option);
        public abstract (List<NetTask>, ExtractedInfo) Extract(string url, IExtractorOption option);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ExtractorFileNameFormat
    {
        [JsonProperty]
        public Dictionary<string, string> Format { get; set; }
            = new Dictionary<string, string>();

        static Dictionary<string, string> ShortTerm;

        public ExtractorFileNameFormat()
        {
            if (ShortTerm == null)
            {
                ShortTerm = new Dictionary<string, string>();

                Type type = typeof(ExtractorFileNameFormat);
                PropertyInfo[] fields = type.GetProperties();

                foreach (var p in fields)
                {
                    object[] attrs = p.GetCustomAttributes(false);

                    foreach (var at in attrs)
                    {
                        var atcast = at as attr;
                        if (atcast != null)
                        {
                            if (atcast.ShortOption != "")
                                ShortTerm.Add(atcast.ShortOption, crop(p.Name));
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Path Formatting based on Python String Formatting Style
        /// https://docs.python.org/2/library/stdtypes.html#string-formatting
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public string Formatting(string raw)
        {
            var builder = new StringBuilder(raw.Length);

            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '%')
                {
                    i++;

                    if (raw[i] == '%')
                    {
                        builder.Append('%');
                        continue;
                    }

                    if (raw[i++] != '(')
                        throw new Exception("Filename formatting error! pos=" + i);
                    
                    var tokenb = new StringBuilder(10);

                    for (; i < raw.Length; i++) 
                    {
                        if (raw[i] == ')')
                        {
                            i++;
                            break;
                        }
                        
                        tokenb.Append(raw[i]);
                    }

                    var token = tokenb.ToString().ToLower();
                    string literal;

                    if (Format.ContainsKey(token))
                        literal = Format[token];
                    else if (ShortTerm.ContainsKey(token) && Format.ContainsKey(ShortTerm[token]))
                        literal = Format[ShortTerm[token]];
                    else
                        throw new Exception($"Error token {token} not found!");

                    var pp = new StringBuilder(5);
                    var type = 's';
                    
                    for (; i < raw.Length; i++)
                    {
                        if (char.IsLetter(raw[i]))
                        {
                            type = raw[i];
                            break;
                        }
                        pp.Append(raw[i]);
                    }

                    var pptk = pp.ToString();

                    if (type == 's')
                    {
                        if (pptk != "")
                            builder.Append(literal.Substring(0, pptk.ToInt()));
                        else
                            builder.Append(literal.ToString());
                    }
                    else if (type == 'd')
                    {
                        builder.Append(literal.ToInt().ToString(pptk));
                    }
                    else if (type == 'x' || type == 'X')
                    {
                        builder.Append(literal.ToInt().ToString(type + pptk));
                    }
                    else if (type == 'f')
                    {
                        builder.Append(float.Parse(literal).ToString(pptk));
                    }
                }
                else
                    builder.Append(raw[i]);
            }

            var result = builder.ToString().Replace('|', 'ㅣ');

            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid) if (c != '/' && c!= '\\') result = result.Replace(c.ToString(), "");

            return result;
        }

        private static string crop(string pp)
        {
            var builder = new StringBuilder();

            foreach (var ch in pp)
            {
                if (char.IsUpper(ch))
                {
                    if (builder.Length != 0)
                        builder.Append("_");
                    builder.Append(char.ToLower(ch));
                }
                else
                    builder.Append(ch);
            }

            return builder.ToString();
        }

        private string check_getter(string pp)
        {
            var cc = crop(pp.Replace("get_", ""));
            if (Format.ContainsKey(cc))
                return Format[cc];
            return null;
        }

        private void check_setter(string pp, string value)
        {
            var cc = crop(pp.Replace("set_", ""));
            if (Format.ContainsKey(cc))
                Format[cc] = value;
            else
                Format.Add(cc, value);
        }

        [AttributeUsage(AttributeTargets.Property)]
        class attr : Attribute
        {
            public string ShortOption { get; set; }
        }

        public string Extractor { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Title { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string OriginalTitle { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Id { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string User { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Account { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Author { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string EnglishAuthor { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Artist { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Group { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Search { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string UploadDate { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Uploader { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string UploaderId { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Character { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Gallery { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Series { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string SeasonNumber { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Season { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Episode { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string EpisodeNumber { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        [attr(ShortOption = "file")]
        public string FilenameWithoutExtension { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        [attr(ShortOption = "ext")]
        public string Extension { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Url { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string License { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Genre { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
        public string Language { get { return check_getter(MethodBase.GetCurrentMethod().Name); } set { check_setter(MethodBase.GetCurrentMethod().Name, value); } }
    }

    public class ExtractorManager : ILazy<ExtractorManager>
    {
        public static ExtractorModel[] Extractors =
        {
            new DCInsideExtractor(),
            new PixivExtractor(),
            new GelbooruExtractor(),
            new NaverExtractor(),
            new EHentaiExtractor(),
            new HitomiExtractor(),
            new InstagramExtractor(),
            new ExHentaiExtractor(),
            new ManamoaExtractor(),
            new ImgurExtractor(),
            new TwitterExtractor(),
            new HiyobiExtractor(),
            new DanbooruExtractor(),
            new FunbeExtractor(),
            new JmanaExtractor(),
            new Scrap.HaneulHaneulExtractor(),
            new Scrap.AttrangsExtractor(),
        };

        public ExtractorModel GetExtractor(string url)
        {
            foreach (var em in Extractors)
            {
                if (em.ValidUrl.IsMatch(url))
                    return em;
            }
            return null;
        }

        public ExtractorModel GetExtractorFromHostName(string url)
        {
            foreach (var em in Extractors)
            {
                if (em.HostName.IsMatch(url))
                    return em;
            }
            return null;
        }

        public void Formatting(ExtractorModel extractor, ref List<NetTask> tasks, IExtractorOption option)
        {
            var ff = extractor.RecommendFormat(option);
            foreach (var task in tasks)
                task.Filename = task.Format.Formatting(ff);
        }
    }
}
