namespace TwitchChat.IRCClient
{
    public struct Badge
    {
        public bool sub;
        public bool mod;
        public string DisplayName;
        public bool turbo;
        public string[] emotes;

        public Badge(bool somthing)
        {
            sub = false;
            mod = false;
            DisplayName = "";
            turbo = false;
            emotes = new string[1];
        }
    }
}