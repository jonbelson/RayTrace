using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public abstract class Skydome : Entity
    {
        public Skydome() { }

        public abstract Colour GetColour(Camera camera, SVector ray);

        public new abstract XMLSkydome ToXML();
    }
}
