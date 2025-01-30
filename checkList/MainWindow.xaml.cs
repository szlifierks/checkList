using System.ComponentModel;
using System.Diagnostics;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Management;
using Microsoft.Win32;
using System.Threading;

namespace checkList
{
    public partial class MainWindow : Window
    {
        private const string RegistryKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "checkListApp";
        private bool isSubmitted;

        //private DispatcherTimer popupTimer;

        public MainWindow()
        {
            InitializeComponent();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrEmpty(exePath))
            {
                exePath = AppDomain.CurrentDomain.BaseDirectory;
            }

            //MessageBox.Show($"Executable Path: {exePath}");
        }

        static void AddToStartup(string appName, string exePath)
        {
            using (RegistryKey key =
                   Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key == null)
                {
                    MessageBox.Show("Nie można otworzyć klucza rejestru.");
                    return;
                }

                key.SetValue(appName, exePath);
            }
        }

        /*private void InitializePopupTimer()
        {
            popupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10) //czas w sekundach po ktorym wraca na wierzch
            };
            popupTimer.Tick += PopupTimer_Tick;
            popupTimer.Start();
        }*/

        /*private void PopupTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!isSubmitted)
                {
                    Topmost = true;
                    Activate();
                    Topmost = false;
                }
                else
                {
                    popupTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Popup timer error: {ex.Message}");
            }
        }*/

        protected override void OnClosing(CancelEventArgs e) //nie mozna zamknac bez wyslania
        {
            if (!isSubmitted)
            {
                e.Cancel = true;
                WindowState = WindowState.Normal;
                Activate();
            }

            base.OnClosing(e);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckSubmitButtonState();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckSubmitButtonState();
        }

        private void CheckSubmitButtonState()
        {
            SubmitButton.IsEnabled = CheckBox1.IsChecked == true && CheckBox2.IsChecked == true &&
                                     CheckBox3.IsChecked == true
                                     && CheckBox4.IsChecked == true && CheckBox5.IsChecked == true &&
                                     CheckBox6.IsChecked == true
                                     && CheckBox8.IsChecked == true && CheckBox9.IsChecked == true;
        }

        private void SubmitClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HasUnrecognizedDevices())
                {
                    MessageBox.Show("wykryto nieznane urzadzenia");
                    return;
                }

                var computerName = Environment.MachineName;

                GenerateReport();

                // TODO: wysylac pliki na serwer

                isSubmitted = true;
                MessageBox.Show($"wyslano z: {computerName}");
                SelfDestruct(); //usuwanie wszystkiego wtf
            }
            catch (Exception ex)
            {
                MessageBox.Show($"blad wysylania: {ex.Message}");
            }
        }

        private void SelfDestruct()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            string batchScript = @"
@echo off
:loop
del """ + exePath + @""" >nul 2>&1
if exist """ + exePath + @""" goto loop
del %0";

            string batchFilePath = Path.Combine(Path.GetTempPath(), "delete_exe.bat");
            File.WriteAllText(batchFilePath, batchScript);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = batchFilePath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);
            Thread.Sleep(1000);
            Environment.Exit(0);
        }

        private bool HasUnrecognizedDevices()
        {
            try
            {
                using (var searcher =
                       new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode != 0"))
                {
                    var devices = searcher.Get();
                    return devices.Count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking devices: {ex.Message}");
                return true; //jesli zwroci blad zakladam ze czegos nie rozpoznalo
            }
        }

        private void GenerateReport()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = AppDomain.CurrentDomain.BaseDirectory;
                }

                string exeDirectory = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(exeDirectory) || !Directory.Exists(exeDirectory))
                {
                    exeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                string filePath = Path.Combine(exeDirectory, "raport.txt");

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine($"Device Name: {Environment.MachineName}");
                    writer.WriteLine($"brak uszkodzeń obudowy: {CheckBox1.IsChecked}");
                    writer.WriteLine($"sprawna klawiatura i touchpad: {CheckBox2.IsChecked}");
                    writer.WriteLine($"sprawne porty (USB, HDMI, itp.): {CheckBox3.IsChecked}");
                    writer.WriteLine($"KB i inne aktualizacje z Altirisa zainstalowane: {CheckBox4.IsChecked}");
                    writer.WriteLine($"ESET: {CheckBox5.IsChecked}");
                    writer.WriteLine($"zainstalowane sterowniki: {CheckBox6.IsChecked}");
                    writer.WriteLine($"zainstalowane podstawowe aplikacje: {CheckBox7.IsChecked}");
                    writer.WriteLine($"sprawne uruchamianie i działanie podstawowych aplikacji: {CheckBox8.IsChecked}");
                    writer.WriteLine($"brak przegrzewania i działające chłodzenie: {CheckBox9.IsChecked}");
                    writer.WriteLine($"włączony BitLocker: {CheckBox10.IsChecked}");
                    writer.WriteLine($"działające WiFi: {CheckBox11.IsChecked}");
                    writer.WriteLine($"przetestowane działanie FortiClienta: {CheckBox12.IsChecked}");
                }

                //MessageBox.Show($"Raport zapisany w: {filePath}");
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
                File.WriteAllText(errorLogPath, ex.ToString());
                MessageBox.Show($"Error raportu: {ex.Message}\nSzczegóły zapisane w {errorLogPath}");
            }
        }
    }
}