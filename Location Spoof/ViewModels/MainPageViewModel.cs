namespace Location_Spoof.ViewModels
{
    using Location_Spoof.Model;
    using Newtonsoft.Json.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Input;

    public class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<Country> CountryList { get; } = new();
        private string ImageFolder;
        private readonly string CacheFilePath;
        private bool _isLoading;
        private string _errorMessage;
        private string _searchText;
        private bool _isDropdownVisible;
        private CancellationTokenSource _searchCts;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                SearchCommand.Execute(value);
            }
        }

        public bool IsDropdownVisible
        {
            get => _isDropdownVisible;
            set
            {
                _isDropdownVisible = value;
                OnPropertyChanged(nameof(IsDropdownVisible));
            }
        }

        public ICommand SearchCommand => new Command<string>(async (text) =>
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(300, _searchCts.Token); // Debounce for 300ms
                SearchCountries(text);
            }
            catch (TaskCanceledException) { }
        });

        public ICommand SelectCountryCommand => new Command<Country>(async (country) =>
        {
            if (country != null)
            {
                try
                {
                    await Shell.Current.GoToAsync(nameof(CountryDetailPage), true, new Dictionary<string, object>
                    {
                        { "SelectedCountry", country }
                    });
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Failed to navigate: {ex.Message}";
                    Console.WriteLine($"🚨 Navigation error: {ex.Message}");
                }
            }
        });

        public ICommand ToggleDropdownCommand => new Command(() => IsDropdownVisible = !IsDropdownVisible);


        public MainPageViewModel()
        {
#if WINDOWS
            CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocationSpoof", "countries_cache.json");
#if DEBUG
            ImageFolder = GetImageFolder();
            if (!Directory.Exists(ImageFolder))
            {
                Console.WriteLine($"🚨 Development ImageFolder {ImageFolder} not found, using production path.");
                ImageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocationSpoof", "Flags");
            }
#else
            // Production path for Release builds
            ImageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocationSpoof", "Flags");
#endif
#else
            CacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "countries_cache.json");
            ImageFolder = Path.Combine(FileSystem.AppDataDirectory, "Flags");
