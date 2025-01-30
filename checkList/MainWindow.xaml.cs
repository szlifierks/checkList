using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using Microsoft.Win32;

namespace checkList
{
    public partial class MainWindow : Window
    {
        private const string RegistryKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "checkListApp";
        private bool isSubmitted;

        public MainWindow()
        {
            InitializeComponent();
        }

        /*private static void AddToStartup(string appName, string exePath)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(appName, exePath);
        }*/

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!isSubmitted)
            {
                e.Cancel = true;
                WindowState = WindowState.Normal;
                Activate();
            }
            base.OnClosing(e);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) => CheckSubmitButtonState();
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) => CheckSubmitButtonState();

        private void CheckSubmitButtonState()
        {
            SubmitButton.IsEnabled = new[] { CheckBox1, CheckBox2, CheckBox3, CheckBox4, CheckBox5, CheckBox6, CheckBox8, CheckBox9 }
                .All(cb => cb.IsChecked == true);
        }

        private void SubmitClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HasUnrecognizedDevices())
                {
                    MessageBox.Show("Wykryto nieznane urządzenia");
                    return;
                }

                GenerateReport();
                //TODO: wysylanie raportu na serwer i usuwanie raportu (patrz SelfDestruct)
                isSubmitted = true;
                SelfDestruct();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wysyłania: {ex.Message}");
            }
        }

        private void SelfDestruct()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string batchFilePath = Path.Combine(Path.GetTempPath(), "delete_exe.bat");

            File.WriteAllText(batchFilePath, $"@echo off\n:loop\ndel \"{exePath}\" >nul 2>&1\nif exist \"{exePath}\" goto loop\ndel %0");//skrypt usuwajacy pliki

            Process.Start(new ProcessStartInfo
            {
                FileName = batchFilePath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            Environment.Exit(0);
        }

        private bool HasUnrecognizedDevices()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode != 0");
                return searcher.Get().Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd sprawdzania urządzeń: {ex.Message}");
                return true;
            }
        }

        private void GenerateReport()
        {
            try
            {
                string exeDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) ??
                                      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filePath = Path.Combine(exeDirectory, "raport.txt");

                File.WriteAllLines(filePath, new[]
                {
                    $"Raport wygenerowano: {DateTime.Now}",
                    $"Nazwa komputera: {Environment.MachineName}",
                    $"Brak uszkodzeń obudowy: {CheckBox1.IsChecked}",
                    $"Sprawna klawiatura i touchpad: {CheckBox2.IsChecked}",
                    $"Sprawne porty: {CheckBox3.IsChecked}",
                    $"KB i aktualizacje z Altirisa: {CheckBox4.IsChecked}",
                    $"ESET: {CheckBox5.IsChecked}",
                    $"Zainstalowane sterowniki: {CheckBox6.IsChecked}",
                    $"Podstawowe aplikacje: {CheckBox7.IsChecked}",
                    $"Podstawowe aplikacje działają: {CheckBox8.IsChecked}",
                    $"Brak przegrzewania i działające chłodzenie: {CheckBox9.IsChecked}",
                    $"Włączony BitLocker: {CheckBox10.IsChecked}",
                    $"Działające WiFi: {CheckBox11.IsChecked}",
                    $"Przetestowane działanie FortiClienta: {CheckBox12.IsChecked}"
                });
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.WriteAllText(errorLogPath, ex.ToString());
                MessageBox.Show($"Błąd raportu: {ex.Message}\nSzczegóły zapisane w {errorLogPath}");
            }
        }
    }
}
