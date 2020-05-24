using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class Camera
    {
        private Point pos = new Point(0, 0, 0);
        private Point target = new Point(0, 0, 1.0f);

        public Point Pos { get { return pos; } set { pos = value; UpdateMatrix(); } }
        public Point Target { get { return target; } set { target = value; UpdateMatrix(); } }

        // Matrix for World to View transform.
        public Matrix WorldToView { get; set; } = new Matrix();

        // Matrix for transforming World normals to View.
        public Matrix NormalMatrix { get; set; } = new Matrix();

        private void UpdateMatrix()
        {
            WorldToView.SetLookAt(pos, target);

            NormalMatrix = WorldToView;
            NormalMatrix.M14 = 0.0;
            NormalMatrix.M24 = 0.0;
            NormalMatrix.M34 = 0.0;
        }

        public Camera(Point from, Point to)
        {
            pos = from;
            target = to;
            UpdateMatrix();
        }
    }
}
