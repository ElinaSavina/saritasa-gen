using SaritasaGen.Infrastructure.Abstractions;
using System.Windows;

namespace SaritasaGen.FeatureGenerator.Services
{
    /// <summary>
    /// Dialog service.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <inheritdoc />
        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
