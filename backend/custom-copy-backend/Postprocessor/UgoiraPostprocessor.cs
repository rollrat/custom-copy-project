﻿// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using custom_copy_backend.Extractor;
using custom_copy_backend.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace custom_copy_backend.Postprocessor
{
    /// <summary>
    /// Postprocessor for pixiv-ugoria(zip) to gif.
    /// </summary>
    public class UgoiraPostprocessor : IPostprocessor
    {
        public List<PixivExtractor.PixivAPI.UgoiraFrames> Frames;

        public override void Run(NetTask task)
        {
            ugoira2gif(task);
        }

        private void ugoira2gif(NetTask task)
        {
            Log.Logs.Instance.Push("[Postprocessor] Start ugoira to gif... " + task.Filename);

            using (var file = File.OpenRead(task.Filename))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            using (var entry = zip.GetEntry(Frames[0].File).Open())
            using (var first_image = Image.Load(entry))
            {
                for (int i = 1; i < Frames.Count; i++)
                {
                    using (var ientry = zip.GetEntry(Frames[i].File).Open())
                    using (var iimage = Image.Load(ientry))
                    {
                        var frame = iimage.Frames.RootFrame;

                        frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = Frames[i].Delay.Value / 10;
                        first_image.Frames.AddFrame(frame);
                    }
                }

                first_image.Save(Path.Combine(Path.GetDirectoryName(task.Filename), Path.GetFileName(task.Filename).Split('_')[0] + ".gif"), new GifEncoder());
            }

            File.Delete(task.Filename);
        }

        private void ugoira2webp(NetTask task)
        {

        }
    }
}
