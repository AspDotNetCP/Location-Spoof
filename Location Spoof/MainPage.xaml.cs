using Location_Spoof.ViewModels;

namespace Location_Spoof
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel;
        private int count = 0;

        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainPageViewModel();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
#if ANDROID
                var permissionStatus = await RequestStoragePermissionsAsync();
                if (permissionStatus == PermissionStatus.Granted)
                {
                    // Proceed with storage operations
                    await viewModel.InitializeAsync();
                }
                else
                {
                    await DisplayAlert("Permission Denied", "Cannot proceed without storage access.", "OK");
                }
#else
                // For platforms that don’t need permission
                await viewModel.InitializeAsync();
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
            }
        }

        private async Task<PermissionStatus> RequestStoragePermissionsAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Error", "Storage permission is required to save flag images.", "OK");
                    }
                }
                return status;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                throw;
            }
            
        }
    }
}