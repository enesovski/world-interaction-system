using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archura.WIS.Core
{
    public sealed class InteractionManager : MonoBehaviour
    {
        private readonly Dictionary<InteractionChannel, HashSet<IInteractionResponder>> listeners = new();
        
        public static InteractionManager Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Multiple InteractionManager have been detected.");
                this.enabled = false;
                return;
            }

            Instance = this;
        }

        public void Register(InteractionChannel channel, IInteractionResponder responder)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (responder == null) throw new ArgumentNullException(nameof(responder));

            if (!listeners.TryGetValue(channel, out var channelListeners))
            {
                channelListeners = new HashSet<IInteractionResponder>();
                listeners.Add(channel, channelListeners);
            }

            channelListeners.Add(responder);
        }

        public void Unregister(InteractionChannel channel, IInteractionResponder responder)
        {
            if (channel == null || responder == null) return;
            if (!listeners.TryGetValue(channel, out var channelListeners)) return;

            channelListeners.Remove(responder);
            if (channelListeners.Count == 0) listeners.Remove(channel);
        }

        public void Publish(InteractionChannel channel, in InteractionSample sample)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (!isActiveAndEnabled || !listeners.TryGetValue(channel, out var channelListeners)) return;

            var receivers = new IInteractionResponder[channelListeners.Count];
            channelListeners.CopyTo(receivers);

            foreach (var responder in receivers)
            {
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
    }
}