using IdeaCanvas.Interfaces;
using IdeaCanvas.Models;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;
using Point = Microsoft.Msagl.Core.Geometry.Point;

namespace IdeaCanvas.Services
{
    public class LayoutService : ILayoutService
    {
        private const double LayoutScale = 200;

        public void ApplyLayout(MindMap map,double curveStrength = 0)
        {
            (GeometryGraph graph, Dictionary<Guid, Node> nodeMap) = BuildGraph(map);
            ApplyMds(graph);
            WriteBackPositions(map, nodeMap);
            WriteBackEdges(map, graph,curveStrength);
        }

        private void ApplyMds(GeometryGraph graph)
        {
            var settings = new MdsLayoutSettings
            {
                ScaleX = LayoutScale,
                ScaleY = LayoutScale,
                IterationsWithMajorization = 30,
                RemoveOverlaps = true,
                EdgeRoutingSettings = { EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.SugiyamaSplines }
            };

            LayoutHelpers.CalculateLayout(graph, settings, null);
        }

        private (GeometryGraph graph, Dictionary<Guid, Node> nodeMap) BuildGraph(MindMap map)
        {
            var graph = new GeometryGraph();
            var nodeMap = new Dictionary<Guid, Node>();

            foreach (var node in map.Nodes)
            {
                double width = node.Layout.Width > 0 ? node.Layout.Width : 100;
                double height = node.Layout.Height > 0 ? node.Layout.Height : 40;

                var boundaryCurve = CurveFactory.CreateRectangle(width, height, new Point(0, 0));
                var geometryNode = new Node(boundaryCurve, node.Id);

                nodeMap.Add(node.Id, geometryNode);
                graph.Nodes.Add(geometryNode);
            }

            foreach (var node in map.Nodes)
            {
                if (node.ParentId.HasValue && nodeMap.TryGetValue(node.ParentId.Value, out var parentGeomNode))
                {
                    var edge = new Edge(parentGeomNode, nodeMap[node.Id]);
                    graph.Edges.Add(edge);
                }
            }

            return (graph, nodeMap);
        }

        private void WriteBackPositions(MindMap map, Dictionary<Guid, Node> nodeMap)
        {
            foreach (var node in map.Nodes)
            {
                if (node.Layout.IsPinned)
                {
                    continue;
                }

                if (nodeMap.TryGetValue(node.Id, out var geomNode))
                {
                    node.Layout.X = geomNode.Center.X;
                    node.Layout.Y = geomNode.Center.Y;
                }
            }
        }

        private void WriteBackEdges(MindMap map, GeometryGraph graph,double curveStrength)
        {
            map.Edges.Clear();
            foreach (var edge in graph.Edges)
            {
                

                Guid sourceId = (Guid)edge.Source.UserData;
                Guid targetId = (Guid)edge.Target.UserData;


                EdgeLayout edgeLayout = new(){
                    SourceNodeId = sourceId,
                    TargetNodeId = targetId

                };

                const int sampleCount = 20;
                var curve = edge.Curve;

                if (curve is LineSegment && curveStrength > 0)
                {
                    var start = curve.Start;
                    var end = curve.End;

                    double dx = end.X - start.X;
                    double dy = end.Y - start.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);

                    if (length > 0)
                    {
                        double normalX = -dy / length;
                        double normalY = dx / length;
                        double offset = curveStrength * length * 0.15;

                        // Erster Kontrollpunkt: bei 1/3 der Strecke, nach einer Seite versetzt
                        var control1 = new Point(
                            start.X + dx * (1.0 / 3.0) + normalX * offset,
                            start.Y + dy * (1.0 / 3.0) + normalY * offset
                        );

                        // Zweiter Kontrollpunkt: bei 2/3 der Strecke, zur ANDEREN Seite versetzt
                        var control2 = new Point(
                            start.X + dx * (2.0 / 3.0) - normalX * offset,
                            start.Y + dy * (2.0 / 3.0) - normalY * offset
                        );

                        for (int i = 0; i <= sampleCount; i++)
                        {
                            double t = (double)i / sampleCount;
                            var point = CubicBezier(start, control1, control2, end, t);
                            edgeLayout.Points.Add(new EdgePoint { X = point.X, Y = point.Y });
                        }
                    }
                    else
                    {
                        edgeLayout.Points.Add(new EdgePoint { X = start.X, Y = start.Y });
                        edgeLayout.Points.Add(new EdgePoint { X = end.X, Y = end.Y });
                    }
                }
            
                else
                {
                    for (int i = 0; i <= sampleCount; i++)
                    {
                        double t = curve.ParStart + (curve.ParEnd - curve.ParStart) * i / sampleCount;
                        var point = curve[t];
                        edgeLayout.Points.Add(new EdgePoint { X = point.X, Y = point.Y });
                    }
                }


                map.Edges.Add(edgeLayout);
            }
        }

        private static Point CubicBezier(Point p0, Point c1, Point c2, Point p1, double t)
        {
            double u = 1 - t;
            double x = u * u * u * p0.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * p1.X;
            double y = u * u * u * p0.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * p1.Y;
            return new Point(x, y);
        }
    }
}