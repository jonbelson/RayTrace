// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Skydomes
{
    public abstract class Skydome : Entity
    {
        public Skydome() { }

        public abstract SColour GetColour(Camera camera, SVector ray);

        public new abstract XMLSkydome ToXML();
    }
}
