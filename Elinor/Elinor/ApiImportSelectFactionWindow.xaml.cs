using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using eZet.EveLib.EveXmlModule;

namespace Elinor
{
    /// <summary>
    /// Interaction logic for ApiImportSelectFactionWindow.xaml
    /// </summary>
    public partial class ApiImportSelectFactionWindow
    {
        private readonly CharWrapper _chara;
        public double Corp;
        public double Faction;

        internal ApiImportSelectFactionWindow(CharWrapper chr)
        {
            InitializeComponent();
            _chara = chr;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var none = new StandingWrapper("<None>", .0);
            cbCorp.Items.Add(none);
            cbFaction.Items.Add(none);
            cbCorp.SelectedIndex = 0;
            cbFaction.SelectedIndex = 0;

            var character = EveXml.CreateCharacter(_chara.KeyId, _chara.VCode, _chara.CharId);
            var standings = character.GetStandings().Result;
            
            foreach (var standing in standings.CharacterStandings.Corporations)
            {
                var wrap = new StandingWrapper(standing.FromName, standing.Standing);
                cbCorp.Items.Add(wrap);
            }

            foreach (var standing in standings.CharacterStandings.Factions)
            {
                var wrap = new StandingWrapper(standing.FromName, standing.Standing);
                cbFaction.Items.Add(wrap);
            }

            ToolTipService.SetShowDuration(imgHelp, int.MaxValue);
        }

        private void BtnOkClick(object sender, RoutedEventArgs e)
        {
            Faction = ((StandingWrapper) cbFaction.SelectedItem).Standing;
            Corp = ((StandingWrapper) cbCorp.SelectedItem).Standing;
            DialogResult = true;
            Close();
        }
    }
}