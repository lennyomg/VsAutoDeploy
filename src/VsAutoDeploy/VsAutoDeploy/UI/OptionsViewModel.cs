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
using EnvDTE80;

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
            p1.FolderName = "common";
            p1.Files.Add(new ProjectFileViewModel { FileName = "file1" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file2" });
            p1.Files.Add(new ProjectFileViewModel { FileName = "file3" });
            p1.OutputPath = "C:\\Windows";

            var p2 = new ProjectViewModel();
            p2.Name = "proj2";
            p1.FolderName = "common/lib";
            p2.Files.Add(new ProjectFileViewModel { FileName = "file4" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file5" });
            p2.Files.Add(new ProjectFileViewModel { FileName = "file6" });

            var p3 = new ProjectViewModel("proj3", new string[0]);
            p3.OutputPath = "C:\\Windows";

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

        #region Projects Property

        public ICollection<ProjectViewModel> Projects { get; private set; }

        #endregion
        

        private readonly SolutionConfiguration solutionConfiguration;
        
        public OptionsViewModel(DTE2 dte, SolutionConfiguration solutionConfiguration)
        {
            this.solutionConfiguration = solutionConfiguration;

            var projects = new List<ProjectViewModel>();
            var dteProjects = dte.Solution.GetProjects().OrderBy(p => p.UniqueName).ToArray();
            foreach (var dteProject in dteProjects)
            {
                try
                {
                    var projectConfiguration = solutionConfiguration.Projects.FirstOrDefault(p => p.ProjectName == dteProject.UniqueName);
                    if (projectConfiguration == null)
                        projectConfiguration = new ProjectConfiguration();
                    
                    var projectViewModel = new ProjectViewModel(dteProject.UniqueName, projectConfiguration.Files);
                    projectViewModel.IsEnabled = projectConfiguration.IsEnabled;
                    projectViewModel.IncludeSubDirectories = projectConfiguration.IncludeSubDirectories;
                    projectViewModel.OutputPath = Path.Combine(Path.GetDirectoryName(dteProject.FullName), (string)dteProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value); ;
                    projects.Add(projectViewModel);
                }
                catch
                {
                }
            }

            Projects = projects.OrderBy(p => p.Name).ToArray();
            TargetDirectory = solutionConfiguration.TargetDirectory;

            var startupProjects = dte.Solution.SolutionBuild.StartupProjects as object[];
            if (startupProjects != null)
            {
                var startupProject = startupProjects.OfType<string>().FirstOrDefault();
                if (startupProject != null)
                    SelectedProject = Projects.FirstOrDefault(p => p.ProjectName == startupProject);
            }

            if (SelectedProject == null)
                SelectedProject = Projects.FirstOrDefault();
        }


        public void Apply()
        {
            solutionConfiguration.TargetDirectory = TargetDirectory;
            solutionConfiguration.Projects.Clear();

            foreach (var projectViewModel in Projects)
            {
                var projectConfiguration = new ProjectConfiguration();
                projectConfiguration.IsEnabled = projectViewModel.IsEnabled;
                projectConfiguration.ProjectName = projectViewModel.ProjectName;
                projectConfiguration.IncludeSubDirectories = projectViewModel.IncludeSubDirectories;

                foreach (var projectFileViewModel in projectViewModel.Files)
                    projectConfiguration.Files.Add(projectFileViewModel.FileName);

                solutionConfiguration.Projects.Add(projectConfiguration);
            }
        }
        

        public class ProjectViewModel
        {
            public string Name { get; set; }

            public bool IsEnabled { get; set; }

            public string FolderName { get; set; }

            public string ProjectName { get; }

            public bool IncludeSubDirectories { get; set; }

            public ObservableCollection<ProjectFileViewModel> Files { get; }

            public string OutputPath { get; set; }


            public ProjectViewModel()
            {
                Files = new ObservableCollection<ProjectFileViewModel>();
            }

            public ProjectViewModel(string projectName, IEnumerable<string> files)
            {
                ProjectName = projectName;
                Name = Path.GetFileNameWithoutExtension(projectName);
                FolderName = Path.GetDirectoryName(projectName);
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
