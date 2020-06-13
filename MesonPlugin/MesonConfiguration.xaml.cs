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

namespace MesonPlugin
{
    public static class CommandLineHelper
    {
        public static string RunCommand(string cmd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            string solutionDir = System.IO.Path.GetDirectoryName(dte.Solution.FullName);

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = solutionDir;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c " + cmd;
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
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
            foreach (MesonOption option in OptionsModel.MesonOptions)
            {
                if (option.Section == "core")
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
            string selectedOption,
            string section)
        {
            this.OptionName = optionName;
            this.Description = description;
            this.AvailableOptions = availableOptions;
            this.SelectedOption = selectedOption;
            this.Section = section;
        }
        public String OptionName { get; set; }
        public String Description { get; set; }
        public List<String> AvailableOptions { get; set; }
        public String SelectedOption { get; set; }
        public String Section { get; set; }
    }

    public class MesonOptionsModel : INotifyPropertyChanged
    {
        public ObservableCollection<MesonOption> MesonOptions { get; set; }
        public MesonOptionsModel()
        {
            
            this.MesonOptions = new ObservableCollection<MesonOption>();
            ThreadHelper.ThrowIfNotOnUIThread();
            string mesonBuildOptions = CommandLineHelper.RunCommand("meson introspect --buildoptions").Trim();

            JArray parsedOptions = JArray.Parse(@mesonBuildOptions);
            foreach (JObject option in parsedOptions)
            {
                string type = option.GetValue("type").ToString();
                if (type != "combo" && type != "boolean")
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
                string section = option.GetValue("section").ToString();
                MesonOption new_opt = new MesonOption(optionName, description, availableOptions, selectedOption, section);
                MesonOptions.Add(new_opt);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
