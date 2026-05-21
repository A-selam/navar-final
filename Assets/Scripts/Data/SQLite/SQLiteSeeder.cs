using System.Collections.Generic;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;
using NavAR.Core.Entities;

namespace NavAR.Data.SQLite
{
    public class SQLiteSeeder
    {
        private readonly SQLiteConnection _db;

        public SQLiteSeeder(SQLiteConnection db)
        {
            _db = db;
        }

        public void SeedIfNeeded()
        {
            var destinationCount = _db.Table<DbDestination>().Count();
            if (destinationCount > 0)
            {
                return;
            }

            // Load all TextAssets in Resources and pick those whose names start with "Floor"
            var allTextAssets = Resources.LoadAll<TextAsset>("");
            var floorAssets = new List<TextAsset>();
            foreach (var ta in allTextAssets)
            {
                if (ta == null) continue;
                if (ta.name.StartsWith("Floor"))
                {
                    floorAssets.Add(ta);
                }
            }

            if (floorAssets.Count == 0)
            {
                Debug.LogError("[SQLiteSeeder] Could not find any Floor*_Data.json files in Resources.");
                return;
            }

            // Aggregate all pieces first to avoid ordering problems (nodes referenced by cross-floor edges)
            var aggAnchors = new List<QRAnchor>();
            var aggDestinations = new List<Destination>();
            var aggNodes = new List<GraphNode>();
            var aggEdges = new List<GraphEdge>();

            foreach (var asset in floorAssets)
            {
                var parsed = JsonUtility.FromJson<SeedDataWrapper>(asset.text);
                if (parsed == null)
                {
                    Debug.LogWarning($"[SQLiteSeeder] Failed to parse {asset.name}, skipping.");
                    continue;
                }

                if (parsed.anchors != null) aggAnchors.AddRange(parsed.anchors);
                if (parsed.destinations != null) aggDestinations.AddRange(parsed.destinations);
                if (parsed.nodes != null) aggNodes.AddRange(parsed.nodes);
                if (parsed.edges != null) aggEdges.AddRange(parsed.edges);
            }

            SeedAnchors(aggAnchors);
            SeedDestinations(aggDestinations);
            SeedNodes(aggNodes);
            SeedEdges(aggEdges);
        }

        private void SeedAnchors(List<QRAnchor> anchors)
        {
            if (anchors == null || anchors.Count == 0)
            {
                return;
            }

            var dbAnchors = new List<DbQRAnchor>();
            foreach (var anchor in anchors)
            {
                dbAnchors.Add(new DbQRAnchor
                {
                    qr_id = anchor.qr_id,
                    floor_id = anchor.floor_id,
                    node_id = anchor.node_id,
                    location_name = anchor.location_name,
                    qr_payload = anchor.qr_id,
                    x = anchor.x,
                    y = anchor.y,
                    z = anchor.z,
                    rotation_y = anchor.rotation_y
                });
            }

            _db.InsertAll(dbAnchors);
        }

        private void SeedDestinations(List<Destination> destinations)
        {
            if (destinations == null || destinations.Count == 0)
            {
                return;
            }

            var dbDestinations = new List<DbDestination>();

            foreach (var dest in destinations)
            {
                var entryNodeIds = new List<string>();
                if (dest.entrance_node_ids != null && dest.entrance_node_ids.Count > 0)
                {
                    entryNodeIds.AddRange(dest.entrance_node_ids.Where(id => !string.IsNullOrWhiteSpace(id)));
                }

                dbDestinations.Add(new DbDestination
                {
                    destination_id = dest.destination_id,
                    floor_id = dest.floor_id,
                    name = dest.name,
                    category = dest.category,
                    entrance_node_ids = string.Join(",",
                        entryNodeIds
                            .Select(id => id.Trim())
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Distinct(System.StringComparer.OrdinalIgnoreCase))
                });
            }

            _db.InsertAll(dbDestinations);
        }

        private void SeedNodes(List<GraphNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            var dbNodes = new List<DbGraphNode>();
            foreach (var node in nodes)
            {
                dbNodes.Add(new DbGraphNode
                {
                    node_id = node.node_id,
                    floor_id = node.floor_id,
                    x = node.x,
                    y = node.y,
                    z = node.z,
                    node_type = (int)node.node_type,
                    is_accessible = node.is_accessible
                });
            }

            _db.InsertAll(dbNodes);
        }

        private void SeedEdges(List<GraphEdge> edges)
        {
            if (edges == null || edges.Count == 0)
            {
                return;
            }

            var dbEdges = new List<DbGraphEdge>();
            foreach (var edge in edges)
            {
                dbEdges.Add(new DbGraphEdge
                {
                    edge_id = edge.edge_id,
                    from_node_id = edge.from_node_id,
                    to_node_id = edge.to_node_id,
                    distance = edge.distance,
                    edge_type = (int)edge.edge_type,
                    is_accessible = edge.is_accessible
                });
            }

            _db.InsertAll(dbEdges);
        }

        [System.Serializable]
        private class SeedDataWrapper
        {
            public List<QRAnchor> anchors = new List<QRAnchor>();
            public List<Destination> destinations = new List<Destination>();
            public List<GraphNode> nodes = new List<GraphNode>();
            public List<GraphEdge> edges = new List<GraphEdge>();
        }
    }
}
