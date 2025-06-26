using Location_Spoof.Model;

namespace Location_Spoof
{
    [QueryProperty(nameof(SelectedCountry), "SelectedCountry")]
    public partial class CountryDetailPage : ContentPage
    {
        private Country selectedCountry;
        public Country SelectedCountry
        {
            get => selectedCountry;
            set
            {
                selectedCountry = value;
                LoadData();
            }
        }

        public CountryDetailPage()
        {
            InitializeComponent();
        }

        private async void LoadData()
        {
            if (SelectedCountry == null) return;

            CountryNameLabel.Text = SelectedCountry.Name;
            CountryCodeLabel.Text = $"Code: {SelectedCountry.CountryCode}";
            FlagImage.Source = SelectedCountry.FlagImage;
            await DisplayAlert("Success", $"LoadData", "OK");

        }

        private async void OnViewMapClicked(object sender, EventArgs e)
        {
            if (SelectedCountry != null)
            {
                var url = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(SelectedCountry.Name)}";
                await Launcher.Default.OpenAsync(url);
            }
        }

    }
}
