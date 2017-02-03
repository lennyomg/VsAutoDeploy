using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using EnvDTE80;
using Window = System.Windows.Window;

namespace VsAutoDeploy
{
    public partial class OptionsView : Window
    {
        private readonly OptionsViewModel viewModel;

        public OptionsView(OptionsViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            DataContext = viewModel;
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Apply();
            DialogResult = true;
            Close();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            comboBox.Focus();

            var path = comboBox.Tag as string;

            if (String.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                comboBox.ItemsSource = null;
                return;
            }

            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(p => p)
                .ToArray();

            comboBox.ItemsSource = files;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = false;

            try
            {
                if (!String.IsNullOrEmpty(viewModel.TargetDirectory) && Directory.Exists(viewModel.TargetDirectory))
                    dialog.SelectedPath = viewModel.TargetDirectory;
            }
            catch
            {
                dialog.SelectedPath = String.Empty;
            }

            if (dialog.ShowDialog() == true)
                viewModel.TargetDirectory = dialog.SelectedPath;
        }
    }
}
