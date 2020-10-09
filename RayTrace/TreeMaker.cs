using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Represents a single node in a space partitioning tree.
    public class Node
    {
        public Node Left { get; set; } = null;
        public Node Right { get; set; } = null;

        public List<int> Triangles = new List<int>();

        public BoundingBox Box { get; set; }

        public Node() { }
    }

    public class TreeMaker
    {
        public int MaxDepth { get; set; } = 10;

        private Matrix _objectToView = new Matrix();

        private Point GetTrianglePointV0(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v0];
        private Point GetTrianglePointV1(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v1];
        private Point GetTrianglePointV2(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v2];

        // Get centroid of all vertices for specified triangles.
        private Point GetCentroid(ModelData modelData, List<int> triangles)
        {
            Point p = new Point();

            foreach (int i in triangles)
            {
                Point p0 = GetTrianglePointV0(modelData, i);
                Point p1 = GetTrianglePointV1(modelData, i);
                Point p2 = GetTrianglePointV2(modelData, i);

                p.X += p0.X + p1.X + p2.X;
                p.Y += p0.Y + p1.Y + p2.Y;
                p.Z += p0.Z + p1.Z + p2.Z;
            }

            int numPoints = triangles.Count() * 3;  // Three vertices for each triangle.

            return p.Scale(1.0/numPoints);
        }

        // Get BoundingBox containing all vertices from specified triangles.
        private BoundingBox GetBoundingBox(ModelData modelData, List <int> triangles)
        {
            BoundingBox bb = new BoundingBox();
            foreach (int i in triangles)
            {
                bb.AddPoint(GetTrianglePointV0(modelData, i));
                bb.AddPoint(GetTrianglePointV1(modelData, i));
                bb.AddPoint(GetTrianglePointV2(modelData, i));
            }

            return bb;
        }

        private enum Side { GreaterThan, LessThan, Overlap };

        private Side CheckSideX(ModelData modelData, int index, double threshold)
        {
            int count = 0;
            if (GetTrianglePointV0(modelData, index).X > threshold) count++;
            if (GetTrianglePointV1(modelData, index).X > threshold) count++;
            if (GetTrianglePointV2(modelData, index).X > threshold) count++;
            if (count == 0)
                return Side.LessThan;
            else if (count == 3)
                return Side.GreaterThan;

            return Side.Overlap;
        }

        private Side CheckSideY(ModelData modelData, int index, double threshold)
        {
            int count = 0;
            if (GetTrianglePointV0(modelData, index).Y > threshold) count++;
            if (GetTrianglePointV1(modelData, index).Y > threshold) count++;
            if (GetTrianglePointV2(modelData, index).Y > threshold) count++;
            if (count == 0)
                return Side.LessThan;
            else if (count == 3)
                return Side.GreaterThan;

            return Side.Overlap;
        }

        private Side CheckSideZ(ModelData modelData, int index, double threshold)
        {
            int count = 0;
            if (GetTrianglePointV0(modelData, index).Z > threshold) count++;
            if (GetTrianglePointV1(modelData, index).Z > threshold) count++;
            if (GetTrianglePointV2(modelData, index).Z > threshold) count++;
            if (count == 0)
                return Side.LessThan;
            else if (count == 3)
                return Side.GreaterThan;

            return Side.Overlap;
        }

        private void AddToList(Side side, int i, List<int> left, List<int> right)
        {
            if (side == Side.LessThan) left.Add(i);
            else if (side == Side.GreaterThan) right.Add(i);
            else if (side == Side.Overlap) { left.Add(i); right.Add(i); }
        }

        // Recursively build tree by dividing the bounding box along the longest axis and splitting the triangles between the two halves.
        private Node MakeNode(ModelData modelData, List<int> triangles, int depth)
        {
            Node node = new Node();

            BoundingBox bb = GetBoundingBox(modelData, triangles);
            node.Box = bb;
            node.Triangles = triangles;

            // Don't bother subdividing further if there are too few triangles.
            if (triangles.Count < 10)
            {
                return node;
            }

            List<int> left = new List<int>();
            List<int> right = new List<int>();

            // Use the centre point of the BB as a threshold for subdividing. Would be better to
            //  use the centre of the triangle points, but this will do for now.
            //Point centre = bb.GetCentre();
            Point centre = GetCentroid(modelData, triangles);

            var longestAxis = bb.GetLongestAxis();
            switch (longestAxis)
            {
                case BoundingBox.Axis.X:
                    foreach (int i in triangles)
                    {
                        var side = CheckSideX(modelData, i, centre.X);
                        AddToList(side, i, left, right);
                    }
                    break;

                case BoundingBox.Axis.Y:
                    foreach (int i in triangles)
                    {
                        var side = CheckSideY(modelData, i, centre.Y);
                        AddToList(side, i, left, right);
                    }
                    break;

                case BoundingBox.Axis.Z:
                    foreach (int i in triangles)
                    {
                        var side = CheckSideZ(modelData, i, centre.Z);
                        AddToList(side, i, left, right);
                    }
                    break;
            }

            // If left and right have > 50% in common, don't split further.
            var intersect = left.Intersect(right).ToList();
            if (intersect.Count > left.Count/2 && intersect.Count > right.Count/2)
            {
                return node;
            }

            depth++;
            if (depth < MaxDepth)
            {
                // Create child Nodes if there are any triangles.
                if (left.Count() > 0) node.Left = MakeNode(modelData, left, depth);
                if (right.Count() > 0) node.Right = MakeNode(modelData, right, depth);
            }

            return node;
        }

        public Node MakeTree(ModelData modelData, Matrix objectToView)
        {
            _objectToView = objectToView;

            List<int> triangles = new List<int>();

            // Generate initial indices.
            for (int i = 0; i < modelData.triangles.Length; i++)
            {
                triangles.Add(i);
            }

            Node root = MakeNode(modelData, triangles, 0);

            return root;
        }
    }
}
