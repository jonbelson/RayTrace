using System;
using System.Collections.Generic;
using System.Configuration;
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
        public Node Parent { get; set; } = null;
        public int Depth { get; set; } = -1;

        public List<int> Triangles = new List<int>();

        public BoundingBox Box { get; set; }

        public Node() { }
    }

    //
    // Create a binary space partition tree for a Model's data.
    //
    public class TreeMaker
    {
        public int MaxDepth { get; set; } = 10;//40;
        public int MinTrianglesPerNode { get; set; } = 50;//10;

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

            int numPoints = triangles.Count * 3;  // Three vertices for each triangle.

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

        private static void AddToList(Side side, int i, List<int> left, List<int> right)
        {
            if (side == Side.LessThan) left.Add(i);
            else if (side == Side.GreaterThan) right.Add(i);
            else if (side == Side.Overlap) { left.Add(i); right.Add(i); }
        }

        // Check if two lists contain exactly the same elements.
        private static bool CheckEquality<T>(List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count) return false;

            return !list1.Except(list2).Any();
        }

        // Split a bounding box into two at the specified point along the specified axis.
        private static (BoundingBox, BoundingBox) SplitBoundingBox(BoundingBox bb, BoundingBox.Axis axis, float p)
        {
            BoundingBox minor = new(bb);
            BoundingBox major = new(bb);

            switch (axis)
            {
                case BoundingBox.Axis.X:
                    if (p >= bb.MinX && p <= bb.MaxX)
                    {
                        minor.MaxX = p;
                        major.MinX = p;
                    }
                    break;
                case BoundingBox.Axis.Y:
                    if (p >= bb.MinY && p <= bb.MaxY)
                    {
                        minor.MaxY = p;
                        major.MinY = p;
                    }
                    break;
                case BoundingBox.Axis.Z:
                    if (p >= bb.MinZ && p <= bb.MaxZ)
                    {
                        minor.MaxZ = p;
                        major.MinZ = p;
                    }
                    break;
            }

            return (minor, major);
        }

        // Recursively build tree by dividing the bounding box along an axis and splitting the triangles between the two halves.
        private Node MakeNode(ModelData modelData, List<int> triangles, BoundingBox bb, int depth, Node parentNode = null)
        {
            Node node = new()
            {
                Box = bb,
                Depth = depth,
                Parent = parentNode
            };

            // Don't bother subdividing further if there are too few triangles.
            if (triangles.Count < MinTrianglesPerNode)
            {
                node.Triangles = triangles;
                return node;
            }

            // Find the centroid to use as a subdivision point.
            Point centre = GetCentroid(modelData, triangles);

            BoundingBox.Axis bestAxis = BoundingBox.Axis.X;
            float f = 0.0f;

            switch (depth%3)
            {
                case 0: bestAxis = BoundingBox.Axis.X; f = centre.X; break;
                case 1: bestAxis = BoundingBox.Axis.Y; f = centre.Y; break;
                case 2: bestAxis = BoundingBox.Axis.Z; f = centre.Z; break;
            }

            List<int> left = new List<int>();
            List<int> right = new List<int>();

            // Split triangles into two groups, divided by the specified plane.
            switch (bestAxis)
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

            // Check that left and right triangle lists aren't just repeating their parent's.
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
                node.Triangles = triangles;
                return node;
            }

            depth++;
            if (depth < MaxDepth)
            {
                // Split the BoundingBox into half along the chosen axis.
                var boxes = SplitBoundingBox(bb, bestAxis, f);
                BoundingBox minor = boxes.Item1;
                BoundingBox major = boxes.Item2;

                Node CreateNode(ModelData modelData, List<int> triangles, BoundingBox bbox, int depth, Node parentNode)
                {
                    return (triangles.Count > 0) ? MakeNode(modelData, triangles, bbox, depth, parentNode) : null;
                }

                if (depth < 2)
                {
                    Task task1 = Task.Factory.StartNew(() =>
                    {
                        node.Left = CreateNode(modelData, left, minor, depth, node);
                        //if (left.Count > 0)
                        //{
                        //    node.Left = MakeNode(modelData, left, minor, depth, node);
                        //}
                    });

                    Task task2 = Task.Factory.StartNew(() =>
                    {
                        node.Right = CreateNode(modelData, right, major, depth, node);
                        //if (right.Count > 0)
                        //{
                        //    node.Right = MakeNode(modelData, right, major, depth, node);
                        //}
                    });

                    Task.WaitAll(task1, task2);
                }
                else
                {
                    // Create left/right child Nodes if they would have any triangles.
                    node.Left = CreateNode(modelData, left, minor, depth, node);
                    node.Right = CreateNode(modelData, right, major, depth, node);

                    /*
                    if (left.Count > 0)
                    {
                        node.Left = MakeNode(modelData, left, minor, depth, node);
                    }
                    if (right.Count > 0)
                    {
                        node.Right = MakeNode(modelData, right, major, depth, node);
                    }
                    */
                }
            }
            else
            {
                //Debug.WriteLine("Max. depth reached. left: " + left.Count + " right: " + right.Count);
            }

            // If we cannot split further, assign all the triangles to this Node.
            if (node.Left == null && node.Right == null)
            {
                node.Triangles = triangles;
            }

            return node;
        }

        //
        // Build a BSP tree from the specified ModelData, transformed according to the passed-in Matrix.
        //
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

            BoundingBox bb = modelData.GetBoundingBox();
            bb = bb.Transform(objectToView);
            Node root = MakeNode(modelData, triangles, bb, 0);

            return root;
        }
    }
}
