using Microsoft.WindowsAPICodePack.Dialogs;
using MSBTool.Common;
using System;
using System.IO;
using System.Windows;


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
                midiExportButton.IsEnabled = true;
            }
        }

        private void midiButton_Click(object sender, RoutedEventArgs e)
        {
            int id = int.Parse((sender as System.Windows.Controls.Button).Uid);
            CommonOpenFileDialog browser = new CommonOpenFileDialog();
            browser.Title = "Select Midi";
            browser.Filters.Add(new CommonFileDialogFilter("Midi", ".mid"));
            if (browser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    MsbOperations.ImportMsb(browser.FileName, FFXIVPath, id);
                }
                catch (IOException)
                {
                    MessageBox.Show("Could not access game data. Is the game open?", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured. Please report this error.\n\n" + ex, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                MessageBox.Show($"Import to slot {id:D3} successful.", "OK!", MessageBoxButton.OK,
                    MessageBoxImage.Asterisk);
            }
        }

        private void MidiExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("exports");

            for (var i = 1; i < 4; i++)
            {
                MsbOperations.ExportMsb(i, FFXIVPath, Path.Combine("exports", $"score_{i:D3}.mid"));
            }

            MessageBox.Show("Export successful. See the /exports folder.", "OK!", MessageBoxButton.OK,
                MessageBoxImage.Asterisk);
        }
    }
}
