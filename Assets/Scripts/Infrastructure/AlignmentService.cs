using UnityEngine;
using UnityEngine.XR.ARFoundation;
using NavAR.Core.Entities;
using UnityEngine.SceneManagement;

namespace NavAR.Infrastructure
{
    public class AlignmentService : MonoBehaviour
    {
        [SerializeField] private ARSession session;
        [SerializeField] private Transform xrOrigin; // Assign your XR Origin here

        // Allow runtime binding when the serialized references are not available in the current scene
        public void SetSession(ARSession s) => session = s;
        public void SetXROrigin(Transform t) => xrOrigin = t;

        public void Realign(QRAnchor anchor)
        {
            EnsureReferences();
            // 1. Reset AR Session to clear any previous drift/offsets (if available)
            if (session != null)
            {
                session.Reset();
            }
            else
            {
                Debug.LogWarning("[AlignmentService] ARSession is null. Skipping session reset.");
            }

            // 2. Position the XR Origin at the location of the QR Anchor
            if (xrOrigin != null)
            {
                var anchorPosition = new Vector3(anchor.x, anchor.y, anchor.z);
                var targetYaw = Quaternion.Euler(0f, anchor.rotation_y, 0f);

                if (TryGetCameraLocalPose(out var cameraLocalPos, out var cameraLocalRot))
                {
                    var cameraYaw = Quaternion.Euler(0f, cameraLocalRot.eulerAngles.y, 0f);
                    xrOrigin.rotation = targetYaw * Quaternion.Inverse(cameraYaw);
                    // Offset by the camera's local pose so the camera lands on the anchor.
                    xrOrigin.position = anchorPosition - (xrOrigin.rotation * cameraLocalPos);
                }
                else
                {
                    xrOrigin.position = anchorPosition;
                    // 3. Rotate the XR Origin to match the anchor's alignment
                    xrOrigin.rotation = targetYaw;
                }
                Debug.Log($"[AlignmentService] Aligned to {anchor.location_name} at ({anchor.x}, {anchor.y}, {anchor.z})");
            }
            else
            {
                Debug.LogError("[AlignmentService] XR Origin is null. Cannot reposition or rotate the origin.");
            }
        }

        public bool RecenterToWorldPosition(Vector3 worldPosition, bool resetSession = false)
        {
            EnsureReferences();
            if (resetSession && session != null)
            {
                session.Reset();
            }

            if (xrOrigin == null)
            {
                Debug.LogError("[AlignmentService] XR Origin is null. Cannot recenter to transition landing.");
                return false;
            }

            if (TryGetCameraLocalPose(out var cameraLocalPos, out _))
            {
                xrOrigin.position = worldPosition - (xrOrigin.rotation * cameraLocalPos);
            }
            else
            {
                xrOrigin.position = worldPosition;
            }
            Debug.Log($"[AlignmentService] Recentered XR Origin to transition landing at ({worldPosition.x:F2}, {worldPosition.y:F2}, {worldPosition.z:F2}).");
            return true;
        }

        private void Awake()
        {
            // Try to auto-assign if someone forgot to wire the references in the Inspector
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (session == null || !IsSceneLoaded(session.gameObject.scene))
            {
                session = FindObjectOfType<ARSession>();
            }

            if (xrOrigin == null || !IsSceneLoaded(xrOrigin.gameObject.scene))
            {
                xrOrigin = FindOriginFromActiveScene();
                if (xrOrigin == null)
                {
                    var originComp = FindObjectOfType<ARSessionOrigin>();
                    if (originComp != null)
                    {
                        xrOrigin = originComp.transform;
                    }
                    else
                    {
                        var go = GameObject.Find("XROrigin");
                        if (go != null) xrOrigin = go.transform;
                    }
                }
            }
        }

        private static Transform FindOriginFromActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            var origins = FindObjectsOfType<ARSessionOrigin>();
            foreach (var origin in origins)
            {
                if (origin == null || origin.gameObject == null)
                {
                    continue;
                }

                if (origin.gameObject.scene == activeScene)
                {
                    return origin.transform;
                }
            }

            return null;
        }

        private static bool IsSceneLoaded(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded;
        }

        private bool TryGetCameraLocalPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;

            if (xrOrigin == null)
            {
                return false;
            }

            var cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null)
            {
                return false;
            }

            if (cameraTransform.parent == xrOrigin)
            {
                localPosition = cameraTransform.localPosition;
                localRotation = cameraTransform.localRotation;
                return true;
            }

            localPosition = xrOrigin.InverseTransformPoint(cameraTransform.position);
            localRotation = Quaternion.Inverse(xrOrigin.rotation) * cameraTransform.rotation;
            return true;
        }
    }
}
