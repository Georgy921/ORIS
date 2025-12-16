using GameAndDot.Shared.Enums;

namespace GameAndDot.Shared.Models
{
    public class EventMessage
    {
        public EventType Type { get; set; }

        public string Id { get; set; }

        public string Username { get; set; }

        public List<PlayerInfo>? PlayerInfo { get; set; }
        public int? DotX { get; set; }
        public int? DotY { get; set; }

    }
}
