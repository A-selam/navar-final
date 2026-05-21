using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace NavAR.EditorTools
{
#if UNITY_EDITOR
    public class NodeMarkerIdRenumberWindow : EditorWindow
    {
        [SerializeField] private Transform rootContainer;
        [SerializeField] private string idPrefix = "Block-H-F0-N-";
        [SerializeField] private int startIndex = 1;
        [SerializeField] private bool updateGameObjectNames = true;

        [MenuItem("Tools/NavAR/Renumber Node Marker IDs")]
        public static void Open()
        {
            GetWindow<NodeMarkerIdRenumberWindow>("Renumber Nodes");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Node Marker Renumbering", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            rootContainer = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Root Container", "Optional. If set, only NodeMarker components under this transform are renumbered."),
                rootContainer,
                typeof(Transform),
                true);

            idPrefix = EditorGUILayout.TextField(
                new GUIContent("ID Prefix", "Prefix used for every new node_id. Update this per floor if needed."),
                idPrefix);

            startIndex = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Start Index", "The first numeric suffix to assign."), startIndex));
            updateGameObjectNames = EditorGUILayout.Toggle(new GUIContent("Rename GameObjects", "Keeps scene object names in sync with node_id."), updateGameObjectNames);

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(!HasAnyTargets()))
            {
                if (GUILayout.Button("Renumber Node Markers", GUILayout.Height(32f)))
                {
                    RenumberNodeMarkers();
                }
            }

            EditorGUILayout.HelpBox(
                "This updates NodeMarker.node_id values for all matching nodes. That is the identity used by the extractor, JSON export, SQLite seeding, and graph routing. source_name is left untouched because it is only used to match auto-generated scene nodes back to QR/target objects.",
                MessageType.Info);
        }

        private bool HasAnyTargets()
        {
            return rootContainer != null || FindObjectsOfType<NodeMarker>().Length > 0;
        }

        private void RenumberNodeMarkers()
        {
            var markers = CollectMarkers();
            if (markers.Count == 0)
            {
                EditorUtility.DisplayDialog("Renumber Node Markers", "No NodeMarker components were found.", "OK");
                return;
            }

            markers.Sort(CompareMarkers);

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            try
            {
                var nextIndex = startIndex;
                var changedCount = 0;

                foreach (var marker in markers)
                {
                    if (marker == null)
                    {
                        continue;
                    }

                    var newNodeId = BuildNodeId(nextIndex);
                    nextIndex++;

                    if (marker.node_id == newNodeId)
                    {
                        continue;
                    }

                    Undo.RecordObject(marker, "Renumber Node Marker Id");
                    marker.node_id = newNodeId;

                    if (updateGameObjectNames && marker.gameObject.name != newNodeId)
                    {
                        Undo.RecordObject(marker.gameObject, "Rename Node GameObject");
                        marker.gameObject.name = newNodeId;
                    }

                    EditorUtility.SetDirty(marker);
                    EditorUtility.SetDirty(marker.gameObject);
                    changedCount++;
                }

                EditorSceneManager.MarkAllScenesDirty();
                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.DisplayDialog("Renumber Node Markers", $"Updated {changedCount} node IDs across {markers.Count} NodeMarker objects.", "OK");
            }
            catch (Exception ex)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError($"[NodeMarkerIdRenumberWindow] Failed to renumber node markers: {ex}");
                EditorUtility.DisplayDialog("Renumber Node Markers", $"Failed to renumber node markers.\n\n{ex.Message}", "OK");
            }
        }

        private List<NodeMarker> CollectMarkers()
        {
            var markers = new List<NodeMarker>();

            if (rootContainer != null)
            {
                markers.AddRange(rootContainer.GetComponentsInChildren<NodeMarker>(true));
                return markers;
            }

            markers.AddRange(FindObjectsOfType<NodeMarker>());
            return markers;
        }

        private int CompareMarkers(NodeMarker left, NodeMarker right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var leftPath = GetHierarchyPath(left.transform);
            var rightPath = GetHierarchyPath(right.transform);
            return string.CompareOrdinal(leftPath, rightPath);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var parts = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", parts);
        }

        private string BuildNodeId(int index)
        {
            var prefix = string.IsNullOrWhiteSpace(idPrefix)
                ? "Block-H-F0-N-"
                : idPrefix;

            return $"{prefix}{index:D3}";
        }
    }
#endif
}