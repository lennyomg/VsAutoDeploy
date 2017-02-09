using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Moq;
using VsAutoDeploy;
using SolutionConfiguration = VsAutoDeploy.SolutionConfiguration;

#pragma warning disable 618

namespace TestApp
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var projects = new List<Project>();

            projects.Add(CreateSolutionFolder("UnitTests",
                CreateProject(@"UnitTests\Company.Project.Assembly1\Company.Project.Assembly1.UnitTests.csproj"),
                CreateProject(@"UnitTests\Company.Project.Assembly2\Company.Project.Assembly2.UnitTests.csproj"),
                CreateProject(@"UnitTests\Company.Project.Assembly3\Company.Project.Assembly3.UnitTests.csproj")));

            projects.Add(CreateProject(@"Company.Project.Assembly1\Company.Project.Assembly1.csproj"));
            projects.Add(CreateProject(@"Company.Project.Assembly3\Company.Project.Assembly3.csproj"));
            projects.Add(CreateProject(@"Company.Project.Assembly2\Company.Project.Assembly2.csproj"));

            var configuration = new SolutionConfiguration();
            configuration.TargetDirectory = @"C:\Temp";
            configuration.IsEnabled = true;
            configuration.Projects.Add(CreateProjectConfiguration(@"Company.Project.Assembly1\Company.Project.Assembly1.csproj", true, false));
            configuration.Projects.Add(CreateProjectConfiguration(@"Company.Project.Assembly3\Company.Project.Assembly3.csproj", false, true));

            var dteMock = new Mock<DTE2> { DefaultValue = DefaultValue.Mock };

            Mock.Get(dteMock.Object.Solution.Projects)
                .As<IEnumerable>()
                .Setup(p => p.GetEnumerator())
                .Returns(() => projects.GetEnumerator());

            Mock.Get(dteMock.Object.Solution.SolutionBuild)
                .Setup(p => p.StartupProjects)
                .Returns(new object[] { projects[1].UniqueName });

            var optionsViewModel = new OptionsViewModel(dteMock.Object, configuration);
            var optionsView = new OptionsView(optionsViewModel);
            optionsView.ShowDialog();
        }

        private static Project CreateProject(string uniqueName)
        {
            var projectMock = new Mock<Project> { DefaultValue = DefaultValue.Mock };
            projectMock.Setup(p => p.UniqueName).Returns(uniqueName);
            projectMock.Setup(p => p.FullName).Returns(Path.Combine(Environment.CurrentDirectory, uniqueName));

            var outputPathMock = new Mock<Property>();
            outputPathMock.Setup(p => p.Value).Returns(@"bin\Debug");

            Mock.Get(projectMock.Object.ConfigurationManager.ActiveConfiguration.Properties)
                .Setup(p => p.Item("OutputPath"))
                .Returns(outputPathMock.Object);

            return projectMock.Object;
        }

        private static Project CreateSolutionFolder(string uniqueName, params Project[] projects)
        {
            var projectItems = new List<ProjectItem>();
            foreach (var project in projects)
            {
                var projectItemMock = new Mock<ProjectItem>();
                projectItemMock.Setup(p => p.SubProject).Returns(project);
                projectItems.Add(projectItemMock.Object);
            }

            var projectMock = new Mock<Project> { DefaultValue = DefaultValue.Mock };
            projectMock.Setup(p => p.UniqueName).Returns(uniqueName);
            projectMock.Setup(p => p.Kind).Returns("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}");

            Mock.Get(projectMock.Object.ProjectItems)
                .As<IEnumerable>()
                .Setup(p => p.GetEnumerator())
                .Returns(() => projectItems.GetEnumerator());

            return projectMock.Object;
        }

        private static ProjectConfiguration CreateProjectConfiguration(string projectName, bool isEnabled, bool includeSubDirectories)
        {
            var projectConfiguration = new ProjectConfiguration();
            projectConfiguration.ProjectName = projectName;
            projectConfiguration.IsEnabled = isEnabled;
            projectConfiguration.IncludeSubDirectories = includeSubDirectories;
            projectConfiguration.Files.Add(Path.GetFileNameWithoutExtension(projectName) + ".dll");
            projectConfiguration.Files.Add(Path.GetFileNameWithoutExtension(projectName) + ".pdb");
            return projectConfiguration;
        }
    }
}
