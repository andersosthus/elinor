using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Elinor.Views
{
    /// <summary>
    /// Interaction logic for ProfileNameWindow.xaml
    /// </summary>
    public partial class ProfileNameWindow
    {
        public ProfileNameWindow()
        {
            InitializeComponent();
        }

        internal string ProfileName { get; private set; }

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            bool ok = true;
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            foreach (var invalid in invalidFileNameChars)
            {
                if (tbName.Text.Contains(invalid.ToString(CultureInfo.InvariantCulture))) ok = false;
            }

            if (ok)
            {
                ProfileName = tbName.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                var sInvalid = invalidFileNameChars.Where(invalidFileNameChar =>
                    !char.IsControl(invalidFileNameChar)).Aggregate("", (current, invalidFileNameChar)
                        => current + (invalidFileNameChar + " "));

                MessageBox.Show(string.Format("Profile name may not contain\n{0}", sInvalid),
                    "Invalid profile name",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            tbName.Focus();
        }

        private void TbNameTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnOk.IsEnabled = tbName.Text != string.Empty;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.Enter) BtnOkClick(this, null);
        }
    }
}