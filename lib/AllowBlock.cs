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
            email = Utils.GetValidEmail(email).ToLower();
            if (email.Length == 0)
                throw new InputException($"Invalid email: {email}");

            string domain = email.Substring(email.IndexOf('@')).ToLower();

            if (m_allowList.Contains(email))
                return;

            if (m_blockList.Contains(email))
                throw new InputException($"Blocked email: {email}");

            if (m_allowList.Contains(domain))
                return;

            if (m_blockList.Contains(domain))
                throw new InputException($"Blocked domain: {email}");

            if (m_allowList.Count > 0)
                throw new InputException($"Not allowed: {email}");

            // no allow list, not blocked -> allowed
        }

        private HashSet<string> m_allowList = new HashSet<string>();
        private HashSet<string> m_blockList = new HashSet<string>();
    }
}
