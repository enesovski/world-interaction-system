using Archura.WIS.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archura.WIS.Sources
{
    public sealed class MouseInteractionSource : MonoBehaviour
    {
        private const int MaxDragSamplesPerFrame = 16;

        [Header("Input")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private Camera targetCamera;

        [Header("Interaction")]
        [SerializeField] private InteractionChannel channel;
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField, Min(0.01f)] private float maxDistance = 1000f;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        [Header("Dragging")]
        [SerializeField] private bool emitWhileDragging;
        [SerializeField, Min(0.01f)] private float dragSpacing = 0.3f;

        private InputAction action;
        private InteractionSurface lastSurface;
        private Vector3 lastHitPosition;
        private Vector3 lastSamplePosition;

        private void OnEnable()
        {
            action = clickAction != null ? clickAction.action : null;

            if (action == null)
                return;

            action.performed += OnClick;
            action.Enable();
        }

        private void OnDisable()
        {
            lastSurface = null;

            if (action == null)
                return;

            action.performed -= OnClick;
            action.Disable();
            action = null;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (action == null || !action.IsPressed() ||
                !TryGetSurfaceHit(out InteractionSurface surface, out Vector3 position))
            {
                lastSurface = null;
                return;
            }

            EmitClick(surface, position);
        }

        private void Update()
        {
            if (!emitWhileDragging || action == null)
                return;

            if (!action.IsPressed() ||
                !TryGetSurfaceHit(out InteractionSurface surface, out Vector3 position))
            {
                lastSurface = null;
                return;
            }

            if (surface != lastSurface)
            {
                EmitClick(surface, position);
                return;
            }

            Vector3 movement = position - lastHitPosition;
            lastHitPosition = position;

            if (movement.sqrMagnitude == 0f || Time.deltaTime <= 0f)
                return;

            EmitDrag(surface, position, movement / Time.deltaTime);
        }

        private void EmitClick(InteractionSurface surface, Vector3 position)
        {
            if (!InteractionEmitter.TryEmit(surface, channel, position, radius, strength))
            {
                lastSurface = null;
                return;
            }

            lastSurface = surface;
            lastHitPosition = position;
            lastSamplePosition = position;
        }

        private void EmitDrag(InteractionSurface surface, Vector3 position, Vector3 velocity)
        {
            Vector3 movement = position - lastSamplePosition;
            float distance = movement.magnitude;
            float spacing = Mathf.Max(0.01f, dragSpacing);

            if (distance < spacing)
                return;

            int count = Mathf.Min(Mathf.FloorToInt(distance / spacing), MaxDragSamplesPerFrame);
            float step = count == MaxDragSamplesPerFrame ? distance / count : spacing;
            Vector3 offset = movement / distance * step;

            for (int i = 0; i < count; i++)
            {
                Vector3 samplePosition = lastSamplePosition + offset;

                if (!InteractionEmitter.TryEmit(surface, channel, samplePosition, radius, strength, velocity))
                {
                    lastSurface = null;
                    return;
                }

                lastSamplePosition = samplePosition;
            }
        }

        private bool TryGetSurfaceHit(out InteractionSurface surface, out Vector3 position)
        {
            surface = null;
            position = default;

            if (Mouse.current == null || channel == null)
                return false;

            Camera camera = targetCamera != null ? targetCamera : Camera.main;

            if (camera == null)
                return false;

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, surfaceLayers, QueryTriggerInteraction.Ignore))
                return false;

            surface = hit.collider.GetComponentInParent<InteractionSurface>();
            position = hit.point;

            return surface != null && surface.isActiveAndEnabled;
        }
    }
}
