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
    }

    public class AuthSubmit
    {
        public string Token { get; set; } = "";
    }
}
