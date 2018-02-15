using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Windows.Interop;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VsAutoDeploy
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [Guid(GuidList.guidVsAutoDeployPkgString)]
    public sealed class VsAutoDeployPackage : Package
    {
        private const string configurationKey = "VsAutoDeployPackage";


        private DTE2 dte;

        private BuildEvents buildEvents;

        private SolutionEvents solutionEvents;

        private OutputWindowPane outputPane;

        private ErrorListProvider errorListProvider;


        private MenuCommand enabledMenuItem;

        private MenuCommand optionsMenuItem;


        private SolutionConfiguration configuration;

        private vsBuildAction currentBuildAction;


        protected override void Initialize()
        {
            base.Initialize();

            dte = (DTE2)GetService(typeof(DTE));
            outputPane = dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("Auto Deploy");
            errorListProvider = new ErrorListProvider(this);

            solutionEvents = dte.Events.SolutionEvents;
            solutionEvents.Opened += SolutionEvents_Opened;
            solutionEvents.AfterClosing += SolutionEvents_AfterClosing;

            buildEvents = dte.Events.BuildEvents;
            buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            buildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                var menuCommandID = new CommandID(GuidList.guidVsAutoDeployCmdSet, (int)PkgCmdIDList.cmdidEnabled);
                enabledMenuItem = new MenuCommand(EnabledMenuItem_Click, menuCommandID);
                mcs.AddCommand(enabledMenuItem);

                menuCommandID = new CommandID(GuidList.guidVsAutoDeployCmdSet, (int)PkgCmdIDList.cmdidOptions);
                optionsMenuItem = new MenuCommand(OptionsMenuItem_Click, menuCommandID);

                mcs.AddCommand(optionsMenuItem);
            }

            AddOptionKey(configurationKey);
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key != configurationKey)
                return;

            try
            {
                configuration = SolutionConfiguration.Load(stream);
            }
            catch (Exception ex)
            {
                configuration = new SolutionConfiguration();

                WriteLine("Cannot load configuration");
                WriteLine($"{ex.GetType().Name}: {ex.Message}");

                outputPane.Activate();
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key != configurationKey || configuration == null)
                return;

            try
            {
                SolutionConfiguration.Save(configuration, stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot save configuration.\r\n{ex.GetType().Name}: {ex.Message}");
            }
        }


        private void SolutionEvents_Opened()
        {
            enabledMenuItem.Visible = true;
            optionsMenuItem.Visible = true;

            if (configuration == null)
                configuration = new SolutionConfiguration();

            UpdateUI();
        }

        private void SolutionEvents_AfterClosing()
        {
            enabledMenuItem.Visible = false;
            optionsMenuItem.Visible = false;
            configuration = null;
        }


        private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            currentBuildAction = action;
            outputPane.Clear();
            errorListProvider.Tasks.Clear();
        }

        private void BuildEvents_OnBuildProjConfigDone(string projectName, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (!success || configuration == null || !configuration.IsEnabled)
                return;

            if (currentBuildAction != vsBuildAction.vsBuildActionBuild && currentBuildAction != vsBuildAction.vsBuildActionRebuildAll)
                return;

            var projectConfiguration = configuration.Projects.FirstOrDefault(p => p.ProjectName == projectName && p.IsEnabled);
            if (projectConfiguration == null || projectConfiguration.Files.Count == 0)
                return;

            var targetDirectory = !String.IsNullOrEmpty(projectConfiguration.TargetDirectory)
                ? projectConfiguration.TargetDirectory
                : configuration.TargetDirectory;

            if (String.IsNullOrEmpty(targetDirectory))
                return;

            targetDirectory = Environment.ExpandEnvironmentVariables(targetDirectory);

            var project = dte.Solution.GetProjects().FirstOrDefault(p => p.UniqueName == projectConfiguration.ProjectName);

            var outputPath = (string)project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value;
            var sourceDirectory = Path.Combine(Path.GetDirectoryName(project.FullName), outputPath);

            var statusBar = (IVsStatusbar)GetService(typeof(SVsStatusbar));
            uint cookie = 0;
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Deploy;

            try
            {
                var files = projectConfiguration.Files
                    .Where(p => !String.IsNullOrWhiteSpace(p))
                    .Select(Environment.ExpandEnvironmentVariables)
                    .SelectMany(p => Directory.GetFiles(sourceDirectory, p, projectConfiguration.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    .Distinct()
                    .OrderBy(p => p)
                    .Where(File.Exists)
                    .ToArray();

                if (files.Length == 0)
                    return;

                WriteLine($"====={projectName}=====");

                statusBar.Animation(1, ref icon);

                for (uint i = 0; i < files.Length; i++)
                {
                    var sourceFileName = files[i];

                    try
                    {
                        var fileInfo = new FileInfo(sourceFileName);

                        if (currentBuildAction == vsBuildAction.vsBuildActionBuild)
                        {
                            DateTime lastDate;
                            if (configuration.FilesCache.TryGetValue(fileInfo.Name, out lastDate) && fileInfo.LastWriteTime == lastDate)
                                continue;
                        }

                        var destFileName = Path.Combine(targetDirectory, sourceFileName.Substring(sourceDirectory.Length));
                        Write(sourceFileName + " -> ");

                        statusBar.Progress(ref cookie, 1, "", i, (uint)files.Length);

                        var destPath = Path.GetDirectoryName(destFileName);
                        if (!Directory.Exists(destPath))
                            Directory.CreateDirectory(destPath);

                        File.Copy(sourceFileName, destFileName, true);
                        configuration.FilesCache[fileInfo.Name] = fileInfo.LastWriteTime;

                        WriteLine(destFileName);
                    }
                    catch (Exception ex)
                    {
                        WriteLine(ex.Message);
                        outputPane.Activate();
                        
                        var error = new ErrorTask();
                        error.ErrorCategory = TaskErrorCategory.Error;
                        error.Category = TaskCategory.Misc;
                        error.Text = ex.Message;
                        error.Document = Path.GetFileName(sourceFileName);

                        if (GetService(typeof(SVsSolution)) is IVsSolution solution && solution.GetProjectOfUniqueName(project.FileName, out var hierarchyItem) == 0)
                            error.HierarchyItem = hierarchyItem;

                        errorListProvider.Tasks.Add(error);

                        dte.ExecuteCommand("View.ErrorList");
                    }
                }

                WriteLine($"====={projectName}=====");
                WriteLine("");
            }
            catch (Exception ex)
            {
                WriteLine($"====={projectName}=====");
                WriteLine(ex.Message);
                WriteLine($"====={projectName}=====");
                WriteLine("");

                outputPane.Activate();
            }
            finally
            {
                statusBar.Animation(0, ref icon);
                statusBar.Progress(ref cookie, 0, "", 0, 0);
            }
        }


        private void EnabledMenuItem_Click(object sender, EventArgs e)
        {
            if (configuration == null)
                return;

            configuration.IsEnabled = !configuration.IsEnabled;
            UpdateUI();
        }

        private void OptionsMenuItem_Click(object sender, EventArgs e)
        {
            if (configuration == null)
                return;

            var optionsViewModel = new OptionsViewModel(dte, configuration);
            var optionsView = new OptionsView(optionsViewModel);

            var interop = new WindowInteropHelper(optionsView);
            interop.EnsureHandle();
            interop.Owner = (IntPtr)dte.MainWindow.HWnd;

            optionsView.ShowDialog();

            UpdateUI();
        }


        private void Write(string message)
        {
            outputPane.OutputString(message);
        }

        private void WriteLine(string message)
        {
            outputPane.OutputString(message + "\n");
        }

        private void UpdateUI()
        {
            enabledMenuItem.Checked = configuration != null && configuration.IsEnabled;
        }
    }
}
