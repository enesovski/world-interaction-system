using Archura.WIS.Core;
using UnityEngine;

namespace Archura.WIS.Debugging
{
    public sealed class DebugResponder : InteractionResponder
    {
        [SerializeField] private bool logInteractions = true;

        protected override void OnInteraction(
            InteractionSurface surface,
            InteractionChannel channel,
            in InteractionSample sample)
        {
            if (!logInteractions) return;

            Debug.Log(
                $"[{channel.name}] {surface.name} received an interaction at {sample.Position}. " +
                $"Radius: {sample.Radius}, Strength: {sample.Strength}",
                this
            );
        }
    }
}