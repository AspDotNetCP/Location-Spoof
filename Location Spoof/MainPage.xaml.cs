using Location_Spoof.Model;
using Location_Spoof.ViewModels;

namespace Location_Spoof
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private MainPageViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainPageViewModel();
            BindingContext = viewModel;

#if WINDOWS
                var vm = new MainPageViewModel();
                _ = vm.LoadCountriesFromApiAsync();
#endif

        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.SearchCountries(e.NewTextValue);
        }

        private async void OnCountrySelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Country selected)
            {
                await Shell.Current.GoToAsync(nameof(CountryDetailPage), true, new Dictionary<string, object>
                {
                    { "SelectedCountry", selected }
                });

                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnLocationClicked(object? sender, EventArgs e)
        {
            //await Shell.Current.GoToAsync(nameof(LocationPage));
        }
    }
}
