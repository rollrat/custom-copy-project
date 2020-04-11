// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace koromo_copy_backend.Script
{
    public class ScriptManager : ILazy<ScriptManager>
    {
        public List<KoromoScriptInstance> Script { get; set; }

        public void Initialization()
        {
            var script_dir = Path.Combine(AppProvider.ApplicationPath, "script");

            if (!Directory.Exists(script_dir))
            {
                Directory.CreateDirectory(script_dir);
            }

            Script = new List<KoromoScriptInstance>();
            foreach (var file in Directory.GetFiles(script_dir).Where(x => x.EndsWith(".js")))
            {
                var instance = KoromoScriptInstance.CreateNewInstance();
                instance.Load(file);
                Script.Add(instance);
            }
        }
    }
}
