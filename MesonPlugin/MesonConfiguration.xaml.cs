using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace MesonPlugin
{
    public static class CommandLineHelper
    {
        public static (string, int) RunCommand(string cmd)
        {
            // Get directory of the currently open solution.
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);

            // Run the command at the solution working directory.
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = solutionDir;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c " + cmd;
            process.StartInfo = startInfo;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (stdout, process.ExitCode);
        }
    }

    /// <summary>
    /// Interaction logic for MesonConfiguration.xaml
    /// </summary>
    public partial class MesonConfigurationWindow : System.Windows.Window
    {
        private MesonOptionsModel OptionsModel;
        public MesonConfigurationWindow()
        {
            InitializeComponent();
            OptionsModel = new MesonOptionsModel();
            this.DataContext = OptionsModel;
        }

        private void ButtonClickCancel(object sender, RoutedEventArgs e)            
        {
            this.Close();
        }

        private void ButtonClickOk(object sender, RoutedEventArgs e)
        {
            this.Close();
            string configureOptions = "";
            // These options use "--" prefix instead of "-D" for some reason.
            List<string> weirdOptions = new List<string> { "backend", "buildtype", "unity",
                "layout", "default-library", "warnlevel"};
            foreach (MesonOption option in OptionsModel.MesonOptions)
            {
                if (weirdOptions.Contains(option.OptionName))
                {
                    configureOptions += " --" + option.OptionName + "=" + option.SelectedOption;
                }
                else
                {
                    configureOptions += " -D" + option.OptionName + "=" + option.SelectedOption;
                }
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            CommandLineHelper.RunCommand("meson configure " + configureOptions);
        }

    }
    public class MesonOption
    {
        public MesonOption(String optionName, 
            String description, 
            List<String> availableOptions, 
            string selectedOption)
        {
            this.OptionName = optionName;
            this.Description = description;
            this.AvailableOptions = availableOptions;
            this.SelectedOption = selectedOption;
        }
        public String OptionName { get; set; }
        public String Description { get; set; }
        public List<String> AvailableOptions { get; set; }
        public String SelectedOption { get; set; }
    }

    public class MesonOptionsModel : INotifyPropertyChanged
    {
        public ObservableCollection<MesonOption> MesonOptions { get; set; }
        public MesonOptionsModel()
        {
            
            this.MesonOptions = new ObservableCollection<MesonOption>();
            ThreadHelper.ThrowIfNotOnUIThread();
            var mesonIntrospect = CommandLineHelper.RunCommand("meson introspect --buildoptions");
            string stdout = mesonIntrospect.Item1;
            int exitCode = mesonIntrospect.Item2;
            
            if (exitCode != 0)
            {
                // Something went wrong, show error message. Maybe solution is not meson generated.
                System.Windows.MessageBox.Show(stdout);
                return;
            }

            JArray parsedOptions = JArray.Parse(@stdout);
            foreach (JObject option in parsedOptions)
            {
                string type = option.GetValue("type").ToString();
                // Skip non-boolean/combo options since the window to change option does not yet support different types
                // in the value column. Also skip build machine options because I'm not sure when they are needed.
                bool build_machine_option = option.GetValue("machine").ToString() == "build";
                if ((type != "combo" && type != "boolean") || build_machine_option)
                {
                    continue;
                }

                List<String> availableOptions = new List<String>();
                if (type == "combo") 
                {
                    availableOptions = option.GetValue("choices").ToObject<List<string>>();
                   
                }
                else if (type == "boolean") 
                {
                    availableOptions = new List<String> { "False", "True" };
                  
                }
                string optionName = option.GetValue("name").ToString();
                string description = option.GetValue("description").ToString();
                string selectedOption = option.GetValue("value").ToString();
                MesonOptions.Add(new MesonOption(optionName, description, availableOptions, selectedOption));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
