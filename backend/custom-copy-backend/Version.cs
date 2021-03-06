﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Text;

namespace custom_copy_backend
{
    public class Version
    {
        public const int MajorVersion = 2020;
        public const int MinorVersion = 04;
        public const int BuildVersion = 11;

        public const string Name = "Custom Copy Project";
        public static string Text { get; } = $"{MajorVersion}.{MinorVersion}.{BuildVersion}";
    }
}
