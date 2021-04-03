using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RayTrace
{
    public class MaterialTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DiffuseTemplate { get; set; }
        public DataTemplate PhongTemplate { get; set; }
        public DataTemplate ReflectiveTemplate { get; set; }
        public DataTemplate RefractiveAndReflectiveTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
/*
            MaterialType type = (MaterialType)item;

            if (type == MaterialType.Diffuse) return DiffuseTemplate;
            if (type == MaterialType.Phong) return PhongTemplate;
            if (type == MaterialType.Reflective) return ReflectiveTemplate;
            if (type == MaterialType.RefractiveAndReflective) return RefractiveAndReflectiveTemplate;
*/

//*
            Material material = (Material)item;

            if (material.Type == MaterialType.Diffuse) return DiffuseTemplate;
            if (material.Type == MaterialType.Phong) return PhongTemplate;
            if (material.Type == MaterialType.Reflective) return ReflectiveTemplate;
            if (material.Type == MaterialType.RefractiveAndReflective) return RefractiveAndReflectiveTemplate;
//*/
            /*
                        if (entity is Sphere) return SphereTemplate;
                        if (entity is Plane) return PlaneTemplate;
                        if (entity is Model) return ModelTemplate;
                        if (entity is DistantLight) return DistantLightTemplate;
                        if (entity is SpotLight) return SpotLightTemplate;
                        if (entity is PointLight) return PointLightTemplate;
            */

            //if (entity is Light) return LightTemplate;

            //Type type = entity.GetType();
            //switch (type)
            //{

            //}

            return base.SelectTemplate(item, container);
        }
    }
}
