using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GetStartedApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void Button_Click(object sender, RoutedEventArgs args)
    {
        Debug.WriteLine($"Click! Celsisu={Celsius.Text}");
        if (Double.TryParse(Celsius.Text, out double celsius))
        {
            var F = celsius * (9d / 5d) + 32;
            Fahrenheit.Text = F.ToString("0.0");
        }
        else
        {
            Celsius.Text = "0";
            Fahrenheit.Text = "0";
        }
    }
}