#endif
            EnsureImageFolder();
            Console.WriteLine($"ImageFolder: {ImageFolder}");
            Console.WriteLine($"CacheFilePath: {CacheFilePath}");
        }

        internal async Task InitializeAsync()
        {
            await LoadCountriesFromApiAsync();

        }

        public async Task LoadCountriesFromApiAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                EnsureImageFolder();
                //EnsureCacheDirectory();
                var countries = await FetchCountryDataAsync();
                await Task.WhenAll(countries.Select(DownloadFlagIfNeededAsync));
                CountryList.Clear();
                foreach (var country in countries.OrderBy(c => c.Name))
                {
                    CountryList.Add(new Country
                    {
                        Name = country.Name,
                        CountryCode = country.Code,
                        FlagImage = ImageSource.FromFile(GetLocalFlagPath(country.Code))
                    });
                }
                Console.WriteLine("✅ All countries processed.");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load countries: {ex.Message}";
                Console.WriteLine($"🚨 Error loading countries: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void EnsureImageFolder()
        {
            try
            {
                if (!Directory.Exists(ImageFolder))
                    Directory.CreateDirectory(ImageFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Failed to create ImageFolder {ImageFolder}: {ex.Message}");
#if WINDOWS
                ImageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocationSpoof", "FallbackFlags");
#else
                ImageFolder = Path.Combine(FileSystem.AppDataDirectory, "FallbackFlags");
#endif
                if (!Directory.Exists(ImageFolder))
                    Directory.CreateDirectory(ImageFolder);
                Console.WriteLine($"Using fallback ImageFolder: {ImageFolder}");
            }
        }


        private string GetImageFolder()
        {
            // Option 1: Development - Try project directory
            try
            {
                var currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                int i = 0;
                while (!string.IsNullOrEmpty(currentDir))
                {
                    if (File.Exists(Path.Combine(currentDir, "Location Spoof.csproj")) && i < 5)
                    {
                        var devPath = Path.Combine(currentDir, "Resources", "Images");
                        Console.WriteLine($"Development ImageFolder found: {devPath}");
                        return devPath;
                    }
                    currentDir = Directory.GetParent(currentDir)?.FullName;

                    i++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error finding development ImageFolder: {ex.Message}");
            }

            // Option 2: Production - Use LocalApplicationData
            var prodPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocationSpoof", "Flags");
            Console.WriteLine($"Using production ImageFolder: {prodPath}");
            return prodPath;
        }


        private void EnsureCacheDirectory()
        {
            var cacheDirectory = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(cacheDirectory) && !Directory.Exists(cacheDirectory))
                Directory.CreateDirectory(cacheDirectory);
        }

        private async Task<List<(string Name, string Code, string ImageUrl)>> FetchCountryDataAsync()
        {
            try
            {
                EnsureCacheDirectory();
#if WINDOWS
                Console.WriteLine($"Checking Windows cache at {CacheFilePath}");
                if (File.Exists(CacheFilePath) && new FileInfo(CacheFilePath).LastWriteTime > DateTime.Now.AddDays(-7))
                {
                    Console.WriteLine($"✅ Using cached data from {CacheFilePath}");
                    var cachedJson = await File.ReadAllTextAsync(CacheFilePath);
                    var cachedArray = JArray.Parse(cachedJson);
                    return ParseCountries(cachedArray);
                }
                Console.WriteLine($"⚠️ Cache not found or outdated at {CacheFilePath}, fetching from API");
#else
                Console.WriteLine($"Checking Android cache at {CacheFilePath}");
                if (File.Exists(CacheFilePath) && new FileInfo(CacheFilePath).LastWriteTime > DateTime.Now.AddDays(-7))
                {
                    Console.WriteLine($"✅ Using cached data from {CacheFilePath}");
                    var cachedJson = await File.ReadAllTextAsync(CacheFilePath);
                    var cachedArray = JArray.Parse(cachedJson);
                    return ParseCountries(cachedArray);
                }
                Console.WriteLine($"⚠️ Cache not found or outdated at {CacheFilePath}, fetching from API");
#endif
                var url = "https://restcountries.com/v3.1/all?fields=name,cca2,flags";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LocationSpoof/1.0 (compatible; .NET MAUI)");
                var json = await client.GetStringAsync(url);
                await File.WriteAllTextAsync(CacheFilePath, json);
                Console.WriteLine($"✅ Saved fresh data to cache at {CacheFilePath}");
                var array = JArray.Parse(json);
                return ParseCountries(array);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to fetch or parse country data: {ex.Message}";
                Console.WriteLine($"🚨 Error fetching or parsing country data: {ex.Message}");
                throw;
            }
        }

        private List<(string Name, string Code, string ImageUrl)> ParseCountries(JArray array)
        {
            var countries = new List<(string Name, string Code, string ImageUrl)>();
            foreach (var obj in array)
            {
                var name = (string)obj["name"]?["common"] ?? "Unknown";
                var code = (string)obj["cca2"] ?? "";
                var imageUrl = (string)obj["flags"]?["png"] ?? "";
                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(imageUrl))
                {
                    countries.Add((name, code, imageUrl));
                }
            }
            return countries;
        }

        private async Task DownloadFlagIfNeededAsync((string Name, string Code, string ImageUrl) country)
        {
            var localPath = GetLocalFlagPath(country.Code);
#if ANDROID
            // Android: Download to AppDataDirectory\Flags if not found
            var androidFlagPath = Path.Combine(FileSystem.AppDataDirectory, "Flags", $"flag_{country.Code.ToLowerInvariant()}.png");
            if (!File.Exists(androidFlagPath))
            {
                try
                {
                    using var client = new HttpClient();
                    var bytes = await client.GetByteArrayAsync(country.ImageUrl);
                    EnsureImageFolder(); // Creates AppDataDirectory\Flags and AlternativeFlags
                    await File.WriteAllBytesAsync(androidFlagPath, bytes);
                    Console.WriteLine($"✅ Downloaded flag for {country.Code} to {androidFlagPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to download {country.Code} to {androidFlagPath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"✅ Flag for {country.Code} already exists at {androidFlagPath}");
            }
#else
            // Windows: Download to ImageFolder if not found in ImageFolder or AlternativeFlags
            if (!File.Exists(localPath) && localPath != "default_flag.png")
            {
                try
                {
                    using var client = new HttpClient();
                    var bytes = await client.GetByteArrayAsync(country.ImageUrl);
                    EnsureImageFolder(); // Creates ImageFolder and AlternativeFlags
                    var targetPath = Path.Combine(ImageFolder, $"flag_{country.Code.ToLowerInvariant()}.png");
                    await File.WriteAllBytesAsync(targetPath, bytes);
                    Console.WriteLine($"✅ Downloaded flag for {country.Code} to {targetPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to download {country.Code} to {ImageFolder}: {ex.Message}");
                }
            }
            else if (File.Exists(localPath))
            {
                Console.WriteLine($"✅ Flag for {country.Code} already exists at {localPath}");
            }
#endif
        }

        private string GetLocalFlagPath(string code)
        {
            var safeCode = code.ToLowerInvariant();
            var fileName = $"flag_{safeCode}.png";
            var path = Path.Combine(ImageFolder ?? string.Empty, fileName);
            return !string.IsNullOrEmpty(ImageFolder) && File.Exists(path) ? path : "default_flag.png";
        }

        private void SearchCountries(string text)
        {
            var filtered = CountryList
                .Where(c => string.IsNullOrWhiteSpace(text) || c.Name.ToLower().Contains(text.ToLower()))
                .ToList();
            CountryList.Clear();
            foreach (var c in filtered)
                CountryList.Add(c);
            // Alternatively: CountryList = new ObservableCollection<Country>(filtered);
        }
    }

}
