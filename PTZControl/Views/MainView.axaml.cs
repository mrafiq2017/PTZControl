using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using PTZControl.ViewModels;

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
        var vm = DataContext as MainViewModel;
        vm.OnValueChange(sender);
    }

    private void ComPortDropdown_DropDownOpened(object sender, EventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm.LoadComPorts();
    }

    private void UserControl_ActualThemeVariantChanged(object? sender, EventArgs e)
    {
    }
}
