using Archura.WIS.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archura.WIS.Debugging
{
    public sealed class DebugResponder : MonoBehaviour, IInteractionResponder
    {
        [SerializeField] private InteractionChannel channel;
        [SerializeField] private bool logInteractions;

        
        private void OnEnable()
        {
            if (channel != null) 
                InteractionManager.Instance.Register(channel, this);
        }

        private void OnDisable()
        {
            if (channel != null) 
                InteractionManager.Instance.Unregister(channel, this);
        }


        public void Receive(in InteractionSample sample)
        {
            if (!isActiveAndEnabled) 
                return;

            if (logInteractions)
                Debug.Log($"{name} received an interaction.", this);
        }
        
    }
}