// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Text;

namespace custom_copy_backend.Component.Hitomi
{
    public struct HitomiTagdata
    {
        public string Tag { get; set; }
        public int Count { get; set; }
    }

    public struct HitomiTagdataCollection
    {
        public List<HitomiTagdata> language { get; set; }
        public List<HitomiTagdata> female { get; set; }
        public List<HitomiTagdata> series { get; set; }
        public List<HitomiTagdata> character { get; set; }
        public List<HitomiTagdata> artist { get; set; }
        public List<HitomiTagdata> group { get; set; }
        public List<HitomiTagdata> tag { get; set; }
        public List<HitomiTagdata> male { get; set; }
        public List<HitomiTagdata> type { get; set; }
    }
}
