using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using PTZControl.ViewModels;

namespace PTZControl.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        InitializeButtonHandlers();
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

    #region Press/Release Handlers

    private void Move_Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl)
        {
            if (DataContext is MainViewModel vm)
            {
                if (ctrl.Name == "moveUp")
                    vm.MoveUpCommand.Execute(null);
                else if (ctrl.Name == "moveDown")
                    vm.MoveDownCommand.Execute(null);
                else if (ctrl.Name == "moveLeft")
                    vm.MoveLeftCommand.Execute(null);
                else if (ctrl.Name == "moveRight")
                    vm.MoveRightCommand.Execute(null);
            }
        }
    }

    private void Move_Released(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.StopMotorCommand.Execute(null);
    }

    private void InitializeButtonHandlers()
    {
        moveUp.AddHandler(
            InputElement.PointerPressedEvent,
            Move_Pressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveUp.AddHandler(
            InputElement.PointerReleasedEvent,
            Move_Released,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveDown.AddHandler(
            InputElement.PointerPressedEvent,
            Move_Pressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveDown.AddHandler(
            InputElement.PointerReleasedEvent,
            Move_Released,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveLeft.AddHandler(
            InputElement.PointerPressedEvent,
            Move_Pressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveLeft.AddHandler(
            InputElement.PointerReleasedEvent,
            Move_Released,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveRight.AddHandler(
            InputElement.PointerPressedEvent,
            Move_Pressed,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        moveRight.AddHandler(
            InputElement.PointerReleasedEvent,
            Move_Released,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    #endregion
}
