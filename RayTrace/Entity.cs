// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public abstract class Entity : INotifyPropertyChanged
    {
        private bool enabled = true;

        public bool Enabled {
            get => enabled;
            set {
                if (enabled != value)
                {
                    enabled = value;
                    EnabledStateChanged();
                    Notify("Enabled");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void EnabledStateChanged() { }

        protected void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); 
        }

        public virtual void AddPropertyChangedHandler(PropertyChangedEventHandler eventHandler)
        {
            PropertyChanged += eventHandler;
        }

        public Guid UID { get; /*set;*/ } = Guid.NewGuid();

        public Scene Scene { get; set; }

        public string Name { get; set; }
        
        //
        // Constructor
        //
        public Entity()
        {
            Name = GetType().Name;
        }

        //
        // Constructor
        //
        public Entity(XMLEntity xmlEntity)
        {
            UID = xmlEntity.UID;
            Name = xmlEntity.Name;
        }

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public virtual void CreateCache(Camera camera) { }

        public abstract HitResult HitTest(Camera camera, Ray ray);

        protected virtual void AsXMLImpl(XMLEntity xml)
        {
            xml.UID = UID;
            xml.Enabled = Enabled;
            xml.Name = Name;
        }

        public virtual XMLEntity ToXML()
        {
            XMLEntity xml = new();
            AsXMLImpl(xml);
            return xml;
        }

        public virtual TreeViewEntityModel ToTreeModel()
        {
            return new TreeViewEntityModel() { Name = Name, Instance = this };
        }
    }
}
