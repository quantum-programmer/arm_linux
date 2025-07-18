using ARM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ARM.Models;
using ARM.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace ARM.ViewModels
{
    internal class SpravocnikPostovViewModel : ViewModelBase
    {


        private readonly IDBService _dbService;
        public ObservableCollection<PostModel> Posts { get; set; }
        public PostModel? SelectedPost { get; set; }
        private ICommand _saveChangesCommand;
        public ICommand AddPostCommand { get; }
        public ICommand DeletePostCommand { get; }


        public SpravocnikPostovViewModel(IDBService dataService)
        {
            _dbService = dataService;
            Posts = new ObservableCollection<PostModel>();
            AddPostCommand = new RelayCommand(AddPost);
            DeletePostCommand = new RelayCommand(DeletePost, () => SelectedPost != null);
            LoadPosts();
        }

        private async void LoadPosts()
        {
            var posts = await  (_dbService as PostgresDBService)?.GetPostsAsync();
            foreach (var post in posts)
            {
                Posts.Add(post);
            }
        }

        private void AddPost()
        {
            var newPost = new PostModel { PostNumber = 0, id = 0, VehicleNumber = "", DriverName = "", FuelType = "", Dose = 0, Earth = 0, MachineType = 0, Side = 0, Volume = 0 }; // Значения по умолчанию
            Posts.Add(newPost);
            SelectedPost = newPost;
        }

        private void DeletePost()
        {
            if (SelectedPost != null)
            {
                Posts.Remove(SelectedPost);
                _ = (_dbService as PostgresDBService)?.DeletePostAsync(SelectedPost);
            }
        }

        public ICommand SaveChangesCommand
        {
            get
            {
                if (_saveChangesCommand == null)
                {
                    _saveChangesCommand = new RelayCommand(async () =>  await SaveChangesAsync());
                }
                return _saveChangesCommand;
            }
        }
        private async Task SaveChangesAsync()
        {
            try
            {
                foreach (var post in Posts)
                {
                    if (post.id == 0)
                        await (_dbService as PostgresDBService)?.AddPostAsync(post);
                    else
                        await (_dbService as PostgresDBService)?.UpdatePostAsync(post);
                }
            }
            catch (Exception ex)
            {
                //Log.Information($"Ошибка при сохранении данных: {ex.Message}");
            }
        }
    }
}
