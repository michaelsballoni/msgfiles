namespace msgfiles
{
    /// <summary>
    /// Manage allow and block lists of email addresses
    /// to validate that a given email address is allowed access
    /// </summary>
    public class AllowBlock
    {
        /// <summary>
        /// Swap in new lists
        /// </summary>
        public void SetLists(HashSet<string> allow, HashSet<string> block)
        {
            try
            {
                m_rwLock.EnterWriteLock();

                m_allowList = allow;
                m_blockList = block;
            }
            finally
            {
                m_rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Ensure that an email address or its domain is allowed,
        /// or at least not blocked
        /// </summary>
        public void EnsureEmailAllowed(string email)
        {
            try
            {
                m_rwLock.EnterReadLock();

                // Normalize the email address
                email = Utils.GetValidEmail(email).ToLower();
                if (email.Length == 0)
                    throw new InputException($"Invalid email: {email}");

                // Include the leading @, list files use this to allow/block entire domains
                string domain = email.Substring(email.IndexOf('@')).ToLower();

                // Look for specific email address blocks first, that trumps all
                if (m_blockList.Contains(email))
                    throw new InputException($"Blocked email: {email}");

                // Check for specific email address being allowed, this trumps domains
                if (m_allowList.Contains(email))
                    return;

                // Check for a whole blocked domain
                if (m_blockList.Contains(domain))
                    throw new InputException($"Blocked domain: {email}");

                // Allow a whole domain
                if (m_allowList.Contains(domain))
                    return;

                // Failing all of that, if there is an allow list,
                // the email is not on any of them, so they're blocked by default
                if (m_allowList.Count > 0)
                    throw new InputException($"Not allowed: {email}");

                // no allow list, not blocked -> allowed
            }
            finally
            {
                m_rwLock.ExitReadLock();
            }
        }

        private HashSet<string> m_allowList = new HashSet<string>();
        private HashSet<string> m_blockList = new HashSet<string>();

        private ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();
    }
}
