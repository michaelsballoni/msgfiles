namespace msgfiles
{
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
            int fileCount, 
            long totalSizeBytes, 
            out bool shouldDelete, 
            out string extractionFolder
        );
    }
}
