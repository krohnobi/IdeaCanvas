# IdeaCanvas

Eine Mindmap-App für .NET MAUI (Blazor Hybrid), bei der Notizen nicht starr radial angeordnet werden, sondern sich organisch im Raum verteilen — berechnet mit [MSAGL](https://github.com/microsoft/automatic-graph-layout) (Microsoft Automatic Graph Layout) und dessen MDS-Algorithmus (Multidimensional Scaling).

Statt eines klassischen "Root in der Mitte, Äste strikt links/rechts"-Layouts (wie FreeMind/XMind) ordnen sich Notizen nach ihrer tatsächlichen Distanz im Gedanken-Baum an — verwandte Ideen rücken näher zusammen, unabhängige Äste verteilen sich frei.

## Features

- **Organisches Layout** über MSAGL/MDS statt starrer Baumstruktur
- **Geschwungene Verbindungslinien** mit einstellbarer Stärke (0 = MSAGL-Standard, mehr = sanfter S-Schwung)
- **Frei verschiebbare Notizen** — gezogene Nodes werden gepinnt und bleiben an ihrer Position, auch wenn sich der Rest der Map neu anordnet
- **Pan & Zoom** auf dem Canvas
- **Kontextmenü** pro Notiz: Text/Beschreibung bearbeiten, Kind-Notiz hinzufügen, löschen, Position lösen
- **Hover-Tooltip** für Beschreibungen, ohne die Karten optisch zu überladen
- **Lokale Persistenz** als JSON-Datei

## Tech-Stack

| Bereich | Technologie |
|---|---|
| App-Framework | .NET MAUI Blazor Hybrid |
| Layout-Engine | [Microsoft.Msagl](https://www.nuget.org/packages/Microsoft.Msagl) / `Microsoft.Msagl.Drawing` |
| Rendering | Inline SVG in Blazor-Komponenten |
| Persistenz | JSON (`System.Text.Json`), lokale Datei |
| Text-Messung | JS-Interop (DOM-basierte Messung für exakte Node-Größen) |

## Architektur

```
IdeaCanvas/
├─ Models/
│  ├─ MindMap.cs          — Container: Titel, Nodes-Liste
│  ├─ MindMapNode.cs       — Text, Description, ParentId, Layout
│  ├─ NodeLayout.cs        — X, Y, Width, Height, IsPinned
│  └─ EdgeLayout.cs        — gesampelte Punktliste je Kante (MSAGL-unabhängig)
├─ Services/
│  ├─ MindMapService.cs    — CRUD auf dem Node-Baum (Add/Remove/Move/Reorder)
│  ├─ MindMapStorageService.cs — Speichern/Laden als JSON
│  └─ LayoutService.cs     — MSAGL-Integration: Graph aufbauen, MDS anwenden,
│                             Ergebnis zurückschreiben, gepinnte Bereiche korrigieren
└─ Pages/
   └─ Debug.razor          — SVG-Rendering, Interaktion, Pan/Zoom
```

**Bewusste Design-Entscheidung:** Die Mindmap ist intern eine **flache Liste** von Nodes mit `ParentId` (wie eine Datenbank-Tabelle mit Self-Referencing-Key), keine verschachtelte `Children`-Baumstruktur — vereinfacht CRUD-Operationen und macht `LayoutService` unabhängig von Rekursion.

**MSAGL kennt kein "Pinning".** Once ein Node vom Nutzer manuell verschoben wird, überspringt `LayoutService` ihn beim Zurückschreiben der MDS-Position und korrigiert stattdessen seinen kompletten Unterbaum um die Differenz zwischen "wo MSAGL ihn hinrechnet" und "wo er wirklich sein soll" — ohne dabei bereits separat gepinnte Nachfahren zu verschieben.

## Setup

```bash
git clone <repo-url>
cd IdeaCanvas
dotnet restore
dotnet build -t:Run -f net8.0-windows10.0.19041.0   # oder Zielplattform nach Wahl
```

## Roadmap

- [ ] Node per Rechtsklick auf leere Canvas-Fläche erstellen (Verknüpfung mit nächstgelegenem Node)
- [ ] Delete-Funktion im Kontextmenü fertigstellen
- [ ] Plattformübergreifende Persistenz (`FileSystem.AppDataDirectory` statt festem Desktop-Pfad)
- [ ] Übersichtsseite für mehrere gespeicherte Mindmaps
- [ ] PNG-Export des sichtbaren Ausschnitts
- [ ] Von Debug-Seite zu polierter App-UI

## Lizenz

_Noch nicht festgelegt._
