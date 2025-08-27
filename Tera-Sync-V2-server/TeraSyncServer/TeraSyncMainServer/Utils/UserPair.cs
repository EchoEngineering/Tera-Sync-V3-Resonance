namespace TeraSyncV2Server.Hubs;

public partial class TeraHub
{
    private record UserPair
    {
        public string UserUID { get; set; }
        public string OtherUserUID { get; set; }
        public bool UserPausedOther { get; set; }
        public bool OtherPausedUser { get; set; }
    }
}
