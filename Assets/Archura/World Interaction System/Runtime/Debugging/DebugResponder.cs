using Archura.WIS.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archura.WIS.Debugging
{
    public sealed class DebugResponder : MonoBehaviour, IInteractionResponder
    {
        private const int RingSegments = 32;

        [SerializeField] private InteractionManager manager;
        [SerializeField] private InteractionChannel channel;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private Color markerColor = Color.cyan;
        [SerializeField, Min(0.01f)] private float markerLifetime = 2f;
        [SerializeField, Range(1, 64)] private int markerCapacity = 16;
        [SerializeField, Min(0.01f)] private float radiusScale = 1f;
        [SerializeField] private bool logInteractions;

        private Marker[] markers;
        private int nextMarker;
        private int receivedCount;

        private void OnEnable()
        {
            markers ??= new Marker[Mathf.Max(1, markerCapacity)];
            if (manager != null && channel != null) manager.Register(channel, this);
        }

        private void OnDisable()
        {
            if (manager != null && channel != null) manager.Unregister(channel, this);
            if (markers == null) return;

            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i].Renderer != null) markers[i].Renderer.enabled = false;
                markers[i].Remaining = 0f;
            }

            nextMarker = 0;
            receivedCount = 0;
        }

        private void OnDestroy()
        {
            if (markers == null) return;
            foreach (var marker in markers)
            {
                if (marker.Renderer == null) continue;
                if (Application.isPlaying) Destroy(marker.Renderer.gameObject);
                else DestroyImmediate(marker.Renderer.gameObject);
            }
        }

        public void Receive(in InteractionSample sample)
        {
            if (!isActiveAndEnabled) return;
            receivedCount++;
            if (markerMaterial == null) return;

            ref var marker = ref markers[nextMarker];
            if (marker.Renderer == null) marker.Renderer = CreateMarker();
            var ring = marker.Renderer;
            ring.startColor = ring.endColor = markerColor;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                var offset = new Vector3(Mathf.Cos(angle) * sample.Radius * radiusScale, 0.03f,
                    Mathf.Sin(angle) * sample.Radius * radiusScale);
                ring.SetPosition(i, sample.Position + offset);
            }

            ring.enabled = true;
            marker.Remaining = markerLifetime;
            nextMarker = (nextMarker + 1) % markers.Length;
        }

        public void Simulate(float deltaTime)
        {
            if (receivedCount > 0 && logInteractions)
                Debug.Log($"{name} received {receivedCount} interaction(s).", this);

            receivedCount = 0;

            for (int i = 0; i < markers.Length; i++)
            {
                ref var marker = ref markers[i];
                if (marker.Renderer == null || !marker.Renderer.enabled) continue;
                marker.Remaining -= Mathf.Max(0f, deltaTime);
                if (marker.Remaining <= 0f) marker.Renderer.enabled = false;
            }
        }

        private LineRenderer CreateMarker()
        {
            var marker = new GameObject("Interaction Marker");
            marker.transform.SetParent(transform, false);
            var ring = marker.AddComponent<LineRenderer>();
            ring.sharedMaterial = markerMaterial;
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.widthMultiplier = 0.04f;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            return ring;
        }

        private struct Marker
        {
            public LineRenderer Renderer;
            public float Remaining;
        }
    }
}
