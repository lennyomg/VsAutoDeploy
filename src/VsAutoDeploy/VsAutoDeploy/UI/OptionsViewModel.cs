using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Newtonsoft.Json;

namespace VsAutoDeploy
{
    public sealed class OptionsViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Design-Time Ctor
#if DEBUG
        [Obsolete("For design-time only")]
        public OptionsViewModel()
        {
            TargetDirectory = "test";

            var p1 = new ProjectViewModel();
            p1.Name = "proj1";
            p1.IsEnabled = true;
            p1.FolderName = "common";
            p1.Files.Add(new ProjectFileViewModel { FileName = "file1" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file2" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file3" });
            p1.OutputFullPath = "C:\\Windows";

            var p2 = new ProjectViewModel();
            p2.Name = "proj2";
            p2.IsEnabled = false;
            p1.FolderName = "common/lib";
            p2.Files.Add(new ProjectFileViewModel { FileName = "file4" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file5" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file6" });

            var p3 = new ProjectViewModel();
            p2.Name = "proj3";
            p2.IsEnabled = true;
            p1.FolderName = "common";
            p3.OutputFullPath = "C:\\Windows";

            Projects = new Collection<ProjectViewModel>();
            Projects.Add(p1);
            Projects.Add(p2);
            Projects.Add(p3);

            SelectedProject = p1;

            solutionConfiguration = new SolutionConfiguration();
        }
#endif
        #endregion


        #region TargetDirectory Property

        private string _targetDirectory;

        public string TargetDirectory
        {
            get { return _targetDirectory; }
            set
            {
                if (_targetDirectory == value)
                    return;

                _targetDirectory = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region SelectedProject Property

        private ProjectViewModel _selectedProject;

        public ProjectViewModel SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (_selectedProject == value)
                    return;

                _selectedProject = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region SelectedProjects Property
        
        public ICollection<ProjectViewModel> SelectedProjects { get; }

        #endregion

        #region Projects Property

        public ICollection<ProjectViewModel> Projects { get; }

        #endregion


        private readonly SolutionConfiguration solutionConfiguration;

        public OptionsViewModel(DTE2 dte, SolutionConfiguration solutionConfiguration)
        {
            this.solutionConfiguration = solutionConfiguration;

            var projects = new List<ProjectViewModel>();
            var dteProjects = dte.Solution.GetProjects()
                .OrderBy(p => Path.GetDirectoryName(p.UniqueName))
                .ThenBy(p => p.UniqueName)
                .ToArray();

            foreach (var dteProject in dteProjects)
            {
                try
                {
                    var projectConfiguration = solutionConfiguration.Projects.FirstOrDefault(p => p.ProjectName == dteProject.UniqueName);
                    if (projectConfiguration == null)
                        projectConfiguration = new ProjectConfiguration();

                    var projectViewModel = new ProjectViewModel(dteProject, projectConfiguration.Files);
                    projectViewModel.IsEnabled = projectConfiguration.IsEnabled;
                    projectViewModel.IncludeSubDirectories = projectConfiguration.IncludeSubDirectories;
                    projects.Add(projectViewModel);
                }
                catch
                {
                }
            }

            Projects = projects;
            TargetDirectory = solutionConfiguration.TargetDirectory;
            SelectedProjects = new ObservableCollection<ProjectViewModel>();

            var startupProjects = dte.Solution.SolutionBuild.StartupProjects as object[];
            if (startupProjects != null)
            {
                var startupProject = startupProjects.OfType<string>().FirstOrDefault();
                if (startupProject != null)
                    SelectedProject = Projects.FirstOrDefault(p => p.Project.UniqueName == startupProject);
            }

            if (SelectedProject == null)
                SelectedProject = Projects.FirstOrDefault();
        }
        
    
        public void Enable(bool isEnabled)
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.IsEnabled = isEnabled;
        }

        public void IncludeSubDirectories(bool include)
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.IncludeSubDirectories = include;
        }

        public void AddOutput(string fileExtension)
        {
            foreach (var item in SelectedProjects)
            {
                var targetFileName = item.Name + fileExtension;

                if (!File.Exists(Path.Combine(item.OutputFullPath, targetFileName)))
                    continue;

                if (item.Files.Any(p => String.Equals(p.FileName, targetFileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                item.Files.Add(new ProjectFileViewModel(targetFileName));
            }
        }

        public void Clear()
        {
            foreach (var projectViewModel in SelectedProjects)
                projectViewModel.Files.Clear();
        }
        

        public void Save()
        {
            solutionConfiguration.TargetDirectory = TargetDirectory;
            solutionConfiguration.Projects.Clear();

            foreach (var projectViewModel in Projects)
            {
                var projectConfiguration = new ProjectConfiguration();
                projectConfiguration.IsEnabled = projectViewModel.IsEnabled;
                projectConfiguration.ProjectName = projectViewModel.Project.UniqueName;
                projectConfiguration.IncludeSubDirectories = projectViewModel.IncludeSubDirectories;

                foreach (var projectFileViewModel in projectViewModel.Files)
                    projectConfiguration.Files.Add(projectFileViewModel.FileName);

                solutionConfiguration.Projects.Add(projectConfiguration);
            }
        }
        

        public class ProjectViewModel: INotifyPropertyChanged
        {
            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged = delegate { };

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
            
            #region IsEnabled Property

            private bool _isEnabled;

            public bool IsEnabled
            {
                get { return _isEnabled; }
                set
                {
                    if (_isEnabled == value)
                        return;

                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }

            #endregion
            
            #region IncludeSubDirectories Property

            private bool _includeSubDirectories;

            public bool IncludeSubDirectories
            {
                get { return _includeSubDirectories; }
                set
                {
                    if (_includeSubDirectories == value)
                        return;

                    _includeSubDirectories = value;
                    OnPropertyChanged();
                }
            }

            #endregion


            public string Name { get; internal set; }

            public string FolderName { get; internal set; }

            public string OutputFullPath { get; internal set; }
            
            public ObservableCollection<ProjectFileViewModel> Files { get; }
            
            internal Project Project { get; }

            public ProjectViewModel()
            {
                Files = new ObservableCollection<ProjectFileViewModel>();
            }

            public ProjectViewModel(Project project, IEnumerable<string> files)
            {
                Project = project;
                Name = Path.GetFileNameWithoutExtension(project.UniqueName);
                FolderName = Path.GetDirectoryName(project.UniqueName);
                OutputFullPath = Path.Combine(Path.GetDirectoryName(project.FullName), (string)project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value); ;
                Files = new ObservableCollection<ProjectFileViewModel>(files.Select(p => new ProjectFileViewModel(p)).ToList());
            }
        }

        public class ProjectFileViewModel
        {
            public string FileName { get; set; }

            public ProjectFileViewModel()
            {
            }

            public ProjectFileViewModel(string fileName)
            {
                FileName = fileName;
            }
        }
    }
}
