using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RayTrace
{
    public class TextureTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextureSolidTemplate { get; set; }
        public DataTemplate TextureSquaresTemplate { get; set; }
        public DataTemplate TextureNoiseTemplate { get; set; }
        //public DataTemplate ReflectiveTemplate { get; set; }
        //public DataTemplate RefractiveAndReflectiveTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TextureSolid) return TextureSolidTemplate;
            if (item is TextureSquares) return TextureSquaresTemplate;
            if (item is TextureNoise) return TextureNoiseTemplate;

            /*
                        MaterialType type = (MaterialType)item;

                        if (type == MaterialType.Diffuse) return DiffuseTemplate;
                        if (type == MaterialType.Phong) return PhongTemplate;
                        if (type == MaterialType.Reflective) return ReflectiveTemplate;
                        if (type == MaterialType.RefractiveAndReflective) return RefractiveAndReflectiveTemplate;
            */

            /*
                        if (entity is Sphere) return SphereTemplate;
                        if (entity is Plane) return PlaneTemplate;
                        if (entity is Model) return ModelTemplate;
                        if (entity is DistantLight) return DistantLightTemplate;
                        if (entity is SpotLight) return SpotLightTemplate;
                        if (entity is PointLight) return PointLightTemplate;
            */

            //if (entity is Light) return LightTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
