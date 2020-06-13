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


namespace MesonPlugin
{
    /// <summary>
    /// Interaction logic for MesonConfiguration.xaml
    /// </summary>
    public partial class MesonConfigurationWindow : Window
    {
        public MesonConfigurationWindow()
        {
            InitializeComponent();
            this.DataContext = new MesonOptionsModel();
        }

        private void ButtonClickCancel(object sender, RoutedEventArgs e)            
        {
            this.Close();
        }

        private void ButtonClickOk(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
    public class MesonOption
    {
        public MesonOption(String optionName, String description, String value)
        {
            this.OptionName = optionName;
            this.Description = description;
            this.Value = value;
        }
        public string OptionName { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
    }

    public class MesonOptionsModel : INotifyPropertyChanged
    {
        public ObservableCollection<MesonOption> MesonOptions { get; set; }

        public MesonOptionsModel()
        {
            this.MesonOptions = new ObservableCollection<MesonOption>();
            MesonOptions.Add(new MesonOption("LTO", "Link time optimization", "False"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
