using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace checkList;

public partial class MainWindow : Window
{
    private const string RegistryKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppName = "checkListApp";
    private bool isSubmitted;

    private DispatcherTimer popupTimer;
    private string tempImagePath1;
    private string tempImagePath2;
    private string tempImagePath3;

    public MainWindow()
    {
        InitializeComponent();
        InitializePopupTimer();
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        
        string appName = "checkListApp";
        
        //AddToStartup(appName, exePath);
        
    }

    static void AddToStartup(string appName, string exePath)
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
        {
            if (key == null)
            {
                MessageBox.Show("Nie można otworzyć klucza rejestru.");
                return;
            }
            
            key.SetValue(appName, exePath);
        }
    }
    private void InitializePopupTimer()
    {
        popupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10) //czas w sekundach po ktorym wraca na wierzch
        };
        popupTimer.Tick += PopupTimer_Tick;
        popupTimer.Start();
    }

    private void PopupTimer_Tick(object sender, EventArgs e)
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
    }

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

    private void AttachPicture1_Click(object sender, RoutedEventArgs e)
    {
        tempImagePath1 = AttachPicture();
        UpdateTextBlockColor(TextBlock1, tempImagePath1);
        CheckSubmitButtonState();
    }

    private void AttachPicture2_Click(object sender, RoutedEventArgs e)
    {
        tempImagePath2 = AttachPicture();
        UpdateTextBlockColor(TextBlock2, tempImagePath2);
        CheckSubmitButtonState();
    }

    private void AttachPicture3_Click(object sender, RoutedEventArgs e)
    {
        tempImagePath3 = AttachPicture();
        UpdateTextBlockColor(TextBlock3, tempImagePath3);
        CheckSubmitButtonState();
    }

    private string AttachPicture()
    {
        string tempFilePath = null;

        try
        {
            if (Clipboard.ContainsImage()) //na poczatku sprawdzam schowek bo wygodniej
            {
                var clipboardImage = Clipboard.GetImage();
                tempFilePath = SaveImageToTempFile(clipboardImage);
            }
            else
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var bitmap = new BitmapImage(new Uri(filePath));
                    tempFilePath = SaveImageToTempFile(bitmap);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to attach picture: {ex.Message}");
        }

        return tempFilePath;
    }

    private string SaveImageToTempFile(BitmapSource image)
    {
        var tempFilePath = Path.GetTempFileName() + ".png";
        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save image: {ex.Message}");
        }

        return tempFilePath;
    }

    private void UpdateTextBlockColor(TextBlock textBlock, string filePath)
    {
        textBlock.Foreground = string.IsNullOrEmpty(filePath) ? Brushes.Red : Brushes.Green;
    }

    private void CheckSubmitButtonState()
    {
        SubmitButton.IsEnabled = !string.IsNullOrEmpty(tempImagePath1) && !string.IsNullOrEmpty(tempImagePath2) &&
                                 !string.IsNullOrEmpty(tempImagePath3);
    }

    private void SubmitClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: wysylac pliki na serwer

            var computerName = Environment.MachineName;

            // usuwanie lokalnych plików
            DeleteTempFile(tempImagePath1);
            DeleteTempFile(tempImagePath2);
            DeleteTempFile(tempImagePath3);

            isSubmitted = true;
            MessageBox.Show($"wyslano z: {computerName}");
            SelfDestruct(); //usuwanie wszystkiego wtf
        }
        catch (Exception ex)
        {
            MessageBox.Show($"blad wysylania: {ex.Message}");
        }
    }

    private void DeleteTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"blad usuwania plikow: {ex.Message}");
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
}