using UnityEngine;

namespace Archura.WIS.Core
{
    public static class InteractionEmitter
    {
        public static bool TryEmit(
            RaycastHit hit,
            InteractionChannel channel,
            float radius = 1f,
            float strength = 1f,
            Vector3 velocity = default)
        {
            if (hit.collider == null) return false;

            return TryEmit(hit.collider, hit.point, channel, radius, strength, velocity);
        }

        public static bool TryEmit(
            Collider collider,
            Vector3 position,
            InteractionChannel channel,
            float radius = 1f,
            float strength = 1f,
            Vector3 velocity = default)
        {
            if (collider == null) return false;

            InteractionSurface surface = collider.GetComponentInParent<InteractionSurface>();
            return TryEmit(surface, channel, position, radius, strength, velocity);
        }

        public static bool TryEmit(
            InteractionSurface surface,
            InteractionChannel channel,
            Vector3 position,
            float radius = 1f,
            float strength = 1f,
            Vector3 velocity = default)
        {
            if (surface == null || !surface.isActiveAndEnabled || channel == null) return false;

            var sample = new InteractionSample(position, velocity, radius, strength);
            surface.Interact(channel, in sample);
            return true;
        }
    }
}