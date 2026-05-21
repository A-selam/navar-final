using System.Collections.Generic;
using System.Linq;
using SQLite4Unity3d;
using UnityEngine;
using NavAR.Core.Entities;
using NavAR.Core.Interfaces;
using System;

namespace NavAR.Data.SQLite
{
    public class SQLiteMapRepository : IMapRepository
    {
        private readonly SQLiteConnection _db;

        public SQLiteMapRepository()
        {
            var dbPath = SQLitePaths.GetDatabasePath();
            _db = new SQLiteConnection(dbPath);

            CreateTables();

            var seeder = new SQLiteSeeder(_db);
            seeder.SeedIfNeeded();
            BackfillAnchorNodeIds();
        }

        public List<Destination> GetAllDestinations()
        {
            return _db.Table<DbDestination>()
                .ToList()
                .Select(MapDestination)
                .ToList();
        }

        public List<Destination> GetDestinationsByCategory(string category)
        {
            return _db.Table<DbDestination>()
                .Where(d => d.category == category)
                .ToList()
                .Select(MapDestination)
                .ToList();
        }

        public List<GraphNode> GetGraphNodes(int floorId)
        {
            return _db.Table<DbGraphNode>()
                .Where(n => n.floor_id == floorId)
                .ToList()
                .Select(MapNode)
                .ToList();
        }

        public List<GraphEdge> GetGraphEdges(int floorId)
        {
            var nodeIds = _db.Table<DbGraphNode>()
                .Where(n => n.floor_id == floorId)
                .ToList()
                .Select(n => n.node_id)
                .ToList();

            if (nodeIds.Count == 0)
            {
                return new List<GraphEdge>();
            }

            var nodeIdSet = new HashSet<string>(nodeIds);
            return _db.Table<DbGraphEdge>()
                .ToList()
                .Where(e => nodeIdSet.Contains(e.from_node_id) && nodeIdSet.Contains(e.to_node_id))
                .Select(MapEdge)
                .ToList();
        }

        // Returns all graph edges in the database (including cross-floor edges)
        public List<GraphEdge> GetAllGraphEdges()
        {
            return _db.Table<DbGraphEdge>()
                .ToList()
                .Select(MapEdge)
                .ToList();
        }

        public QRAnchor GetQRAnchor(string qrPayload)
        {
            var dbAnchor = _db.Table<DbQRAnchor>()
                .FirstOrDefault(a => a.qr_payload == qrPayload || a.qr_id == qrPayload);

            if (dbAnchor == null)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Could not find QR Anchor with ID: {qrPayload}");
                return null;
            }

            var anchor = MapAnchor(dbAnchor);
            ApplyAnchorNodeReference(anchor);
            return anchor;
        }

        private void CreateTables()
        {
            _db.CreateTable<DbQRAnchor>();
            _db.CreateTable<DbDestination>();
            _db.CreateTable<DbGraphNode>();
            _db.CreateTable<DbGraphEdge>();
            _db.CreateTable<DbNavigationSession>();
            _db.CreateTable<DbTelemetryRecord>();
            EnsureAnchorNodeIdColumn();
            EnsureDestinationNodeIdsColumn();
            DropLegacyDestinationEntrancesTable();
        }

