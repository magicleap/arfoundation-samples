using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARAnchorManager))]
    [RequireComponent(typeof(ARRaycastManager))]
    public class AnchorCreator : MonoBehaviour
    {
        public void RemoveAllAnchors()
        {
            foreach (var anchor in m_Anchors)
            {
                m_AnchorManager.RemoveAnchor(anchor);
            }
            m_Anchors.Clear();
        }

        void Awake()
        {
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_AnchorManager = GetComponent<ARAnchorManager>();
            m_Anchors = new List<ARAnchor>();
        }

        void Update()
        {
            if (Input.touchCount == 0)
                return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;

            if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.FeaturePoint))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = s_Hits[0].pose;
                var anchor = m_AnchorManager.AddAnchor(hitPose);
                if (anchor == null)
                {
                    Logger.Log("Error creating anchor");
                }
                else
                {
                    m_Anchors.Add(anchor);
                }
            }
        }

        public void AnchorRaycastFromObject(GameObject source)
        {
            // Raycast against planes and feature points
            const TrackableType trackableTypes =
                TrackableType.FeaturePoint |
                TrackableType.PlaneWithinPolygon;

            Ray ray = new Ray(source.transform.position, source.transform.forward);
            // Perform the raycast
            if (m_RaycastManager.Raycast(ray, s_Hits, trackableTypes))
            {
                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hit = s_Hits[0];

                // Create a new anchor
                var anchor = m_AnchorManager.AddAnchor(hit.pose);
                if (anchor)
                {
                    // Remember the anchor so we can remove it later.
                    m_Anchors.Add(anchor);
                }
                else
                {
                    Logger.Log("Error creating anchor");
                }
            }
        }

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        List<ARAnchor> m_Anchors;

        ARRaycastManager m_RaycastManager;

        ARAnchorManager m_AnchorManager;
    }
}
