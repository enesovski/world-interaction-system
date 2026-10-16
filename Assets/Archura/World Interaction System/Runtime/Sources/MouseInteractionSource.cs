using Archura.WIS.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Archura.WIS.Sources
{
    public sealed class MouseInteractionSource : MonoBehaviour
    {
        [SerializeField] private InteractionManager manager;
        [SerializeField] private InteractionChannel channel;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        private void Update()
        {
            if (!WasClicked() || manager == null || channel == null) return;
            Camera camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null) return;

            Ray ray = camera.ScreenPointToRay(GetMousePosition());
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, surfaceLayers, QueryTriggerInteraction.Ignore)) return;

            var sample = new InteractionSample(hit.point, Vector3.zero, radius, strength);
            manager.Publish(channel, in sample);
        }

        private static bool WasClicked()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }
}
