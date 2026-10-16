using UnityEngine;

namespace Archura.WIS.Core
{
    public abstract class InteractionResponder : MonoBehaviour
    {
        [SerializeField] private InteractionChannel[] acceptedChannels;

        internal void Receive(InteractionSurface surface, InteractionChannel channel, in InteractionSample sample)
        {
            if (!isActiveAndEnabled || !Accepts(channel)) return;

            OnInteraction(surface, channel, in sample);
        }

        private bool Accepts(InteractionChannel channel)
        {
            if (channel == null || acceptedChannels == null) return false;

            foreach (var accepted in acceptedChannels)
                if (accepted == channel)
                    return true;

            return false;
        }

        protected abstract void OnInteraction(
            InteractionSurface surface, InteractionChannel channel, in InteractionSample sample);
    }
}