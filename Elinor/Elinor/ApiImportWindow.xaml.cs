using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using eZet.EveLib.EveXmlModule;

namespace Elinor
{
    /// <summary>
    /// Interaction logic for ApiImportWindow.xaml
    /// </summary>
    public partial class ApiImportWindow
    {
        private bool? _getStandingAccess;
        public Settings Settings = new Settings();

        public ApiImportWindow()
        {
            InitializeComponent();
            Settings.Accounting = 0;
            Settings.BrokerRelations = 0;
        }

        private void BtnGetCharsClick(object sender, RoutedEventArgs e)
        {
            cbChars.Items.Clear();
            try
            {
                var keyIdString = tbKeyId.Text;
                var vcode = tbVCode.Text;
                int keyId;

                if (!int.TryParse(keyIdString, out keyId))
                    return;

                var api = EveXml.CreateApiKey(keyId, vcode);
                api.Init();
                var characters = api.GetCharacterList();

                //var info = new APIKeyInfo(keyid.ToString(CultureInfo.InvariantCulture), vcode);
                //info.Query();

                if (characters.Result.Characters.Count == 0)
                {
                    MessageBox.Show("No characters for this API information.\nPlease check you API information",
                        "No characters found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    lblChar.Visibility = Visibility.Visible;
                    cbChars.Visibility = Visibility.Visible;

                    foreach (var chr in characters.Result.Characters)
                    {
                        var chara = new CharWrapper
                        {
                            KeyId = keyId,
                            VCode = vcode,
                            Charname = chr.CharacterName,
                            CharId = chr.CharacterId
                        };

                        cbChars.Items.Add(chara);
                        cbChars.SelectedIndex = 0;
                    }
                    if (cbChars.Items.Count > 0)
                    {
                        btnOk.IsEnabled = true;
                    }
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

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            btnOk.IsEnabled = false;
            pbLoading.Visibility = Visibility.Visible;
            var chara = (CharWrapper) cbChars.SelectedItem;

            var worker = new BackgroundWorker();
            _getStandingAccess = true;
            worker.RunWorkerCompleted += delegate
            {
                if (_getStandingAccess == true) DialogResult = true;
                else Dispatcher.Invoke(new Action(Close));
            };
            worker.DoWork += delegate
            {
                var character = EveXml.CreateCharacter(chara.KeyId, chara.VCode, chara.CharId).Init();
                var charSheet = character.GetCharacterSheet().Result;


                foreach (var skill in charSheet.Skills)
                {
                    if (skill.TypeId == 3446) //"Broker Relations"
                        Settings.BrokerRelations = skill.Level;
                    if (skill.TypeId == 16622) //"Accounting" 
                        Settings.Accounting = skill.Level;
                }

                Dispatcher.Invoke(new Action(delegate
                {
                    var aisfw =
                        new ApiImportSelectFactionWindow(chara)
                        {
                            Topmost = true,
                            Top = Top + 10,
                            Left = Left + 10,
                        };
                    _getStandingAccess = aisfw.ShowDialog();
                    if (_getStandingAccess == true)
                    {
                        Settings.CorpStanding = aisfw.Corp;
                        Settings.FactionStanding = aisfw.Faction;
                    }
                    else
                    {
                        _getStandingAccess = false;
                    }
                }));

                Settings.ProfileName = chara.Charname;
            };

            worker.RunWorkerAsync();
        }

        private void BtnCreateKeyClick(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://support.eveonline.com/api/Key/CreatePredefined/524296/0/false");
        }
    }
}