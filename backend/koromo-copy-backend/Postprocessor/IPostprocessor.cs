// This source code is a part of Koromo Copy Project.
// Copyright (C) 2020. rollrat. Licensed under the MIT Licence.

using koromo_copy_backend.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace koromo_copy_backend.Postprocessor
{
    /// <summary>
    /// Postprocessor interface
    /// </summary>
    public abstract class IPostprocessor
    {
        public abstract void Run(NetTask task);
    }
}
