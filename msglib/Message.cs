namespace msgfiles
{
    public class msg
    {
        public string token { get; set; } = "";
        public string from { get; set; } = "";
        public string to { get; set; } = "";
        public string message { get; set; } = "";
        public DateTimeOffset created { get; set; }
    }
}
