using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace MesonPlugin
{
    public static class Helpers
    {
        public static (string, int) RunCommand(string cmd)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Run the command at the solution root.
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = GetSolutionDirectory();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c " + cmd;
            process.StartInfo = startInfo;
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (stdout, process.ExitCode);
        }

        public static string GetSolutionDirectory()
        {
            // Get directory of the currently open solution.
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            return System.IO.Path.GetDirectoryName(dte.Solution.FullName);
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
            ThreadHelper.ThrowIfNotOnUIThread();
            this.Close();
            ConfigureSolution();
            RegenerateSolution();
        }

        private static void RegenerateSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            XmlDocument regenProject = new XmlDocument();
            regenProject.Load(Helpers.GetSolutionDirectory() + "\\REGEN.vcxproj");
            string configuration = regenProject.GetElementsByTagName("Configuration").Item(0).InnerText;
            string platform = regenProject.GetElementsByTagName("Platform").Item(0).InnerText;

            // Find regeneration command from the project
            string regen_command = "";
            XmlNode custom = regenProject.GetElementsByTagName("CustomBuild").Item(0);
            foreach (XmlNode child in custom.ChildNodes)
            {
                if (child.Name == "Command")
                {
                    regen_command = child.InnerText;
                }
            }

            // Get line with vcvarsall.bat call to invoke msbuild which handle the solution regeneration
            string[] lines = regen_command.Split('\n');
            foreach (string line in lines)
            {
                if (line.Contains("vcvarsall.bat"))
                {
                    var (stdout, exitCode) = Helpers.RunCommand(line + " && msbuild REGEN.vcxproj /p:Platform=" + platform + " /p:Configuration=" + configuration);
                    if (exitCode != 0)
                    {
                        System.Windows.MessageBox.Show(stdout);
                    }
                }

            }
        }

        private void ConfigureSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string configureOptions = "";
            // These options use "--" prefix instead of "-D" for some reason.
            List<string> weirdOptions = new List<string> { "backend", "buildtype", "unity",
                "layout", "default-library", "warnlevel"};
            // These options are skipped because they are not used in windows
            List<string> skippedOptions = new List<string> { "install_umask" };
            foreach (MesonOption option in OptionsModel.MesonOptions)
            {
                if (weirdOptions.Contains(option.Name))
                {
                    configureOptions += " --" + option.Name + "=" + option.SelectedOption;
                }
                else if (skippedOptions.Contains(option.Name))
                {
                    // Do nothing
                }
                else
                {
                    if (option.SelectedOption != "[]") // Do nothing if the value is an empty list
                    {
                        configureOptions += " -D" + option.Name + "=" + option.SelectedOption;
                    }
                }
            }
            configureOptions = configureOptions.Replace(System.Environment.NewLine, "");
            configureOptions = configureOptions.Replace("[", "");
            configureOptions = configureOptions.Replace("]", "");
            // This might be slightly dangerous but it is needed because in the lists there are two spaces between
            // values whereas they should be comma-separated with no sapces
            configureOptions = configureOptions.Replace("  ", "");

            var (stdout, exitCode) = Helpers.RunCommand("meson configure " + configureOptions);
            if (exitCode != 0)
            {
                System.Windows.MessageBox.Show("Configure command \"meson configure " + configureOptions + "\"\n failed" + stdout);
            }
        }
    }

    public class MesonOption
    {
        public MesonOption(String name, 
            String description, 
            List<String> availableOptions, 
            string selectedOption,
            bool editable)
        {
            this.Name = name;
            this.Description = description;
            this.AvailableOptions = availableOptions;
            this.SelectedOption = selectedOption;
            this.Editable = editable;
        }
        public String Name { get; set; }
        public String Description { get; set; }
        public List<String> AvailableOptions { get; set; }
        public String SelectedOption { get; set; }
        public bool Editable { get; set; }
    }

    public class MesonOptionsModel : INotifyPropertyChanged
    {
        public ObservableCollection<MesonOption> MesonOptions { get; set; }
        public MesonOptionsModel()
        {
            
            this.MesonOptions = new ObservableCollection<MesonOption>();
            ThreadHelper.ThrowIfNotOnUIThread();
            var mesonIntrospect = Helpers.RunCommand("meson introspect --buildoptions");
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
                // Skip build machine options because I'm not sure when they are needed.
                if (option.GetValue("machine").ToString() == "build")
                {
                    continue;
                }

                List<String> availableOptions = new List<String>();
                bool editable = false;
                if (type == "combo") 
                {
                    availableOptions = option.GetValue("choices").ToObject<List<string>>();
                }
                else if (type == "boolean") 
                {
                    availableOptions = new List<String> { "False", "True" };
                }
                else
                {
                    editable = true;
                }
                string name = option.GetValue("name").ToString();
                string description = option.GetValue("description").ToString();
                string selectedOption = option.GetValue("value").ToString();
                MesonOptions.Add(new MesonOption(name, description, availableOptions, selectedOption, editable));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
