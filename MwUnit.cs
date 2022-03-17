namespace RainlinkImportMaker
{
    public class MwUnit
    {
        public string Ip { get; private set; }
        public string IpRemote { get; private set; }
        public MwLink Link { get; private set; }

        public bool IsTxVolatile
        {
            get
            {
                if (Link.Tech.StartsWith("ip"))
                    return true;
                else
                    return false;
            }
        }

        public MwUnit(string Ip, string IpRemote, MwLink Link)
        {
            this.Ip = Ip;
            this.IpRemote = IpRemote;
            this.Link = Link;
        }
    }
}
