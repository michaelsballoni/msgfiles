namespace msgfiles
{
    /// <summary>
    /// Interface for client applications to prove pacification and user prompts
    /// </summary>
    public interface IClientApp
    {
        bool Cancelled { get; }
        void Log(string msg);
        void Progress(double progress);
        bool ConfirmDownload
        (
            string from, 
            string message, 
            out bool shouldDelete
        );
        bool ConfirmExtraction
        (
            string manifest, 
            out bool shouldDelete, 
            out string extractionFolder
        );
    }
}
