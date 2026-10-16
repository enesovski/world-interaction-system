using Archura.WIS.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archura.WIS.Sources
{
    public sealed class MouseInteractionSource : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private Camera targetCamera;

        [Header("Interaction")]
        [SerializeField] private InteractionChannel channel;
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField, Min(0.01f)] private float maxDistance = 1000f;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        private InputAction action;

        private void OnEnable()
        {
            action = clickAction != null ? clickAction.action : null;
            if (action == null) return;

            action.performed += OnClick;
            action.Enable();
        }

        private void OnDisable()
        {
            if (action == null) return;

            action.performed -= OnClick;
            action.Disable();
            action = null;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (Mouse.current == null || channel == null) return;

            Camera camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, surfaceLayers, QueryTriggerInteraction.Ignore))
                return;

            InteractionEmitter.TryEmit(hit, channel, radius, strength);
        }
    }
}