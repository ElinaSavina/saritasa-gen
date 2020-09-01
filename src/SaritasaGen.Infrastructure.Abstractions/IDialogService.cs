namespace SaritasaGen.Infrastructure.Abstractions
{
    /// <summary>
    /// Dialog service. Contains methods to manage dialog windows.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Show error.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="title">Title.</param>
        void ShowError(string message, string title);
    }
}
