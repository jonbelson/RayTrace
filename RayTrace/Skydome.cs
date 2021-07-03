using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    abstract public class Skydome
    {
        public Skydome() { }

        public abstract Colour GetColour(Camera camera, Vector ray);
    }
}
