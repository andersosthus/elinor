using System;
using System.Diagnostics;
using System.Windows;
using Elinor.Models;
using Elinor.Services;

namespace Elinor.Views
{
    /// <summary>
    /// Interaction logic for ApiImportWindow.xaml
    /// </summary>
    public partial class ApiImportWindow
    {
        private EveApi _api;
        private bool? _getStandingAccess;
        public Settings Settings = new Settings();

        public ApiImportWindow()
        {
            InitializeComponent();
            Settings.Accounting = 0;
            Settings.BrokerRelations = 0;
        }

        private async void BtnGetCharsClick(object sender, RoutedEventArgs e)
        {
            cbChars.Items.Clear();
            try
            {
                var keyIdString = tbKeyId.Text;
                var vcode = tbVCode.Text;
                int keyId;

                if (!int.TryParse(keyIdString, out keyId))
                    return;

                _api = new EveApi(keyId, vcode);

                var characters = await _api.GetCharactersAsync();

                if (characters.Characters.Count == 0)
                {
                    MessageBox.Show("No characters for this API information.\nPlease check you API information",
                        "No characters found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    lblChar.Visibility = Visibility.Visible;
                    cbChars.Visibility = Visibility.Visible;

                    foreach (var character in characters.Characters)
                    {
                        cbChars.Items.Add(new CharWrapper
                        {
                            KeyId = keyId,
                            VCode = vcode,
                            Charname = character.CharacterName,
                            CharId = character.CharacterId
                        });
                    }

                    if (cbChars.Items.Count <= 0)
                        return;

                    cbChars.SelectedIndex = 0;
                    btnOk.IsEnabled = true;
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Key ID must be a number", "Invalid Key ID", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            tbKeyId.Focus();
        }

        private void BtnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void BtnOkClick(object sender, RoutedEventArgs e)
        {
            btnOk.IsEnabled = false;
            pbLoading.Visibility = Visibility.Visible;

            var selectedCharacter = cbChars.SelectedItem as CharWrapper;
            if (selectedCharacter == null)
                return;

            _getStandingAccess = true;

            var characterApi = new EveCharacter(selectedCharacter.KeyId, selectedCharacter.VCode,
                selectedCharacter.CharId);

            var charSheet = await characterApi.GetCharacterSheetAsync();
            foreach (var skill in charSheet.Skills)
            {
                if (skill.TypeId == 3446) //"Broker Relations"
                    Settings.BrokerRelations = skill.Level;
                if (skill.TypeId == 16622) //"Accounting" 
                    Settings.Accounting = skill.Level;
            }

            var factionWindow = new ApiImportSelectFactionWindow(characterApi)
            {
                Topmost = true,
                Top = Top + 10,
                Left = Left + 10,
            };

            _getStandingAccess = factionWindow.ShowDialog();
            if (_getStandingAccess == true)
            {
                Settings.CorpStanding = factionWindow.Corp;
                Settings.FactionStanding = factionWindow.Faction;
            }
            else
            {
                _getStandingAccess = false;
            }

            Settings.ProfileName = selectedCharacter.Charname;

            if (_getStandingAccess == true)
                DialogResult = true;
            else
                Close();
        }

        private void BtnCreateKeyClick(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://community.eveonline.com/support/api-key/CreatePredefined?accessMask=8");
        }
    }
}