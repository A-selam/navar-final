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
            // Load the JSON file we extracted and placed in the Resources folder. 
            // Note: Do not include ".json" in the string!
            TextAsset jsonFile = Resources.Load<TextAsset>("Floor0_Data");

            if (jsonFile != null)
            {
                // Parse the JSON back into C# objects
                var data = JsonUtility.FromJson<MapDataWrapper>(jsonFile.text);
                
                if (data != null)
                {
                    _mockAnchors = data.anchors;
                    _mockDestinations = data.destinations;
                    _mockNodes = data.nodes ?? new List<GraphNode>();
                    _mockEdges = data.edges ?? new List<GraphEdge>();
                    _nodesById = _mockNodes
                        .Where(n => !string.IsNullOrWhiteSpace(n.node_id))
                        .GroupBy(n => n.node_id)
                        .ToDictionary(g => g.Key, g => g.First());
                    Debug.Log($"[MockMapRepository] Loaded {_mockDestinations.Count} Destinations and {_mockAnchors.Count} QR Anchors from JSON.");
                }
            }
            else
            {
                Debug.LogError("[MockMapRepository] Could not find Floor0_Data.json in the Resources folder!");
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
