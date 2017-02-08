using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace EnvDTE80
{
    public static class DteHelpers
    {
        public static IEnumerable<Project> GetProjects(this Solution solution)
        {
            var result = new List<Project>();

            foreach (var project in solution.Projects.OfType<Project>())
            {
                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    result.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    result.Add(project);
                }
            }

            return result;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            var result = new List<Project>();
            foreach (var projectItem in solutionFolder.ProjectItems.OfType<ProjectItem>())
            {
                var subProject = projectItem.SubProject;
                if (subProject == null)
                    continue;

                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    result.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    result.Add(subProject);
                }
            }

            return result;
        }
    }
}