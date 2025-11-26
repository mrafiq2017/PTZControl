using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using PTZControl.ViewModels;
using System;

namespace PTZControl.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetService<MainViewModel>();
    }

    private void OnValueChange(object sender, SelectionChangedEventArgs e)
    {

    }

    private void UserControl_ActualThemeVariantChanged(object? sender, EventArgs e)
    {
    }

    private void Border_ActualThemeVariantChanged(object? sender, EventArgs e)
    {
    }
}
