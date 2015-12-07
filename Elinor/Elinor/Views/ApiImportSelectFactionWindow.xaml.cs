using System.Windows;
using System.Windows.Controls;
using Elinor.Services;

namespace Elinor.Views
{
    /// <summary>
    /// Interaction logic for ApiImportSelectFactionWindow.xaml
    /// </summary>
    public partial class ApiImportSelectFactionWindow
    {
        private readonly EveCharacter _characterApi;
        public double Corp;
        public double Faction;

        internal ApiImportSelectFactionWindow(EveCharacter chr)
        {
            InitializeComponent();
            _characterApi = chr;
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var none = new StandingWrapper("<None>", .0);
            cbCorp.Items.Add(none);
            cbFaction.Items.Add(none);
            cbCorp.SelectedIndex = 0;
            cbFaction.SelectedIndex = 0;

            var standings = await _characterApi.GetCharacterStandingsAsync();

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