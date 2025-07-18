using System.Windows.Input;
using ARM.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ARM.Views;
using System.Collections.ObjectModel;
using ARM.Models;
using ARM.Services;
using System.Linq;
using System;
using Serilog;

namespace ARM.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ICommand OpenSettingsViewCommand => new RelayCommand(OpenSettings);
    public ICommand OpenInfoViewCommand => new RelayCommand(OpenInfo);
    public ICommand OpenSpravocnikCommand => new RelayCommand(OpenSpravocnik);
    //public ICommand OpenReportsCommand => new RelayCommand(OpenReports);

    [ObservableProperty]
    public ObservableCollection<ARMReport> reports = new();

    [ObservableProperty]
    private ARMReport? _selectedReport;

    private PostModel? _selectedPost;

    private readonly IDBService _dbService;

    public ObservableCollection<PostGroupModel> AutoCisternGroups { get; } = new();
    public ObservableCollection<PostGroupModel> DispenserGroups { get; } = new();
    
    public MainViewModel(IDBService dbService)
    {
        _dbService = dbService;
        LoadPostsAsync();
        LoadListReports();
    }

    //генерация постов + сайд
    private async void LoadPostsAsync()
    {
        var postList = await (_dbService as PostgresDBService)?.GetPostsAsync();
        if (postList != null)
        {
            AutoCisternGroups.Clear();
            DispenserGroups.Clear();

            var autoGroups = postList
                .Where(p => p.MachineType == 0)
                .GroupBy(p => p.Side)
                .Select(g => new PostGroupModel
                {
                    Side = g.Key,
                    Posts = new ObservableCollection<PostModel>(
                        g.Select(post =>
                        {
                            post.SelectPostCommand = new RelayCommand(() => SelectedPost = post);
                            return post;
                        }))
                }).ToList();

            var dispenserGroups = postList
                .Where(p => p.MachineType == 1)
                .GroupBy(p => p.Side)
                .Select(g => new PostGroupModel
                {
                    Side = g.Key,
                    Posts = new ObservableCollection<PostModel>(
                        g.Select(post =>
                        {
                            post.SelectPostCommand = new RelayCommand(() => SelectedPost = post);
                            return post;
                        }))
                }).ToList();

            foreach (var group in autoGroups)
                AutoCisternGroups.Add(group);

            foreach (var group in dispenserGroups)
                DispenserGroups.Add(group);
        }
    }

    private async void LoadListReports()
    {
        try
        {
            var reportsFromDb = await _dbService.GetAllReportsAsync();
            Reports = new ObservableCollection<ARMReport>(reportsFromDb);
            Log.Information("Отчеты успешно загружены из базы данных");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при загрузке отчетов из базы данных"); // Использование Log
        }
    }

    public PostModel? SelectedPost
    {
        get => _selectedPost;
        set => SetProperty(ref _selectedPost, value);
    }
    public IRelayCommand<PostModel> SelectPostCommand => new RelayCommand<PostModel>(post => SelectedPost = post);


    private void OpenSettings()
    {
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var modal = new SettingsView { DataContext = new SettingsViewModel() };
        modal.ShowDialog(window);
    }
    
    private void OpenInfo()
    {
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var modal = new InfoPageView { DataContext = new InfoPageViewModel() };
        modal.ShowDialog(window);
    }

    private void OpenSpravocnik()
    { 
        var window = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var modal = new SpravocnikPostovView { DataContext = new SpravocnikPostovViewModel(_dbService) };
        modal.ShowDialog(window);
    }

    [RelayCommand]
    private void OpenReport(ARMReport report)
    {
        // Обработка открытия отчета
        Log.Information($"Открытие отчета: {report.Name}");
        // Здесь можно добавить логику открытия конкретного отчета
    }




}

