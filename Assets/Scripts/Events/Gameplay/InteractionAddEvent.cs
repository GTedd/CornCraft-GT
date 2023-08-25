#nullable enable
using CraftSharp.Interaction;

namespace CraftSharp.Event
{
    public record InteractionAddEvent : BaseEvent
    {
        public int InteractionId;
        public InteractionInfo Info { get; }

        public InteractionAddEvent(int id, InteractionInfo info)
        {
            InteractionId = id;
            Info = info;
        }
    }
}