        private void EnsureAnchorNodeIdColumn()
        {
            try
            {
                var columns = _db.Query<TableInfoRow>("PRAGMA table_info(qr_anchors);");
                var hasNodeIdColumn = columns.Any(c =>
                    string.Equals(c.name, "node_id", StringComparison.OrdinalIgnoreCase));

                if (!hasNodeIdColumn)
                {
                    _db.Execute("ALTER TABLE qr_anchors ADD COLUMN node_id TEXT;");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Could not verify/update qr_anchors schema: {ex.Message}");
            }
        }

        private void EnsureDestinationNodeIdsColumn()
        {
            try
            {
                var columns = _db.Query<TableInfoRow>("PRAGMA table_info(destinations);");
                var hasNodeIdsColumn = columns.Any(c =>
                    string.Equals(c.name, "entrance_node_ids", StringComparison.OrdinalIgnoreCase));

                if (!hasNodeIdsColumn)
                {
                    _db.Execute("ALTER TABLE destinations ADD COLUMN entrance_node_ids TEXT;");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Could not verify/update destination schema: {ex.Message}");
            }
        }

        private void DropLegacyDestinationEntrancesTable()
        {
            try
            {
                _db.Execute("DROP TABLE IF EXISTS destination_entrances;");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Could not drop legacy destination_entrances table: {ex.Message}");
            }
        }

        private void BackfillAnchorNodeIds()
        {
            const float tolerance = 0.01f;
            var anchors = _db.Table<DbQRAnchor>()
                .ToList()
                .Where(a => string.IsNullOrWhiteSpace(a.node_id))
                .ToList();

            if (anchors.Count == 0)
            {
                return;
            }

            var nodes = _db.Table<DbGraphNode>().ToList();
            if (nodes.Count == 0)
            {
                return;
            }

            var updated = false;
            foreach (var anchor in anchors)
            {
                var candidates = nodes;
                if (anchor.floor_id > 0)
                {
                    candidates = nodes.Where(n => n.floor_id == anchor.floor_id).ToList();
                }

                DbGraphNode bestNode = null;
                var bestDistance = float.MaxValue;
                foreach (var node in candidates)
                {
                    var dx = node.x - anchor.x;
                    var dy = node.y - anchor.y;
                    var dz = node.z - anchor.z;
                    var dist = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestNode = node;
                    }
                }

                if (bestNode != null && bestDistance <= tolerance)
                {
                    anchor.node_id = bestNode.node_id;
                    updated = true;
                }
            }

            if (updated)
            {
                _db.UpdateAll(anchors);
            }
        }

        private static Destination MapDestination(DbDestination db)
        {
            return new Destination
            {
                destination_id = db.destination_id,
                floor_id = db.floor_id,
                name = db.name,
                category = db.category,
                entrance_node_ids = ParseEntranceNodeIds(db.entrance_node_ids)
            };
        }

        private static List<string> ParseEntranceNodeIds(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            return raw
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static GraphNode MapNode(DbGraphNode db)
        {
            return new GraphNode
            {
                node_id = db.node_id,
                floor_id = db.floor_id,
                x = db.x,
                y = db.y,
                z = db.z,
                node_type = (NodeType)db.node_type,
                is_accessible = db.is_accessible
            };
        }

        private static GraphEdge MapEdge(DbGraphEdge db)
        {
            return new GraphEdge
            {
                edge_id = db.edge_id,
                from_node_id = db.from_node_id,
                to_node_id = db.to_node_id,
                distance = db.distance,
                edge_type = (EdgeType)db.edge_type,
                is_accessible = db.is_accessible
            };
        }

        private static QRAnchor MapAnchor(DbQRAnchor db)
        {
            return new QRAnchor
            {
                qr_id = db.qr_id,
                floor_id = db.floor_id,
                node_id = db.node_id,
                location_name = db.location_name,
                x = db.x,
                y = db.y,
                z = db.z,
                rotation_y = db.rotation_y
            };
        }

        private void ApplyAnchorNodeReference(QRAnchor anchor)
        {
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.node_id))
            {
                return;
            }

            var dbNode = _db.Table<DbGraphNode>()
                .FirstOrDefault(n => n.node_id == anchor.node_id);

            if (dbNode == null)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Anchor {anchor.qr_id} references missing node {anchor.node_id}.");
                return;
            }

            anchor.x = dbNode.x;
            anchor.y = dbNode.y;
            anchor.z = dbNode.z;

            if (anchor.floor_id != dbNode.floor_id)
            {
                Debug.LogWarning($"[SQLiteMapRepository] Anchor {anchor.qr_id} floor {anchor.floor_id} does not match node {dbNode.node_id} floor {dbNode.floor_id}. Using node floor.");
                anchor.floor_id = dbNode.floor_id;
            }
        }

        private class TableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }
    }
}
