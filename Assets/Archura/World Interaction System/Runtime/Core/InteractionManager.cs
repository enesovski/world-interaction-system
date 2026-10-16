using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archura.WIS.Core
{
    public sealed class InteractionManager : MonoBehaviour
    {
        private readonly Dictionary<InteractionChannel, HashSet<IInteractionResponder>> listeners = new();
        private readonly HashSet<IInteractionResponder> responders = new();
        private readonly List<IInteractionResponder> receiverBuffer = new();
        private readonly List<IInteractionResponder> simulationBuffer = new();
        private List<PendingInteraction> pending = new();
        private List<PendingInteraction> dispatching = new();

        public void Register(InteractionChannel channel, IInteractionResponder responder)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (responder == null) throw new ArgumentNullException(nameof(responder));

            if (!listeners.TryGetValue(channel, out var channelListeners))
            {
                channelListeners = new HashSet<IInteractionResponder>();
                listeners.Add(channel, channelListeners);
            }

            if (channelListeners.Add(responder)) responders.Add(responder);
        }

        public void Unregister(InteractionChannel channel, IInteractionResponder responder)
        {
            if (channel == null || responder == null) return;
            if (!listeners.TryGetValue(channel, out var channelListeners)) return;
            if (!channelListeners.Remove(responder)) return;

            if (channelListeners.Count == 0) listeners.Remove(channel);
            foreach (var remainingListeners in listeners.Values)
            {
                if (remainingListeners.Contains(responder)) return;
            }
            responders.Remove(responder);
        }

        public void Publish(InteractionChannel channel, in InteractionSample sample)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (!isActiveAndEnabled) return;
            pending.Add(new PendingInteraction(channel, sample));
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled) return;
            (pending, dispatching) = (dispatching, pending);

            for (int i = 0; i < dispatching.Count; i++)
            {
                var interaction = dispatching[i];
                if (!listeners.TryGetValue(interaction.Channel, out var channelListeners)) continue;

                receiverBuffer.Clear();
                receiverBuffer.AddRange(channelListeners);
                var sample = interaction.Sample;

                foreach (var responder in receiverBuffer)
                {
                    if (!isActiveAndEnabled) return;
                    if (!channelListeners.Contains(responder)) continue;
                    try
                    {
                        responder.Receive(in sample);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, responder as UnityEngine.Object);
                    }
                }
            }

            dispatching.Clear();
            simulationBuffer.Clear();
            simulationBuffer.AddRange(responders);

            foreach (var responder in simulationBuffer)
            {
                if (!isActiveAndEnabled) return;
                if (!responders.Contains(responder)) continue;
                try
                {
                    responder.Simulate(Time.deltaTime);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, responder as UnityEngine.Object);
                }
            }
        }

        private void OnDisable()
        {
            pending.Clear();
            dispatching.Clear();
        }

        private readonly struct PendingInteraction
        {
            public InteractionChannel Channel { get; }
            public InteractionSample Sample { get; }

            public PendingInteraction(InteractionChannel channel, InteractionSample sample)
            {
                Channel = channel;
                Sample = sample;
            }
        }
    }
}
