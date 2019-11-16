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
using Microsoft.WindowsAPICodePack.Dialogs;
using MSBTool;


namespace FFXIVConductor
{
    public partial class MainWindow : Window
    {
        public static string FFXIVPath;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void pathButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog browser = new CommonOpenFileDialog();
            browser.IsFolderPicker = true;

            if (browser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathText.Text = browser.FileName;
                FFXIVPath = browser.FileName + "\\game\\sqpack\\ffxiv";
                midiButton1.IsEnabled = true;
                midiButton2.IsEnabled = true;
                midiButton3.IsEnabled = true;
            }
        }

        private void midiButton_Click(object sender, RoutedEventArgs e)
        {
            int id = int.Parse((sender as System.Windows.Controls.Button).Uid);
            CommonOpenFileDialog browser = new CommonOpenFileDialog();
            browser.Title = "Select Midi";
            browser.Filters.Add(new CommonFileDialogFilter("Midi", ".mid"));
            if(browser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                outputText.Text = Program.ImportMsb(browser.FileName, FFXIVPath, id);
            }
        }
    }
}
