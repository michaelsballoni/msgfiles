using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msgfiles
{
    public class AuthInfo
    {
        public string Display { get; set; } = "";
        public string Email { get; set; } = "";
        public string SessionToken {  get; set; } = "";

        public void Normalize()
        {
            Display = Display.Trim();
            Email = Email.Trim();

            SessionToken = SessionToken.Trim();

            if (string.IsNullOrWhiteSpace(Display))
                throw new Exception("Display name is missing");

            if (string.IsNullOrWhiteSpace(Email))
                throw new Exception("Email address is missing");
        }
    }

    public class AuthSubmit
    {
        public string ChallengeToken { get; set; } = "";
    }

    public class AuthResponse
    {
        public string SessionToken { get; set; } = "";
    }

    public class Message
    {
        public string FromName { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string ToName { get; set; } = "";
        public string ToEmail { get; set; } = "";
        public string MessageText { get; set; } = "";
        public string MessageToken { get; set; } = "";
        public string PayloadFilename { get; set; } = "";
        public long PayloadSizeBytes { get; set; } = -1;
        public string PayloadChecksumMd5 { get; set; } = "";
    }

    public class Session
    {
        public string Token { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
