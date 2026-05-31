using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NavAR.Core.Entities;
using NavAR.Core.Interfaces;

namespace NavAR.Data
{
    // A private helper class to read the JSON structure we exported
    [System.Serializable]
    public class MapDataWrapper
    {
        public List<QRAnchor> anchors = new List<QRAnchor>();
        public List<Destination> destinations = new List<Destination>();
        public List<GraphNode> nodes = new List<GraphNode>();
        public List<GraphEdge> edges = new List<GraphEdge>();
    }

    public class MockMapRepository : IMapRepository
    {
        private List<QRAnchor> _mockAnchors = new List<QRAnchor>();
        private List<Destination> _mockDestinations = new List<Destination>();
        private List<GraphNode> _mockNodes = new List<GraphNode>();
        private List<GraphEdge> _mockEdges = new List<GraphEdge>();
        private Dictionary<string, GraphNode> _nodesById = new Dictionary<string, GraphNode>();

        public MockMapRepository()
        {
            LoadExtractedData();
        }

        private void LoadExtractedData()
        {
            var floorAssets = Resources.LoadAll<TextAsset>("")
                .Where(asset => asset != null && asset.name.StartsWith("Floor"))
                .OrderBy(asset => asset.name)
                .ToList();

            if (floorAssets.Count > 0)
            {
                var anchors = new List<QRAnchor>();
                var destinations = new List<Destination>();
                var nodes = new List<GraphNode>();
                var edges = new List<GraphEdge>();

                foreach (var asset in floorAssets)
                {
                    var data = JsonUtility.FromJson<MapDataWrapper>(asset.text);
                    if (data == null)
                    {
                        Debug.LogWarning($"[MockMapRepository] Failed to parse {asset.name}, skipping.");
                        continue;
                    }

                    if (data.anchors != null) anchors.AddRange(data.anchors);
                    if (data.destinations != null) destinations.AddRange(data.destinations);
                    if (data.nodes != null) nodes.AddRange(data.nodes);
                    if (data.edges != null) edges.AddRange(data.edges);
                }

                _mockAnchors = anchors;
                _mockDestinations = destinations;
                _mockNodes = nodes;
                _mockEdges = edges;
                _nodesById = _mockNodes
                    .Where(n => !string.IsNullOrWhiteSpace(n.node_id))
                    .GroupBy(n => n.node_id)
                    .ToDictionary(g => g.Key, g => g.First());
                Debug.Log($"[MockMapRepository] Loaded {_mockDestinations.Count} Destinations, {_mockAnchors.Count} QR Anchors, {_mockNodes.Count} nodes, and {_mockEdges.Count} edges from {floorAssets.Count} JSON resources.");
            }
            else
            {
                Debug.LogError("[MockMapRepository] Could not find any Floor*_Data.json files in the Resources folder.");
            }
        }

        public List<Destination> GetAllDestinations()
        {
            return _mockDestinations;
        }

        public List<Destination> GetDestinationsByCategory(string category)
        {
            return _mockDestinations.FindAll(d => d.category == category);
        }

        public List<GraphNode> GetGraphNodes(int floorId)
        {
            return _mockNodes.FindAll(n => n.floor_id == floorId);
        }

        public List<GraphEdge> GetGraphEdges(int floorId)
        {
            var nodeIds = new HashSet<string>();
            foreach (var node in _mockNodes)
            {
                if (node.floor_id == floorId && !string.IsNullOrWhiteSpace(node.node_id))
                {
                    nodeIds.Add(node.node_id);
                }
            }

            return _mockEdges.FindAll(e => nodeIds.Contains(e.from_node_id) && nodeIds.Contains(e.to_node_id));
        }

        public List<GraphEdge> GetAllGraphEdges()
        {
            return _mockEdges;
        }

        public QRAnchor GetQRAnchor(string qrPayload)
        {
            // Search through our loaded JSON data to find the matching QR Code
            QRAnchor foundAnchor = _mockAnchors.Find(anchor => anchor.qr_id == qrPayload);

            if (foundAnchor == null)
            {
                Debug.LogWarning($"[MockMapRepository] Could not find QR Anchor with ID: {qrPayload}");
            }

            ApplyAnchorNodeReference(foundAnchor);
            return foundAnchor;
        }

        private void ApplyAnchorNodeReference(QRAnchor anchor)
        {
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.node_id))
            {
                return;
            }

            if (_nodesById != null && _nodesById.TryGetValue(anchor.node_id, out var node))
            {
                anchor.x = node.x;
                anchor.y = node.y;
                anchor.z = node.z;

                if (anchor.floor_id != node.floor_id)
                {
                    anchor.floor_id = node.floor_id;
                }
            }
            else
            {
                Debug.LogWarning($"[MockMapRepository] Anchor {anchor.qr_id} references missing node {anchor.node_id}.");
            }
        }
    }
}
