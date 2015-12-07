using System;
using System.Data;
using System.Windows.Media;

namespace Elinor
{
    internal class CalculateDataThread
    {
        private readonly double _buyPrice;
        private readonly MainWindow _main;
        private readonly double _sellPrice;

        public CalculateDataThread(double sell, double buy, MainWindow main)
        {
            _sellPrice = sell;
            _buyPrice = buy;
            _main = main;
        }

        internal static double BrokerFee(int brokerRelations, double corpStanding, double factionStanding)
        {
            return ((.01 - (brokerRelations*.0005))/Math.Pow(2, ((0.14*factionStanding) + (.06*corpStanding))));
        }

        internal static double SalesTax(int accounting)
        {
            return .015*(1 - (accounting*.1));
        }

        internal void Run()
        {
            if (_sellPrice < 0 || _buyPrice < 0)
            {
                _main.Dispatcher.Invoke(new Action(delegate
                {
                    _main.lblRevenue.Content = "- ISK";
                    _main.lblCoS.Content = "- ISK";
                    _main.lblProfit.Content = "- ISK";
                    _main.lblMargin.Content = "- %";
                    _main.lblMarkup.Content = "- %";

                    _main.dgSamples.DataContext = null;
                    _main.dgSamplesFive.DataContext = null;

                    _main.brdImportant.BorderBrush =
                        new SolidColorBrush(Colors.LightGray);
                    _main.lblMargin.Foreground = new SolidColorBrush(Colors.Black);
                }));
            }
            else
            {
                double brokerFee = BrokerFee(_main.Settings.BrokerRelations, _main.Settings.CorpStanding,
                    _main.Settings.FactionStanding);
                double salesTax = SalesTax(_main.Settings.Accounting);


                double revenue = ((_sellPrice - .01) - (_sellPrice - .01)*brokerFee - (_sellPrice - .01)*salesTax);
                double cos = (_buyPrice + .01) + (_buyPrice + .01)*brokerFee;

                var samples = new DataTable();
                samples.Columns.Add("Size", typeof (string));
                samples.Columns.Add("CoS", typeof (string));
                samples.Columns.Add("Profit", typeof (string));
                var samplesF = new DataTable();
                samplesF.Columns.Add("Size", typeof (string));
                samplesF.Columns.Add("CoS", typeof (string));
                samplesF.Columns.Add("Profit", typeof (string));

                for (int i = 1; i < 100000000; i *= 10)
                {
                    samples.Rows.Add(String.Format("{0:n0}", i), String.Format("{0:n}m", i*cos/1000000),
                        String.Format("{0:n}m", i*(revenue - cos)/1000000));
                    samplesF.Rows.Add(String.Format("{0:n0}", i*5), String.Format("{0:n}m", i*cos*5/1000000),
                        String.Format("{0:n}m", i*5*(revenue - cos)/1000000));
                }
                _main.Dispatcher.Invoke(new Action(delegate
                {
                    _main.lblRevenue.Content = String.Format("{0:n} ISK", revenue);
                    _main.lblCoS.Content = String.Format("{0:n} ISK", cos);
                    _main.lblProfit.Content = String.Format("{0:n} ISK",
                        revenue - cos);

                    double margin = 100*(revenue - cos)/revenue;
                    _main.lblMargin.Content = Math.Abs(margin) < 10000
                        ? String.Format("{0:n}%",
                            margin)
                        : (margin > 0 ? "∞%" : "-∞%");

                    double markup = 100*(revenue - cos)/cos;
                    _main.lblMarkup.Content = Math.Abs(markup) < 10000
                        ? String.Format("{0:n}%",
                            markup)
                        : (markup > 0 ? "∞%" : "-∞%");

                    if (margin/100 >= _main.Settings.MarginThreshold)
                    {
                        _main.lblMargin.Foreground =
                            new SolidColorBrush(Colors.ForestGreen);
                        _main.brdImportant.BorderBrush =
                            new LinearGradientBrush(Colors.LimeGreen,
                                Colors.ForestGreen, 45.0);
                    }
                    else if (margin/100 > _main.Settings.MinimumThreshold)
                    {
                        _main.lblMargin.Foreground =
                            new SolidColorBrush(Colors.Orange);
                        _main.brdImportant.BorderBrush =
                            new LinearGradientBrush(Colors.Yellow, Colors.Orange,
                                45.0);
                    }
                    else
                    {
                        _main.lblMargin.Foreground =
                            new SolidColorBrush(Colors.Red);
                        _main.brdImportant.BorderBrush =
                            new LinearGradientBrush(Colors.OrangeRed, Colors.Red,
                                45.0);
                    }

                    _main.dgSamples.ItemsSource = samples.AsDataView();
                    _main.dgSamplesFive.ItemsSource = samplesF.AsDataView();
                }));
            }
        }
    }
}