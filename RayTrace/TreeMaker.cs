using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    //
    // Create a binary space partition tree for a Model's data.
    //
    public class TreeMaker
    {
        public int MaxDepth { get; set; } = 40;
        public int MinTrianglesPerNode { get; set; } = 10;

        private Matrix _objectToView = new Matrix();

        private SPoint GetTrianglePointV0(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v0];
        private SPoint GetTrianglePointV1(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v1];
        private SPoint GetTrianglePointV2(ModelData modelData, int index) => _objectToView * modelData.points[modelData.triangles[index].v2];

        // Get centroid of all vertices for specified triangles.
        private Point GetCentroid(ModelData modelData, List<int> triangles)
        {
            Point p = new Point();

            foreach (int i in triangles)
            {
                SPoint p0 = GetTrianglePointV0(modelData, i);
                SPoint p1 = GetTrianglePointV1(modelData, i);
                SPoint p2 = GetTrianglePointV2(modelData, i);

                p.X += p0.X + p1.X + p2.X;
                p.Y += p0.Y + p1.Y + p2.Y;
                p.Z += p0.Z + p1.Z + p2.Z;
            }

            int numPoints = triangles.Count() * 3;  // Three vertices for each triangle.

            return p.Scale(1.0f/numPoints);
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

/*
        private BoundingBox SplitBoundingBox(BoundingBox parent, BoundingBox.Axis axis, bool left)
        {
            BoundingBox bb = new(parent.MinX, parent.MaxX, parent.MinY, parent.MaxY, parent.MinZ, parent.MaxZ);

            float width = bb.GetWidth();
            float height = bb.GetHeight();
            float depth = bb.GetDepth();

            switch (axis)
            {
                case BoundingBox.Axis.X:
                    if (left)
                    {
                        bb.MaxX = bb.MinX + width/2;
                    }
                    else
                    {
                        bb.MinX = bb.MaxX - width/2;
                    }
                    break;
                case BoundingBox.Axis.Y:
                    if (left)
                    {
                        bb.MaxY = bb.MinY + height/2;
                    }
                    else
                    {
                        bb.MinY = bb.MaxY - height/2;
                    }
                    break;
                case BoundingBox.Axis.Z:
                    if (left)
                    {
                        bb.MaxZ = bb.MinZ + depth/2;
                    }
                    else
                    {
                        bb.MinZ = bb.MaxZ - depth/2;
                    }
                    break;
            }
            return bb;
        }
*/

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

        private static bool CheckEquality<T>(List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count) return false;

            return !list1.Except(list2).Any();
        }

        // Recursively build tree by dividing the bounding box along the longest axis and splitting the triangles between the two halves.
        private Node MakeNode(ModelData modelData, List<int> triangles, int depth, Node parentNode = null)
        {
            Node node = new Node();

            BoundingBox bb = GetBoundingBox(modelData, triangles);
            node.Box = bb;
            node.Triangles = triangles;

            //if (triangles.Count == 178)
            //{
            //    int i = 0;
            //}

            // Don't bother subdividing further if there are too few triangles.
            if (triangles.Count < MinTrianglesPerNode)
            {
                return node;
            }

            List<int> left = new List<int>();
            List<int> right = new List<int>();

            // Find the centroid to use as a subdivision point.
            Point centre = GetCentroid(modelData, triangles);

            // Find which split axis (X, Y or Z) gives the best split of triangles.

            int leftX = 0;
            int rightX = 0;
            int overlapX = 0;
            int leftY = 0;
            int rightY = 0;
            int overlapY = 0;
            int leftZ = 0;
            int rightZ = 0;
            int overlapZ = 0;

            foreach (int i in triangles)
            {
                switch (CheckSideX(modelData, i, centre.X))
                {
                    case Side.LessThan: leftX++; break;
                    case Side.GreaterThan: rightX++; break;
                    case Side.Overlap: overlapX++; break;
                    default: break;
                }
                switch (CheckSideY(modelData, i, centre.Y))
                {
                    case Side.LessThan: leftY++; break;
                    case Side.GreaterThan: rightY++; break;
                    case Side.Overlap: overlapY++; break;
                    default: break;
                }
                switch (CheckSideZ(modelData, i, centre.Z))
                {
                    case Side.LessThan: leftZ++; break;
                    case Side.GreaterThan: rightZ++; break;
                    case Side.Overlap: overlapZ++; break;
                    default: break;
                }
            }

            float ratioX = (float)leftX / rightX;
            float ratioY = (float)leftY / rightY;
            float ratioZ = (float)leftZ / rightZ;

            float ratioOverlapX = (float)overlapX / triangles.Count;
            float ratioOverlapY = (float)overlapY / triangles.Count;
            float ratioOverlapZ = (float)overlapZ / triangles.Count;

            Func<float, float> DistFromOne = delegate (float f) { return Math.Abs(1f - f); };

            float factorX = DistFromOne(ratioX) + 2*ratioOverlapX;
            float factorY = DistFromOne(ratioY) + 2*ratioOverlapY;
            float factorZ = DistFromOne(ratioZ) + 2*ratioOverlapZ;

            BoundingBox.Axis BestAxis;

            if (factorX < factorY)
            {
                if (factorX < factorZ)
                    BestAxis = BoundingBox.Axis.X;
                else
                    BestAxis = BoundingBox.Axis.Z;
            }
            else
            {
                if (factorY < factorZ)
                    BestAxis = BoundingBox.Axis.Y;
                else
                    BestAxis = BoundingBox.Axis.Z;
            }

            //var longestAxis = bb.GetLongestAxis();
            switch (BestAxis /*longestAxis*/)
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

            if (parentNode != null)
            {
                if (CheckEquality(parentNode.Triangles, left))
                {
                    return node;
                }
                if (CheckEquality(parentNode.Triangles, right))
                {
                    return node;
                }
            }

            // If left and right have > 50% in common, don't split further.
            List<int> intersect = left.Intersect(right).ToList();
            if (intersect.Count > (int)(0.75f*left.Count) && intersect.Count > (int)(0.75f*right.Count))
            {
                //Debug.WriteLine("Left and right share more than half the triangles.");
                return node;
            }

            depth++;
            if (depth < MaxDepth)
            {
                // Create child Nodes if there are any triangles.
                if (left.Count() > 0)
                {
                    node.Left = MakeNode(modelData, left, depth, node);
                }
                if (right.Count() > 0)
                {
                    node.Right = MakeNode(modelData, right, depth, node);
                }

                // Check that the child Nodes children aren't just repeating our children.
                if (node.Left != null)
                {
                    if (CheckEquality(node.Left.Triangles, node.Triangles))
                    {
                        node.Left = null;
                    }
                }
                if (node.Right != null)
                {
                    if (CheckEquality(node.Right.Triangles, node.Triangles))
                    {
                        node.Right = null;
                    }
                }
            }
            else
            {
                Debug.WriteLine("Max. depth reached. left: " + left.Count + " right: " + right.Count);
            }

            return node;
        }

        public Node MakeTree(ModelData modelData, Matrix objectToView)
        {
            _objectToView = objectToView;

            List<int> triangles = new();

            // Generate initial indices.
            if (modelData.SubGroups.Length > 0)
            {
                foreach (var group in modelData.SubGroups)
                {
                    if (group.Enabled)
                    {
                        triangles.AddRange(group.Triangles);
                    }
                }
            }
            else
            {
                for (int i = 0; i < modelData.triangles.Length; i++)
                {
                    triangles.Add(i);
                }
            }

            Node root = MakeNode(modelData, triangles, 0);

            return root;
        }
    }
}
