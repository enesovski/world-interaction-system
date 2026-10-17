using System;
using UnityEngine;

namespace Archura.WIS.Core
{
    public sealed class InteractionSurface : MonoBehaviour
    {
        private InteractionResponder[] responders;

        private void Awake()
        {
            responders = GetComponentsInChildren<InteractionResponder>();
        }

        public void Interact(InteractionChannel channel, in InteractionSample sample)
        {
            if (!isActiveAndEnabled || channel == null || responders == null) return;

            foreach (var responder in responders)
            {
                if (responder == null) continue;

                try
                {
                    responder.Receive(this, channel, in sample);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, responder);
                }
            }
        }
    }
}