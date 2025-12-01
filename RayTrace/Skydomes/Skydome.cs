using System;
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
