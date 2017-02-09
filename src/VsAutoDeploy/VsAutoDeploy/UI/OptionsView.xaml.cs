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


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
            DialogResult = true;
            Close();
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
        
        private void OutputFilesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            comboBox.Focus();

            var projectViewModel = viewModel.SelectedProject;
            if (projectViewModel == null)
                return;

            var path = projectViewModel.OutputFullPath;

            if (String.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                comboBox.ItemsSource = null;
                return;
            }

            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(p => Path.GetFileNameWithoutExtension(path) == projectViewModel.Name)
                .ThenBy(p => Path.GetFileNameWithoutExtension(path))
                .ThenBy(p => Path.GetExtension(path))
                .Select(Path.GetFileName)
                .OrderBy(p => p)
                .ToArray();

            comboBox.ItemsSource = files;
        }


        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            ((ListBox)sender).ScrollIntoView(viewModel.SelectedProject);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var removedItems = e.RemovedItems.OfType<OptionsViewModel.ProjectViewModel>();
            foreach (var item in removedItems)
                viewModel.SelectedProjects.Remove(item);

            var addedItems = e.AddedItems.OfType<OptionsViewModel.ProjectViewModel>().Except(viewModel.SelectedProjects);
            foreach (var item in addedItems)
                viewModel.SelectedProjects.Add(item);
        }


        private void EnableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Enable(true);
        }

        private void DisableMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Enable(false);
        }

        private void IncludeSubDirectoriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IncludeSubDirectories(true);
        }

        private void DontIncludeSubDirectoriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IncludeSubDirectories(false);
        }

        private void AddOutputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddOutput();
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Clear();
        }
    }
}
