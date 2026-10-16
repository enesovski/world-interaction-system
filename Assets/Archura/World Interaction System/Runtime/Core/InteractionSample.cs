using UnityEngine;

namespace Archura.WIS.Core
{
    public readonly struct InteractionSample
    {
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
        public float Radius { get; }
        public float Strength { get; }

        public InteractionSample(Vector3 position, Vector3 velocity, float radius, float strength)
        {
            Position = position;
            Velocity = velocity;
            Radius = Mathf.Max(0f, radius);
            Strength = Mathf.Clamp01(strength);
        }
    }
}
