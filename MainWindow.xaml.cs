using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using Microsoft.Win32;
using System.Threading;

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

            if (combobox.SelectedIndex < 0) return;

            combobox.Dispatcher.Invoke(() =>
            {
                combobox.Tag = isLocation ? ((Location)combobox.SelectedItem).value : ((Job)combobox.SelectedItem).value;
            });

            if (cbJob.SelectedIndex > -1 && cbLocation.SelectedIndex > -1)
            {
                var occupation = ((Job)cbJob.SelectedItem).occupation;
                var location =  ((Location)cbLocation.SelectedItem).value;
                new Thread(() =>
                {
                    var list = QueriesHttp.GetListPME(occupation, location);
                    dgData.Dispatcher.Invoke(() =>
                    {
                        dgData.ItemsSource = list;
    
                    });
                }).Start();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|CSV file (*.csv)|*.csv";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFileDialog.ShowDialog() == true)
            {
                WriteCSV((List<PME>)dgData.ItemsSource, saveFileDialog.FileName);
            }
        }

        private void WriteCSV<T>(IEnumerable<T> items, string path)
        {
            if (items == null) return;
            Type itemType = typeof(T);
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(string.Join(";", props.Select(p => p.Name)));

                foreach (var item in items)
                {
                    writer.WriteLine(string.Join(";", props.Select(p => p.GetValue(item, null))));
                }
            }
        }

        void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var combobox = (ComboBox)sender;
            if (combobox.IsDropDownOpen == true)
            {
                var textEdit = combobox.Template.FindName("PART_EditableTextBox", combobox) as TextBox;
                textEdit.SelectionStart = textEdit.Text.Length;
                textEdit.SelectionLength = 0;
            }
        }

        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var combobox = (ComboBox)sender;
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
