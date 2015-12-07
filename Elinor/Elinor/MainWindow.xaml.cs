using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using eZet.EveLib.Core.Cache;
using eZet.EveLib.EveXmlModule;
using eZet.EveLib.EveXmlModule.Models.Misc;

namespace Elinor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        internal Settings Settings { get; set; }

        private readonly FileSystemWatcher _fileSystemWatcher = new FileSystemWatcher();

        private readonly DirectoryInfo _logdir =
            new DirectoryInfo(Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\EVE\logs\marketlogs");

        private readonly DirectoryInfo _profdir = new DirectoryInfo("profiles");
        private double _buy;
        private bool _cacheCleared;
        private bool _closePending;
        private FileSystemEventArgs _lastEvent;

        private double _sell;
        private long _typeId;

        public MainWindow()
        {
            InitializeComponent();

            if (_logdir.Parent != null && !_logdir.Parent.Exists)
            {
                if (Properties.Settings.Default.logpath != "")
                {
                    string path = Properties.Settings.Default.logpath;
                    try
                    {
                        _logdir = new DirectoryInfo(path);
                        SetWatcherAndStuff();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Settings file corrupted, sorry.");
                    }
                }
                else
                {
                    var dlg = new SelectLogPathWindow();
                    bool? showDialog = dlg.ShowDialog();
                    if (showDialog != null && (bool) showDialog)
                    {
                        _logdir = dlg.Logpath;
                        Properties.Settings.Default.logpath = _logdir.FullName;
                        Properties.Settings.Default.Save();
                        SetWatcherAndStuff();
                    }
                }
            }
            else
            {
                SetWatcherAndStuff();
            }
        }
        

        private void SetWatcherAndStuff()
        {
            if (!_logdir.Exists)
                _logdir.Create();
            if (!_profdir.Exists)
                _profdir.Create();

            var init = new BackgroundWorker();
            init.DoWork += (sender, args) =>
            {
                var status = EveXml.Eve.GetServerStatus().Result;
                               };
            init.RunWorkerAsync();

            _fileSystemWatcher.Path = _logdir.FullName;
            _fileSystemWatcher.Created += FileSystemWatcherOnCreated;
            _fileSystemWatcher.EnableRaisingEvents = true;

            dgSamples.ColumnWidth = DataGridLength.SizeToCells;
            dgSamplesFive.ColumnWidth = DataGridLength.SizeToCells;
            UpdateStatus();
        }

        private void ProcessData(string s)
        {
            List<List<string>> table = CSVReader.GetTableFromCSV(s);

            if (table == null) return;


            IOrderedEnumerable<List<string>> sell = from List<string> row in table
                                                    where row[7] == "False" && row[13] == "0"
                                                    orderby
                                                        double.Parse(row[0], CultureInfo.InvariantCulture) ascending
                                                    select row;
            string sss = sell.Any() ? sell.ElementAt(0)[0] : "-1.0";
            _sell = double.Parse(sss, CultureInfo.InvariantCulture);

            IOrderedEnumerable<List<string>> buy = from List<string> row in table
                                                   where row[7] == "True" && row[13] == "0"
                                                   orderby
                                                       double.Parse(row[0], CultureInfo.InvariantCulture) descending
                                                   select row;
            string bbb = buy.Any() ? buy.ElementAt(0)[0] : "-1.0";
            _buy = double.Parse(bbb, CultureInfo.InvariantCulture);

            IEnumerable<List<string>> aRow = from List<string> row in table
                                             where row[13] == "0"
                                             select row;

            foreach (var list in aRow)
            {
                long i;
                _typeId = long.TryParse(list[2], out i) ? i : -1;
                break;
            }

            var setItemName = new BackgroundWorker();
            setItemName.DoWork += (sender, args) =>
            {
                var item = EveXml.Eve.GetTypeName(_typeId).Result;

                                          Dispatcher.Invoke(new Action(delegate
                                                                           {
                                                                               if (item.Types.Count > 0)
                                                                               {
                                                                                   var type =
                                                                                       item.Types[0];
                                                                                   lblItemName.Content = type.TypeName;
                                                                                   lblItemName.ToolTip = type.TypeName;
                                                                               }
                                                                               else
                                                                               {
                                                                                   lblItemName.Content = "Unknown";
                                                                                   lblItemName.ToolTip =
                                                                                       "Product not found";
                                                                               }
                                                                           }));
                                      };
            setItemName.RunWorkerAsync();

            var getVolumes = new BackgroundWorker();
            getVolumes.DoWork += (sender, args) =>
                                     {
                                         var volumes = new Dictionary<string, long>();
                                         if (_typeId > 0)
                                         {
                                             volumes = VolumeFetcher.GetVolumes(table);
                                         }


                                         Dispatcher.Invoke(new Action(delegate
                                                                          {
                                                                              if (volumes.Count > 0)
                                                                              {
                                                                                  long i, j;
                                                                                  if (
                                                                                      volumes.TryGetValue("sellvol",
                                                                                                          out i) &&
                                                                                      volumes.TryGetValue("sellmov",
                                                                                                          out j))
                                                                                      lblSellvols.Content =
                                                                                          string.Format(
                                                                                              "{0:n0}/{1:n0}", i, j);

                                                                                  if (
                                                                                      volumes.TryGetValue("buyvol",
                                                                                                          out i) &&
                                                                                      volumes.TryGetValue("buymov",
                                                                                                          out j))
                                                                                      lblBuyvols.Content =
                                                                                          string.Format(
                                                                                              "{0:n0}/{1:n0}", i, j);
                                                                              }
                                                                          }));
                                     };
            getVolumes.RunWorkerAsync();

            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 lblSell.Content = _sell >= 0
                                                                       ? String.Format("{0:n} ISK", _sell)
                                                                       : "No orders in range";
                                                 lblBuy.Content = _buy >= 0
                                                                      ? String.Format("{0:n} ISK", _buy)
                                                                      : "No orders in range";
                                             }));

            var cdt = new CalculateDataThread(_sell, _buy, this);
            var calc = new Thread(cdt.Run);
            calc.Start();
        }

        private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 lblItemName.Content = "Fetching...";
                                                 lblItemName.ToolTip = string.Empty;

                                                 if (cbAutoCopy.IsChecked != null && (bool) cbAutoCopy.IsChecked)
                                                 {
                                                     var img = new BitmapImage();
                                                     img.BeginInit();
                                                     img.UriSource =
                                                         new Uri(
                                                             "pack://application:,,,/Elinor;component/Images/38_16_195.png");
                                                     img.EndInit();
                                                     imgCopyStatus.Source = img;
                                                 }
                                             }));

            _lastEvent = fileSystemEventArgs;
            while (MiscTools.IsFileLocked(new FileInfo(fileSystemEventArgs.FullPath))) Thread.Sleep(25);
            if (fileSystemEventArgs.ChangeType == WatcherChangeTypes.Created &&
                fileSystemEventArgs.Name.EndsWith(".txt"))
            {
                ProcessData(fileSystemEventArgs.FullPath);
            }

            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 if (cbAutoCopy.IsChecked != null && (bool) cbAutoCopy.IsChecked)
                                                 {
                                                     bool isSell = rbSell.IsChecked != null && (bool) rbSell.IsChecked;

                                                     if (rbSell.IsChecked != null && (bool) rbSell.IsChecked)
                                                         ClipboardTools.SetClipboardWrapper(
                                                             ClipboardTools.GetSellPrice(_sell, Settings));
                                                     else if (rbBuy.IsChecked != null && (bool) rbBuy.IsChecked)
                                                         ClipboardTools.SetClipboardWrapper(
                                                             ClipboardTools.GetBuyPrice(_buy, Settings));


                                                     var img = new BitmapImage();
                                                     img.BeginInit();
                                                     img.UriSource = (isSell && _sell > 0) || (!isSell && _buy > 0)
                                                                         ? new Uri(
                                                                               "pack://application:,,,/Elinor;component/Images/38_16_193.png")
                                                                         : new Uri(
                                                                               "pack://application:,,,/Elinor;component/Images/38_16_194.png");
                                                     img.EndInit();
                                                     imgCopyStatus.Source = img;
                                                 }
                                             }));
            UpdateStatus();
        }
        
        private void UpdateStatus()
        {
            long size = _logdir.GetFiles().Sum(fi => fi.Length);

            Dispatcher.Invoke(
                new Action(delegate { tbStatus.Text = String.Format("Market logs: {0:n0} KB", size/1024); }));
        }

        private void TbStatusMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CacheTools.ClearMarketLogs(_logdir);
            UpdateStatus();
        }

        private void BtnStayOnTopClick(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 if (btnStayOnTop.IsChecked != null)
                                                     Topmost = (bool) btnStayOnTop.IsChecked;
                                             }));
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Properties.Settings.Default.pin = btnStayOnTop.IsChecked != null && (bool) btnStayOnTop.IsChecked;

            if (cbAutoCopy.IsChecked == null || !(bool)cbAutoCopy.IsChecked)
                Properties.Settings.Default.autocopy = 0;
            else if (rbSell.IsChecked != null && (bool)rbSell.IsChecked)
                Properties.Settings.Default.autocopy = 1;
            else if(rbBuy.IsChecked != null && (bool) rbBuy.IsChecked)
                Properties.Settings.Default.autocopy = -1;
            
            Properties.Settings.Default.Save();
            Settings.SaveSettings(Settings);
        }

        private void TiSettingsLostFocus(object sender, RoutedEventArgs e)
        {
            Settings.SaveSettings(Settings);
        }

        private void TiSettingsGotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 slMargin.Value = Settings.MarginThreshold;
                                                 slMinimum.Value = Settings.MinimumThreshold;

                                                 tbCorpStanding.Text =
                                                     string.Format(CultureInfo.InvariantCulture, "{0:n2}",
                                                                   Settings.CorpStanding);
                                                 tbFactionStanding.Text =
                                                     string.Format(CultureInfo.InvariantCulture, "{0:n2}",
                                                                   Settings.FactionStanding);

                                                 cbBrokerRelations.SelectedIndex = Settings.BrokerRelations;
                                                 cbAccounting.SelectedIndex = Settings.Accounting;

                                                 cbAdvancedSettings.IsChecked = Settings.AdvancedStepSettings;
                                                 tbSellFract.Text =
                                                     (Settings.SellPercentage*100).ToString(CultureInfo.InvariantCulture);
                                                 tbBuyFract.Text =
                                                     (Settings.BuyPercentage*100).ToString(CultureInfo.InvariantCulture);
                                                 tbSellThresh.Text =
                                                     Settings.SellThreshold.ToString(CultureInfo.InvariantCulture);
                                                 tbBuyThresh.Text =
                                                     Settings.BuyThreshold.ToString(CultureInfo.InvariantCulture);
                                             }));
        }

        private void SlMarginValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.MarginThreshold = slMargin.Value;
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 slMinimum.Maximum = slMargin.Value;
                                                 tbPreferred.Text =
                                                     (slMargin.Value*100).ToString(CultureInfo.InvariantCulture);
                                             }));
        }

        private void SlMinimumValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Settings.MinimumThreshold = slMinimum.Value;
            Dispatcher.Invoke(
                new Action(delegate { tbMinimum.Text = (slMinimum.Value*100).ToString(CultureInfo.InvariantCulture); }));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            slMargin.ValueChanged += SlMarginValueChanged;
            slMinimum.ValueChanged += SlMinimumValueChanged;

            tbCorpStanding.TextChanged += TbCorpStandingTextChanged;
            tbFactionStanding.TextChanged += TbFactionStandingTextChanged;
            tbCorpStanding.LostFocus += TbStandingOnLostFocus;
            tbFactionStanding.LostFocus += TbStandingOnLostFocus;

            cbBrokerRelations.SelectionChanged += CbBrokerRelationsSelectionChanged;
            cbAccounting.SelectionChanged += CbAccountingSelectionChanged;

            for (int i = 0; i < 6; i++)
            {
                cbBrokerRelations.Items.Add(i);
                cbAccounting.Items.Add(i);
            }

            Settings = new Settings();
            cbProfiles.Items.Add(Settings);
            cbProfiles.SelectedIndex = 0;

            PopupPlacements();

            Tutorial.Main = this;
            Tutorial.ShowTutorialHint();

            UpdateProfiles();

            btnUpdate.IsChecked = Properties.Settings.Default.checkforupdates;

            var delayer = new BackgroundWorker();
            delayer.DoWork += (o, args) => Updates.CheckForUpdates();
            delayer.RunWorkerAsync();

            btnStayOnTop.IsChecked = Properties.Settings.Default.pin;
            Topmost = Properties.Settings.Default.pin;

            if (Properties.Settings.Default.autocopy == 0)
            {
                cbAutoCopy.IsChecked = false;
                rbSell.IsChecked = true;
            }
            else
            {
                cbAutoCopy.IsChecked = true;
                rbSell.IsChecked = Properties.Settings.Default.autocopy > 0;
                rbBuy.IsChecked = Properties.Settings.Default.autocopy < 0;
            }
        }

        private void TbStandingOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            double standing;
            var tbSender = (TextBox) sender;
            if (double.TryParse(tbSender.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out standing))
            {
                if (standing > 10) tbSender.Text = "10";
                if (standing < -10) tbSender.Text = "-10";
            }
        }

        private void PopupPlacements()
        {
            ppFactionStanding.PlacementTarget = tbFactionStanding;
            ppCorpStanding.PlacementTarget = tbCorpStanding;
        }
        
        private void UpdateProfiles()
        {
            foreach (FileInfo file in _profdir.GetFiles())
            {
                Settings settings = Settings.ReadSettings(file.Name.Replace(file.Extension, ""));
                if (settings != null)
                {
                    cbProfiles.Items.Add(settings);
                }
            }
        }

        private void CbBrokerRelationsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.BrokerRelations = cbBrokerRelations.SelectedIndex;
            UpdateBrokerFee();
        }

        private void BtnDefaultClick(object sender, RoutedEventArgs e)
        {
            var tSettings = new Settings {ProfileName = Settings.ProfileName};
            Settings = tSettings;
            Settings.SaveSettings(Settings);
            TiSettingsGotFocus(this, null);
        }

        private void UpdateBrokerFee()
        {
            Dispatcher.Invoke(new Action(delegate
                                             {
                                                 lblBrokerRelations.Content = String.Format("Broker fee: {0:n}%",
                                                                                            CalculateDataThread.
                                                                                                BrokerFee(
                                                                                                    Settings.
                                                                                                        BrokerRelations,
                                                                                                    Settings.
                                                                                                        CorpStanding,
                                                                                                    Settings.
                                                                                                        FactionStanding)*
                                                                                            100);
                                             }));
        }

        private void CbAccountingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Accounting = cbAccounting.SelectedIndex;
            Dispatcher.Invoke(
                new Action(
                    delegate
                        {
                            lblSalesTax.Content = String.Format("Sales tax: {0:n}%",
                                                                CalculateDataThread.SalesTax(Settings.Accounting)*100);
                        }));
        }

        private void CbProfilesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnDelete.IsEnabled = cbProfiles.SelectedItem.ToString() != "Default";
            Settings.SaveSettings(Settings);
            Settings = (Settings) cbProfiles.SelectedItem;
            TiSettingsGotFocus(this, null);
            if (_lastEvent != null) FileSystemWatcherOnCreated(this, _lastEvent);
        }

        private void BtnNewClick(object sender, RoutedEventArgs e)
        {
            var settings = new Settings();
            var pnw = new ProfileNameWindow
                          {
                              Top = Top + Height/10,
                              Left = Left + Width/10,
                              Topmost = Topmost,
                          };
            if (pnw.ShowDialog() == true)
            {
                settings.ProfileName = pnw.ProfileName;
                cbProfiles.Items.Add(settings);
                cbProfiles.SelectedItem = settings;
                tcMain.SelectedIndex = 1;
                Settings.SaveSettings(settings);
            }
        }

        private void BtnImportClick(object sender, RoutedEventArgs e)
        {
            var aiw = new ApiImportWindow
                          {
                              Topmost = true,
                              Top = Top + Height/10,
                              Left = Left + Width/10,
                          };

            if (aiw.ShowDialog() == true)
            {
                Settings settings = aiw.Settings;
                string fName = string.Format("profiles\\{0}.dat", settings.ProfileName);
                if (File.Exists(fName))
                {
                    MessageBoxResult result = MessageBox.Show("Character exists. Update?",
                                                              "Character already exists", MessageBoxButton.YesNo,
                                                              MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        int index = 1;
                        File.Delete(fName);
                        for (int i = 0; i < cbProfiles.Items.Count; i++)
                        {
                            var tmp = (Settings) cbProfiles.Items[i];
                            if (tmp.ProfileName == settings.ProfileName)
                            {
                                index = i;
                                cbProfiles.SelectedIndex = 0;
                                cbProfiles.Items.RemoveAt(i);
                                break;
                            }
                        }
                        cbProfiles.Items.Insert(index, settings);
                        cbProfiles.SelectedItem = settings;
                        tcMain.SelectedIndex = 1;
                    }
                }
                else
                {
                    cbProfiles.Items.Add(settings);
                    cbProfiles.SelectedItem = settings;
                    tcMain.SelectedIndex = 1;
                }
            }
        }

        private void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (cbProfiles.SelectedItem.ToString() == "Default") return;
            Settings tSet = Settings;
            int i = cbProfiles.SelectedIndex;
            cbProfiles.SelectedIndex = i - 1;
            cbProfiles.Items.RemoveAt(i);
            File.Delete("profiles\\" + tSet.ProfileName + ".dat");
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            var abt = new AboutWindow
                          {
                              Topmost = Topmost,
                              Top = Top + Height/10,
                              Left = Left + Width/10
                          };
            abt.ShowDialog();
        }

        private void PinWindow(object sender, ExecutedRoutedEventArgs e)
        {
            btnStayOnTop.IsChecked = !btnStayOnTop.IsChecked;
            BtnStayOnTopClick(this, null);
        }

        private void CbAutoCopyChecked(object sender, RoutedEventArgs e)
        {
            gbAutocopy.IsEnabled = true;
            imgCopyStatus.Source = null;
        }

        private void CbAutoCopyUnchecked(object sender, RoutedEventArgs e)
        {
            gbAutocopy.IsEnabled = false;
            imgCopyStatus.Source = null;
        }

        private void AutoCopy(object sender, ExecutedRoutedEventArgs e)
        {
            cbAutoCopy.IsChecked = !cbAutoCopy.IsChecked;
        }

        private void LblSellMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClipboardTools.SetClipboardWrapper(ClipboardTools.GetSellPrice(_sell, Settings));
        }

        private void LblBuyMouseDown(object sender, MouseButtonEventArgs e)
        {
            ClipboardTools.SetClipboardWrapper(ClipboardTools.GetBuyPrice(_buy, Settings));
        }

        private void MiSubmitBugClick(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://code.google.com/p/elinor/issues/list");
        }

        private void MiSubmitFeatureClick(object sender, RoutedEventArgs e)
        {
            Process.Start(@"http://redd.it/xl6mf");
        }

        private void RbChecked(object sender, RoutedEventArgs e)
        {
            double price = rbSell.IsChecked != null && (bool) rbSell.IsChecked
                               ? ClipboardTools.GetSellPrice(_sell, Settings)
                               : ClipboardTools.GetBuyPrice(_buy, Settings);
            ClipboardTools.SetClipboardWrapper(price);
        }

        private void BtnTutorialClick(object sender, RoutedEventArgs e)
        {
            Tutorial.NextTip();
        }

        private void CbAdvancedSettingsChecked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This is a barely tested feature.\nPlease use with caution and report any bugs.",
                                "Experimental feature", MessageBoxButton.OKCancel, MessageBoxImage.Warning) ==
                MessageBoxResult.OK)
            {
                Settings.AdvancedStepSettings = true;
                gbAdvancedSettings.IsEnabled = true;
            }
            else
            {
                cbAdvancedSettings.IsChecked = false;
            }
        }

        private void CbAdvancedSettingsUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.AdvancedStepSettings = false;
            gbAdvancedSettings.IsEnabled = false;
        }

        private void TbSellFractTextChanged(object sender, TextChangedEventArgs e)
        {
            double fract;
            if (double.TryParse(tbSellFract.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out fract))
            {
                Settings.SellPercentage = fract/100;
            }
        }

        private void TbSellThreshTextChanged(object sender, TextChangedEventArgs e)
        {
            double thresh;
            if (double.TryParse(tbSellThresh.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out thresh))
            {
                Settings.SellThreshold = thresh;
            }
        }

        private void TbBuyFractTextChanged(object sender, TextChangedEventArgs e)
        {
            double fract;
            if (double.TryParse(tbBuyFract.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out fract))
            {
                Settings.BuyPercentage = fract/100;
            }
        }

        private void TbBuyThreshTextChanged(object sender, TextChangedEventArgs e)
        {
            double thresh;
            if (double.TryParse(tbBuyThresh.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out thresh))
            {
                Settings.BuyThreshold = thresh;
            }
        }

        private void TbCorpStandingTextChanged(object sender, TextChangedEventArgs e)
        {
            double standing;

            if (double.TryParse(tbCorpStanding.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out standing))
            {
                if (standing <= 10 && standing >= -10)
                {
                    ppCorpStanding.IsOpen = false;
                    Settings.CorpStanding = standing;
                    UpdateBrokerFee();
                }
                else
                {
                    ppCorpStanding.IsOpen = true;
                }
            }
        }

        private void TbFactionStandingTextChanged(object sender, TextChangedEventArgs e)
        {
            double standing;

            if (double.TryParse(tbFactionStanding.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out standing))
            {
                if (standing <= 10 && standing >= -10)
                {
                    ppFactionStanding.IsOpen = false;
                    Settings.FactionStanding = standing;
                    UpdateBrokerFee();
                }
                else
                {
                    ppFactionStanding.IsOpen = true;
                }
            }
        }

        private void TbPreferredKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                double d;
                if (double.TryParse(tbPreferred.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                {
                    d /= 100;
                    if (d <= slMargin.Maximum)
                    {
                        slMargin.Value = d;
                    }
                    else
                    {
                        tbPreferred.Text = (slMargin.Maximum*100).ToString(CultureInfo.InvariantCulture);
                        slMargin.Value = slMargin.Maximum;
                    }
                }
            }
        }

        private void TbMinimumKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                double d;
                if (double.TryParse(tbMinimum.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                {
                    d /= 100;
                    if (d <= slMinimum.Maximum)
                    {
                        slMinimum.Value = d;
                    }
                    else
                    {
                        tbMinimum.Text = (slMinimum.Maximum*100).ToString(CultureInfo.InvariantCulture);
                        slMinimum.Value = slMinimum.Maximum;
                    }
                }
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (!_cacheCleared)
            {
                _closePending = true;
                e.Cancel = true;
                var clearCache = new BackgroundWorker();
                clearCache.DoWork += (o, args) => CacheTools.ClearApiCache();
                clearCache.RunWorkerCompleted += (o, args) =>
                                                     {
                                                         _cacheCleared = true;
                                                         if (_closePending) Close();
                                                     };
                clearCache.RunWorkerAsync();
            }
        }

        private void BtnUpdateClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.checkforupdates = btnUpdate.IsChecked != null && (bool) btnUpdate.IsChecked;
            Properties.Settings.Default.Save();
            if(Properties.Settings.Default.checkforupdates) Updates.CheckForUpdates();
        }
    }
}