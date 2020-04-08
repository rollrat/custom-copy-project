// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace koromo_copy_backend.Component.DCInside
{
    public class DCInsideManager
    {
        public static string ESNO { get; set; }
        public static SortedDictionary<string, string> Galleries { get; set; }
        public static SortedDictionary<string, string> MinorGalleries { get; set; }

        public static void Initialize()
        {
            ESNO = DCInsideParser.ParseGallery(NetTools.DownloadString("https://gall.dcinside.com/board/lists?id=hit")).esno;
        }
    }
}
