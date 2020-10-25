using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RayTrace
{
    public class EntityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CameraTemplate { get; set; }
        public DataTemplate SphereTemplate { get; set; }
        public DataTemplate PlaneTemplate { get; set; }
        public DataTemplate ModelTemplate { get; set; }
        public DataTemplate DistantLightTemplate { get; set; }
        public DataTemplate PointLightTemplate { get; set; }
        public DataTemplate SpotLightTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Entity entity = (Entity)item;

            if (entity is Camera) return CameraTemplate;
            if (entity is Sphere) return SphereTemplate;
            if (entity is Plane) return PlaneTemplate;
            if (entity is Model) return ModelTemplate;
            if (entity is DistantLight) return DistantLightTemplate;
            if (entity is SpotLight) return SpotLightTemplate;
            if (entity is PointLight) return PointLightTemplate;
            //if (entity is Light) return LightTemplate;

            //Type type = entity.GetType();
            //switch (type)
            //{

            //}

            return base.SelectTemplate(item, container);
        }
    }
}
