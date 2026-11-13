// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Class to represent a group/object of a Model, required to handle the case
    //  where it has a different Material.
    public class ModelSubGroup : Entity
    {
        private readonly Model _model;
        private readonly ModelData _modelData;

        private ModelData.SubGroup _group;

        public ModelData.SubGroup Group { get => _group; }

        public List<ModelSubGroup> Children { get; } = [];

        public bool HasMaterial() => Material != null;
        public bool HasRoughness() => HasMaterial() && Material.HasRoughness();
        public bool HasTransparency() => HasMaterial() && Material.HasTransparency();
        public bool HasReflectivity() => HasMaterial() && Material.HasReflectivity();
        public bool HasNormalMap() => HasMaterial() && Material.HasNormalMap();

        protected override void EnabledStateChanged()
        {
            //_model.SetGroupEnabled();
            _group.Enabled = Enabled;
        }

        public Material Material { get; set; }

        //
        // Constructor
        //
        public ModelSubGroup(Model owner, ModelData.SubGroup group)
        {
            _model = owner;
            _modelData = owner.GetModelData();
            _group = group;
            Name = group.Name;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            throw new NotImplementedException();
        }
    }
}
