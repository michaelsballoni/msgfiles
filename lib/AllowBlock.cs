using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msgfiles
{
    public class AllowBlock
    {
        public void SetLists(HashSet<string> allow, HashSet<string> block)
        {
            m_allowList = allow;
            m_blockList = block;
        }

        public void EnsureEmailAllowed(string email)
        {
            email = email.Trim().ToLower();
            if (email.Length == 0 || email.EndsWith('.'))
                throw new InputException($"Invalid email: {email}");

            try
            {
                if ((new System.Net.Mail.MailAddress(email)).Address != email)
                    throw new InputException($"Invalid email: {email}");
            }
            catch
            {
                throw new InputException($"Invalid email: {email}");
            }

            string domain = email.Substring(email.IndexOf('@')).Trim();

            if (m_blockList.Contains(email))
                throw new InputException($"Blocked email: {email}");

            if (m_blockList.Contains(domain))
                throw new InputException($"Blocked domain: {email}");

            if (m_allowList.Count > 0 && !m_allowList.Contains(email) && !m_allowList.Contains(domain))
                throw new InputException($"Not allowed: {email}");
        }

        private HashSet<string> m_allowList = new HashSet<string>();
        private HashSet<string> m_blockList = new HashSet<string>();
    }
}
