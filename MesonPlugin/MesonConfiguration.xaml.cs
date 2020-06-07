using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MesonPlugin
{
    /// <summary>
    /// Interaction logic for MesonConfiguration.xaml
    /// </summary>
    public partial class MesonConfiguration : Window
    {
        public MesonConfiguration()
        {
            InitializeComponent();
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
}
