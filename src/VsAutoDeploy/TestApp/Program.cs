using System;
using VsAutoDeploy;

#pragma warning disable 618

namespace TestApp
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var optionsViewModel = new OptionsViewModel();
            var optionsView = new OptionsView(optionsViewModel);
            optionsView.ShowDialog();
        }
    }
}
