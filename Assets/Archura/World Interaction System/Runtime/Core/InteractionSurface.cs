using Archura.WIS.Core;
using UnityEngine;

namespace Archura.WIS.Surfaces
{
    public sealed class InteractionSurface : MonoBehaviour
    {
        [SerializeField] private InteractionChannel channel;

        public InteractionChannel Channel => channel;
    }
}