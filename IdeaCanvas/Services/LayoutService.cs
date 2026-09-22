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
        // Grundabstand zwischen Nodes, den MDS beim Verteilen anstrebt.
        private const double LayoutScale = 200;

        /// <summary>
        /// Kompletter Layout-Durchlauf: Graph aufbauen, MSAGL rechnen lassen,
        /// Ergebnis zurückschreiben, gepinnte Bereiche korrigieren, Kanten sampeln.
        /// Reihenfolge ist wichtig - siehe Kommentare unten.
        /// </summary>
        public void ApplyLayout(MindMap map, double curveStrength = 0)
        {
            (GeometryGraph graph, Dictionary<Guid, Node> nodeMap) = BuildGraph(map);

            // MSAGL berechnet alle Positionen frei, OHNE Rücksicht auf gepinnte Nodes -
            // MSAGL kennt "Pinning" als Konzept gar nicht.
            ApplyMds(graph);

            AnchorRootToOrigin(map, graph, nodeMap);

            // Rohe MSAGL-Positionen ins eigene Modell übernehmen (außer bei gepinnten Nodes,
            // deren alte Position bleibt unangetastet).
            WriteBackPositions(map, nodeMap);

            // PROBLEM: MDS platziert seine "Punktewolke" irgendwo im Raum, unabhängig davon,
            // wo dein gepinnter Node eigentlich sitzen soll. Deshalb: nachträglich den
            // Unterbaum jedes gepinnten Nodes so verschieben, dass der gepinnte Node
            // wieder an seiner echten Position landet - der Rest der Wolke drumherum
            // (die MDS-Anordnung) bleibt dabei relativ unverändert, nur verschoben.
            AlignPinnedSubtrees(map, nodeMap);

            // Kanten erst ganz am Ende sampeln, weil sie die FINALEN (schon korrigierten)
            // Positionen brauchen.
            WriteBackEdges(map, graph, curveStrength);
        }

        /// <summary>
        /// Root soll immer die feste "Mitte" der Mindmap sein. MDS platziert seine
        /// Punktewolke aber bei jedem Aufruf an einer leicht anderen Stelle im Raum -
        /// deshalb wird hier der GESAMTE Graph so verschoben, dass Root exakt am
        /// festen Ankerpunkt (0,0) landet. Die relative Anordnung aller anderen Nodes
        /// bleibt dabei unverändert, nur die absolute Position rückt gerade.
        /// </summary>
        private void AnchorRootToOrigin(MindMap map, GeometryGraph graph, Dictionary<Guid, Node> nodeMap)
        {
            var rootNode = map.Nodes.FirstOrDefault(n => n.ParentId == null);
            if (rootNode == null || !nodeMap.TryGetValue(rootNode.Id, out var rootGeomNode))
                return;

            double offsetX = 0 - rootGeomNode.Center.X;
            double offsetY = 0 - rootGeomNode.Center.Y;

            graph.Translate(new Point(offsetX, offsetY));
        }

        /// <summary>
        /// Rückt die Unterbäume gepinnter Nodes gerade, nachdem MDS ihre Positionen
        /// "verwürfelt" hat. Idee: MDS' Ergebnis ist nur bis auf Verschiebung/Drehung
        /// korrekt - wir korrigieren hier nur die Verschiebung (keine Drehung).
        /// </summary>
        private void AlignPinnedSubtrees(MindMap map, Dictionary<Guid, Node> nodeMap)
        {
            // Nur "oberste" Pins behandeln (deren Vorfahre nicht selbst schon gepinnt ist) -
            // sonst würde ein gepinntes Kind eines gepinnten Elternteils DOPPELT korrigiert
            // werden (einmal über den Elternteil-Durchlauf, einmal über seinen eigenen),
            // was bei wiederholtem ApplyLayout zu immer weiter "wandernden" Nodes führt.
            var topLevelPinned = map.Nodes.Where(n => n.Layout.IsPinned && !HasPinnedAncestor(map, n));

            foreach (var pinnedNode in topLevelPinned)
            {
                if (!nodeMap.TryGetValue(pinnedNode.Id, out var geomNode))
                    continue;

                // Differenz zwischen "wo der Node WIRKLICH sein soll" (Layout.X/Y, vom
                // Nutzer gezogen) und "wo MSAGL ihn gerade hingerechnet hat" (geomNode.Center).
                // Das ist der Korrektur-Vektor, den der ganze Unterbaum mitmachen muss.
                double offsetX = pinnedNode.Layout.X - geomNode.Center.X;
                double offsetY = pinnedNode.Layout.Y - geomNode.Center.Y;

                if (offsetX == 0 && offsetY == 0)
                    continue; // nichts zu tun, Positionen stimmen schon überein

                // Denselben Offset auf JEDEN Nachfahren addieren, damit die ganze
                // Unterstruktur "im Ganzen" mitwandert statt nur der Pin-Node selbst.
                foreach (var descendantId in GetSubtreeIds(map, pinnedNode.Id))
                {
                    if (descendantId == pinnedNode.Id)
                        continue; // der gepinnte Node selbst ist schon an der richtigen Stelle

                    var node = map.Nodes.FirstOrDefault(n => n.Id == descendantId);
                    if (node != null)
                    {
                        node.Layout.X += offsetX;
                        node.Layout.Y += offsetY;
                    }
                }
            }
        }

        /// <summary>
        /// Prüft, ob irgendein Vorfahre (Parent, Großparent, ...) dieses Nodes
        /// selbst gepinnt ist. Läuft die ParentId-Kette einfach nach oben durch.
        /// </summary>
        private static bool HasPinnedAncestor(MindMap map, MindMapNode node)
        {
            var currentParentId = node.ParentId;
            while (currentParentId.HasValue)
            {
                var parent = map.Nodes.FirstOrDefault(n => n.Id == currentParentId);
                if (parent == null) return false;
                if (parent.Layout.IsPinned) return true;
                currentParentId = parent.ParentId;
            }
            return false;
        }

        /// <summary>
        /// Liefert die Ids eines Nodes und ALLER seiner Nachfahren (Kinder, Enkel, ...).
        /// Iterativ per Warteschlange statt Rekursion (Breadth-First).
        /// </summary>
        private static IEnumerable<Guid> GetSubtreeIds(MindMap map, Guid rootId)
        {
            var result = new List<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                result.Add(currentId);

                foreach (var child in map.Nodes.Where(n => n.ParentId == currentId))
                    queue.Enqueue(child.Id);
            }

            return result;
        }

        /// <summary>
        /// Ruft MSAGLs MDS-Algorithmus auf. Das ist der einzige Schritt, an dem
        /// tatsächlich "Layout-Magie" passiert - alles davor/danach ist nur
        /// Vor-/Nachbereitung.
        /// </summary>
        private void ApplyMds(GeometryGraph graph)
        {
            var settings = new MdsLayoutSettings
            {
                ScaleX = LayoutScale,
                ScaleY = LayoutScale,
                IterationsWithMajorization = 30, // Nachbearbeitung, macht Ergebnis "glatter"
                RemoveOverlaps = true,           // verhindert, dass sich Node-Boxen überlappen
                EdgeRoutingSettings = { EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.SugiyamaSplines }
            };

            LayoutHelpers.CalculateLayout(graph, settings, null);
        }

        /// <summary>
        /// Übersetzt unser eigenes MindMap-Modell (Guid, ParentId) in MSAGLs
        /// eigene Objektwelt (Node, Edge, GeometryGraph). MSAGL kennt unsere
        /// Klassen nicht, deshalb reines "Umkopieren" in zwei Durchläufen:
        /// erst alle Nodes anlegen, DANACH erst Edges (weil beide Enden einer
        /// Edge schon im Dictionary stehen müssen).
        /// </summary>
        private (GeometryGraph graph, Dictionary<Guid, Node> nodeMap) BuildGraph(MindMap map)
        {
            var graph = new GeometryGraph();
            var nodeMap = new Dictionary<Guid, Node>();

            foreach (var node in map.Nodes)
            {
                // Fallback-Größe, falls noch nie gemessen wurde (Width/Height == 0)
                double width = node.Layout.Width > 0 ? node.Layout.Width : 100;
                double height = node.Layout.Height > 0 ? node.Layout.Height : 40;

                var boundaryCurve = CurveFactory.CreateRectangle(width, height, new Point(0, 0));
                // node.Id als UserData mitgeben, damit wir später (WriteBackEdges) von der
                // MSAGL-Edge wieder zurück zu "welcher unserer Guid gehört das" finden.
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

        /// <summary>
        /// Übernimmt die von MSAGL berechneten Positionen zurück in unser Modell.
        /// Gepinnte Nodes werden übersprungen - ihre Position bleibt, wie der
        /// Nutzer sie manuell gesetzt hat.
        /// </summary>
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

        /// <summary>
        /// Wandelt jede MSAGL-Kante in eine Liste roher Punkte (EdgeLayout) um,
        /// die das Rendering ohne jedes MSAGL-Wissen als Linie/Kurve zeichnen kann.
        /// </summary>
        private void WriteBackEdges(MindMap map, GeometryGraph graph, double curveStrength)
        {
            map.Edges.Clear();

            foreach (var edge in graph.Edges)
            {
                Guid sourceId = (Guid)edge.Source.UserData;
                Guid targetId = (Guid)edge.Target.UserData;

                var sourceNode = map.Nodes.FirstOrDefault(n => n.Id == sourceId);
                var targetNode = map.Nodes.FirstOrDefault(n => n.Id == targetId);

                var edgeLayout = new EdgeLayout { SourceNodeId = sourceId, TargetNodeId = targetId };

                bool isPinned = (sourceNode?.Layout.IsPinned ?? false) || (targetNode?.Layout.IsPinned ?? false);

                // Ist einer der beiden Enden gepinnt, ist MSAGLs eigene Kurve (edge.Curve)
                // NICHT mehr korrekt (die zeigt ja auf die "rohe", unkorrigierte MDS-Position,
                // nicht auf die tatsächliche, nachträglich verschobene Node-Position).
                // Deshalb in dem Fall die Kante komplett selbst neu berechnen, ausgehend
                // von den echten aktuellen Layout.X/Y-Werten.
                edgeLayout.Points = isPinned && sourceNode != null && targetNode != null
                    ? SamplePinnedEdge(sourceNode, targetNode, curveStrength)
                    : SampleMsaglEdge(edge.Curve, curveStrength);

                map.Edges.Add(edgeLayout);
            }
        }

        /// <summary>
        /// Kante zwischen zwei (potenziell gepinnten) Nodes: Start-/Endpunkt sind
        /// dort, wo die Verbindungslinie den Rand der jeweiligen Box verlässt
        /// (nicht die Node-Mitte selbst - das sähe "durchgestochen" aus).
        /// </summary>
        private List<EdgePoint> SamplePinnedEdge(MindMapNode source, MindMapNode target, double curveStrength)
        {
            var start = GetBoundaryPoint(source, new Point(target.Layout.X, target.Layout.Y));
            var end = GetBoundaryPoint(target, new Point(source.Layout.X, source.Layout.Y));
            return SampleStraightOrBulged(start, end, curveStrength);
        }

        /// <summary>
        /// Kante direkt aus MSAGLs eigener Kurve abtasten (normaler, ungepinnter Fall).
        /// Eine "Curve" ist eine Funktion, kein fertiges Punkte-Array - deshalb tasten
        /// wir sie an mehreren gleichmäßig verteilten Stellen ab ("Sampling"), um daraus
        /// eine Punktliste für die Anzeige zu bekommen.
        /// </summary>
        private List<EdgePoint> SampleMsaglEdge(ICurve curve, double curveStrength)
        {
            // Simple gerade Verbindung ohne Hindernis im Weg: optional künstlich wölben.
            if (curve is LineSegment)
                return SampleStraightOrBulged(curve.Start, curve.End, curveStrength);

            // Komplexere Kurve (MSAGL ist bereits um ein Hindernis herumgeführt):
            // so lassen, wie sie ist, nur an sampleCount Stellen abtasten.
            const int sampleCount = 20;
            var points = new List<EdgePoint>();
            for (int i = 0; i <= sampleCount; i++)
            {
                // t linear zwischen ParStart und ParEnd interpolieren (NICHT einfach 0..20,
                // weil der Parameterbereich einer MSAGL-Kurve nicht zwingend 0..1 ist).
                double t = curve.ParStart + (curve.ParEnd - curve.ParStart) * i / sampleCount;
                var point = curve[t]; // MSAGL-Indexer: "wo bin ich auf der Kurve bei Parameter t"
                points.Add(new EdgePoint { X = point.X, Y = point.Y });
            }
            return points;
        }

        /// <summary>
        /// Gemeinsame Sampling-Logik für "gerade Linie" ODER "künstlich gewölbtes S"
        /// zwischen zwei Punkten. curveStrength == 0 -> einfach gerade Linie (2 Punkte).
        /// curveStrength > 0 -> kubische Bézier-Kurve mit S-Schwung.
        /// </summary>
        private List<EdgePoint> SampleStraightOrBulged(Point start, Point end, double curveStrength)
        {
            var points = new List<EdgePoint>();

            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = Math.Sqrt(dx * dx + dy * dy); // Kantenlänge (Satz des Pythagoras)

            if (curveStrength <= 0 || length == 0)
            {
                // Keine Wölbung gewünscht, oder Start==Ende (Division durch 0 vermeiden):
                // einfach die zwei Endpunkte, fertig.
                points.Add(new EdgePoint { X = start.X, Y = start.Y });
                points.Add(new EdgePoint { X = end.X, Y = end.Y });
                return points;
            }

            // Senkrechter Vektor zur Verbindungslinie, normalisiert (Länge = 1).
            // Ein Vektor (dx, dy) wird durch Vertauschen + Vorzeichenwechsel einer
            // Komponente um 90° gedreht: (dx, dy) -> (-dy, dx). Das "normalisiert"
            // heißt hier nur: durch die Länge teilen, damit der Vektor exakt Länge 1 hat -
            // sonst würde "offset" (unten) je nach Kantenlänge unterschiedlich stark wirken.
            double normalX = -dy / length;
            double normalY = dx / length;

            // Wie stark die Kurve zur Seite ausschlägt - proportional zur Kantenlänge,
            // damit kurze und lange Kanten optisch ähnlich stark gewölbt wirken.
            double offset = curveStrength * length * 0.15;

            // Zwei Kontrollpunkte bei 1/3 und 2/3 der Strecke, auf ENTGEGENGESETZTEN
            // Seiten der Linie (+ normalX/Y beim ersten, - beim zweiten) - dadurch
            // schwingt die resultierende Kurve erst zur einen, dann zur anderen Seite,
            // was optisch wie ein "S" aussieht (statt nur einer einseitigen Wölbung).
            var control1 = new Point(start.X + dx * (1.0 / 3.0) + normalX * offset, start.Y + dy * (1.0 / 3.0) + normalY * offset);
            var control2 = new Point(start.X + dx * (2.0 / 3.0) - normalX * offset, start.Y + dy * (2.0 / 3.0) - normalY * offset);

            const int sampleCount = 20;
            for (int i = 0; i <= sampleCount; i++)
            {
                double t = (double)i / sampleCount; // hier IST der Bereich 0..1, eigene Formel
                var point = CubicBezier(start, control1, control2, end, t);
                points.Add(new EdgePoint { X = point.X, Y = point.Y });
            }
            return points;
        }

        /// <summary>
        /// Berechnet, wo eine gedachte Linie von der Node-MITTE in Richtung "towards"
        /// den RAND der Box verlässt (statt an der Mitte zu enden - das sähe aus, als
        /// würde die Kante durch die Box "durchstechen"). Reiner Strahlensatz: prüft,
        /// ob die Linie zuerst die linke/rechte oder die obere/untere Kante trifft,
        /// und nimmt den näheren der beiden Treffer.
        /// </summary>
        private static Point GetBoundaryPoint(MindMapNode node, Point towards)
        {
            double cx = node.Layout.X, cy = node.Layout.Y;
            double hw = node.Layout.Width / 2, hh = node.Layout.Height / 2; // halbe Breite/Höhe

            double dx = towards.X - cx;
            double dy = towards.Y - cy;

            // "Wie oft müsste ich (dx, dy) skalieren, damit ich genau am linken/rechten
            // bzw. oberen/unteren Rand ankomme?" - das kleinere der beiden t gewinnt,
            // weil die Linie dort zuerst die Box verlässt.
            double tx = dx != 0 ? hw / Math.Abs(dx) : double.PositiveInfinity;
            double ty = dy != 0 ? hh / Math.Abs(dy) : double.PositiveInfinity;
            double t = Math.Min(tx, ty);

            return new Point(cx + dx * t, cy + dy * t);
        }

        /// <summary>
        /// Standard-Formel für eine kubische Bézier-Kurve: berechnet den Punkt auf der
        /// Kurve bei Parameter t (0 = Start, 1 = Ende), beeinflusst von zwei
        /// "Zug"-Kontrollpunkten c1/c2 dazwischen. u = "Rest-Anteil" (1-t) macht die
        /// Formel symmetrisch zwischen Start und Ende.
        /// </summary>
        private static Point CubicBezier(Point p0, Point c1, Point c2, Point p1, double t)
        {
            double u = 1 - t;
            double x = u * u * u * p0.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * p1.X;
            double y = u * u * u * p0.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * p1.Y;
            return new Point(x, y);
        }
    }
}