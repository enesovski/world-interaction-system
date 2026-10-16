using Archura.WIS.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Archura.WIS.Surfaces;

namespace Archura.WIS.Sources
{
    public sealed class MouseInteractionSource : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private Camera targetCamera;
        
        [Header("Interaction Settings")]
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField, Min(0.01f)] private float maxDistance = 1000f;
        [SerializeField, Min(0.01f)] private float radius = 1f;
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        private void OnEnable()
        {
            if (clickAction == null || clickAction.action == null) return;

            clickAction.action.performed += OnClick;
            clickAction.action.Enable();
        }

        private void OnDisable()
        {
            if (clickAction == null || clickAction.action == null) return;

            clickAction.action.performed -= OnClick;
            clickAction.action.Disable();
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (Mouse.current == null || InteractionManager.Instance == null) return;

            Camera camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, surfaceLayers, QueryTriggerInteraction.Ignore))
                return;

            if (!hit.collider.TryGetComponent(out InteractionSurface surface))
                surface = hit.collider.GetComponentInParent<InteractionSurface>();

            if (surface == null) return;

            var sample = new InteractionSample(hit.point, Vector3.zero, radius, strength);
            InteractionManager.Instance.Publish(surface.Channel, in sample);
        }
    }
}