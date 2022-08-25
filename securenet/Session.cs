namespace msgfiles
{
    public class Session
    {
        public string token { get; set; } = "";
        public string email { get; set; } = "";
        public string display { get; set; } = "";
        public Dictionary<string, string> variables { get; set; } = new Dictionary<string, string>();
    }
}
