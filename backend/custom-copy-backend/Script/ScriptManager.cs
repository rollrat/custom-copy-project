// This source code is a part of Custom Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using custom_copy_backend.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace custom_copy_backend.Script
{
    public class ScriptManager : ILazy<ScriptManager>
    {
        public List<CustomScriptInstance> Script { get; set; }

        public CustomScriptInstance CommonEnvironment { get; set; }

        public void Initialization()
        {
            var script_dir = Path.Combine(AppProvider.ApplicationPath, "script");

            if (!Directory.Exists(script_dir))
            {
                Directory.CreateDirectory(script_dir);
            }

            Script = new List<CustomScriptInstance>();
            //var CommonEnvironment = CustomScriptInstance.CreateNewInstance();
            foreach (var file in Directory.GetFiles(script_dir).Where(x => x.EndsWith(".js")))
            {
                var instance = CustomScriptInstance.CreateNewInstance();
                instance.Load(file);
                Script.Add(instance);
            }
        }
    }
}
