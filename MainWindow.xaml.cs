using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;

namespace CSharpRecherchePME
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int TIMELAPSE = 3000;
        Stopwatch _stopWatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            _stopWatch.Reset();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combobox = ((ComboBox)e.Source);
            bool isLocation = combobox == cbLocation;

            if(combobox.SelectedIndex < 0) return;

            combobox.Dispatcher.Invoke(() =>
            {
                combobox.Tag = isLocation ? ((Location)combobox.SelectedItem).value : ((Job)combobox.SelectedItem).value;
            });

            if(cbJob.SelectedIndex > -1 && cbLocation.SelectedIndex > -1)
            {
                dgData.Dispatcher.Invoke(() =>
                {
                    dgData.ItemsSource = QueriesHttp.GetListPME(((Job)cbJob.SelectedItem).occupation, ((Location)cbLocation.SelectedItem).value);
                });
            }
        }

        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var combobox = ((ComboBox)e.Source);
            string search = "";
            bool compare = false;

            combobox.Dispatcher.Invoke(() =>
            {
                if (combobox.Tag == null)
                {
                    combobox.Tag ??= "";
                    combobox.ItemsSource = null;
                    return;
                }                

                compare = !combobox.Text.Equals(combobox.Tag);
                search = combobox.Text;
            });

            bool isLocation = combobox == cbLocation;

            if (compare)
            {
                Task.Run(() =>
                    {
                        if (isLocation)
                        {
                            var task = Task.Run<List<Location>>(() =>
                            {
                                return QueriesHttp.GetListLocations(search);
                            });
                            var locs = task.Result;
                            combobox.Dispatcher.Invoke(() =>
                            {
                                
                                combobox.ItemsSource = locs;
                                combobox.DisplayMemberPath = "value";
                                
                                if (locs.Count > 0) combobox.IsDropDownOpen = true;
                            });
                        }
                        else
                        {
                            var task = Task.Run<List<Job>>(() =>
                            {
                                return QueriesHttp.GetListJobs(search);
                            });
                            var jobs = task.Result;
                            combobox.Dispatcher.Invoke(() =>
                            {                                
                                combobox.ItemsSource = jobs;
                                combobox.DisplayMemberPath = "value";
                                
                                if (jobs.Count > 0) combobox.IsDropDownOpen = true;
                            });
                        }

                        combobox.Dispatcher.Invoke(() => combobox.Tag = combobox.Text);
                    });
            }
        }
    }
}
