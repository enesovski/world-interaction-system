namespace Archura.WIS.Core
{
    public interface IInteractionResponder
    {
        void Receive(in InteractionSample sample);
    }
}