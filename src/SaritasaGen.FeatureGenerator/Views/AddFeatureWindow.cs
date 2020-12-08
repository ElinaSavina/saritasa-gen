using SaritasaGen.Infrastructure.Mvvm.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace SaritasaGen.FeatureGenerator.Views
{
    /// <summary>
    /// Add feature window.
    /// </summary>
    public class AddFeatureWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddCommandWindow" /> class.
        /// </summary>
        /// <param name="uiShell">UI shell.</param>
        /// <param name="userControl">User control.</param>
        /// <param name="viewModel">View model.</param>
        public AddFeatureWindow(AddFeatureControl userControl, FeatureViewModel viewModel)
        {
            Title = "Add feature";
            Width = 400;
            MinHeight = 180;
            Background = Brushes.WhiteSmoke;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            DataContext = viewModel;
            userControl.DataContext = DataContext;
            Content = userControl;
        }
    }
}
