using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NavAR.EditorTools
{
    [ExecuteInEditMode]
    public class NodeGraphVisualizer : MonoBehaviour
    {
        [Header("Source")]
        public Transform nodesParent;

        [Header("Display")]
        public float nodeSphereRadius = 0.15f;
        public Color nodeColor = new Color(0.1f, 0.6f, 1f, 1f);
        public Color edgeColor = new Color(0.2f, 0.9f, 0.4f, 1f);
        public bool showLabels = true;
        public bool showEdges = true;

        private void OnDrawGizmos()
        {
            DrawGraph();
        }

        private void DrawGraph()
        {
            NodeMarker[] markers;
            if (nodesParent != null)
            {
                markers = nodesParent.GetComponentsInChildren<NodeMarker>();
            }
            else
            {
                markers = FindObjectsOfType<NodeMarker>();
            }

            if (markers == null || markers.Length == 0)
            {
                return;
            }

            // Draw nodes
            Gizmos.color = nodeColor;
            foreach (var m in markers)
            {
                if (m == null) continue;
                var pos = m.transform.position;
                Gizmos.DrawSphere(pos, nodeSphereRadius);

#if UNITY_EDITOR
                if (showLabels)
                {
                    var label = string.IsNullOrWhiteSpace(m.node_id) ? m.gameObject.name : m.node_id;
                    Handles.color = nodeColor;
                    Handles.Label(pos + Vector3.up * (nodeSphereRadius * 1.5f), label);
                }
#endif
            }

            if (!showEdges) return;

            // Draw edges
            Gizmos.color = edgeColor;
            foreach (var m in markers)
            {
                if (m == null) continue;
                var link = m.GetComponent<NodeEdgeLink>();
                if (link == null) continue;

                foreach (var t in link.targets)
                {
                    if (t == null) continue;
                    Gizmos.DrawLine(m.transform.position, t.transform.position);
                }
            }
        }
    }
}
