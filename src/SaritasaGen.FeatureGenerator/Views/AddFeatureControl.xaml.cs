using CalcBinding;
using System.Windows;
using System.Windows.Controls;

namespace SaritasaGen.FeatureGenerator.Views
{
    /// <summary>
    /// Interaction logic for AddFeatureControl.xaml.
    /// </summary>
    public partial class AddFeatureControl : UserControl
    {
        public AddFeatureControl()
        {
            InitializeComponent();

            // We have to explicitly refer to Calc.Binding namespace because of exception in runtime.
            var reference = FalseToVisibility.Hidden;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FeatureNameTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            BaseClassComboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            ReturnDtoTextBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            ReturnBuiltInTextBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
        }
    }
}
