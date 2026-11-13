// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Simple texture that just returns asingle colour.
    public class TextureSolid : Texture, INotifyPropertyChanged
    {
        public override TextureType Type { get; } = TextureType.Solid;

        public Colour Colour { get;
            set
            {
                if (field != value)
                {
                    field?.PropertyChanged -= Colour_PropertyChanged;

                    field = value;

                    field?.PropertyChanged += Colour_PropertyChanged;

                    Notify();
                }
            }
        }

        private void Colour_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify("Colour");
        }

        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        //
        // Constructor.
        //
        public TextureSolid() : this(Palette.Grey) { }

        //
        // Constructor.
        //
        public TextureSolid(Colour colour)
        {
            Name = "Solid";

            Colour = colour;
        }

        //
        // Constructor.
        //
        public TextureSolid(XMLTextureSolid xmlTextureSolid)
        {
            Colour = new Colour(xmlTextureSolid.Colour);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override SColour GetSColour(HitResult hr) { return Colour.ToSColour(); }

        public override Colour GetBaseColour() { return Colour; }

        public override XMLTexture ToXML()
        {
            XMLTextureSolid xml = new XMLTextureSolid();
            xml.Colour = Colour.ToXML();
            return xml;
        }
    }
}
