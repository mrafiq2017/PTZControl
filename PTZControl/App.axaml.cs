using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using PTZControl.ViewModels;
using PTZControl.Views;

namespace PTZControl;

public partial class App : Application
{
    public override void Initialize()
    {
        Ioc.Default.ConfigureServices(ConfigureServices());
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        AddServicesForViewModels(services, "PTZControl.ViewModels");
        return services.BuildServiceProvider();
    }

    public void AddServicesForViewModels(IServiceCollection services, string folderNamespace)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith(folderNamespace) && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            if (type.Name.Contains("ViewModel"))
            {
                services.AddSingleton(type);
            }
        }
    }
}
