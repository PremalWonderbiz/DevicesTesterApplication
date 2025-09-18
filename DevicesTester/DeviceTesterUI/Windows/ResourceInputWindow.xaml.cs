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
using System.Windows.Shapes;

namespace DeviceTesterUI.Windows
{
    /// <summary>
    /// Interaction logic for ResourceInputWindow.xaml
    /// </summary>
    public partial class ResourceInputWindow : Window
    {
        public string ResourceText
        {
            get => ResourceInputTextBox.Text;
            set => ResourceInputTextBox.Text = value; 
        }

        public ResourceInputWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ResourceText = ResourceInputTextBox.Text;
            DialogResult = true; // Closes the window and returns success
        }
    }
}
