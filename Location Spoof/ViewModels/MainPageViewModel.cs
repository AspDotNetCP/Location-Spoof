namespace Location_Spoof.ViewModels
{
    using Location_Spoof.Model;
    using Newtonsoft.Json.Linq;
    using System.Collections.ObjectModel;

    public class MainPageViewModel
    {
        public ObservableCollection<Country> CountryList { get; } = new();
        private readonly string ImageFolder;
#if WINDOWS
        private readonly string CacheFilePath = @"C:\Users\keyma\source\repos\Location Spoof\Location Spoof\Resources\countries_cache.json";
#else
        private readonly string CacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "countries_cache.json");
#endif

        public MainPageViewModel()
        {
#if ANDROID
                ImageFolder = Path.Combine(FileSystem.AppDataDirectory, "Flags");
#else
            // Windows or other platforms
            ImageFolder = @"C:\Users\keyma\source\repos\Location Spoof\Location Spoof\Resources\Images\";
#endif
            // Fix: Removed 'await' and called the method without awaiting since constructors cannot be async.
            LoadCountriesFromApiAsync().ConfigureAwait(false);
        }

        public async Task LoadCountriesFromApiAsync()
        {
            try
            {
                EnsureImageFolder();

                var countries = await FetchCountryDataAsync();

                CountryList.Clear();

                foreach (var country in countries)
                {
                    await DownloadFlagIfNeededAsync(country);

                    CountryList.Add(new Country
                    {
                        Name = country.Name,
                        CountryCode = country.Code,
                        FlagImage = GetLocalFlagPath(country.Code)
                    });
                }

                Console.WriteLine("✅ All countries processed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error loading countries: {ex.Message}");
            }
        }

        private void EnsureImageFolder()
        {
            if (!Directory.Exists(ImageFolder))
                Directory.CreateDirectory(ImageFolder);
        }

        private async Task<List<(string Name, string Code, string ImageUrl)>> FetchCountryDataAsync()
        {
            try
            {
                // Load from cache if available
                if (File.Exists(CacheFilePath))
                {
                    var cachedJson = await File.ReadAllTextAsync(CacheFilePath);
                    var cachedArray = JArray.Parse(cachedJson);

                    return ParseCountries(cachedArray);
                }

                // Else fetch from API
                var url = "https://restcountries.com/v3.1/all?fields=name,cca2,flags";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                var json = await client.GetStringAsync(url);
                await File.WriteAllTextAsync(CacheFilePath, json); // Save for future use

                var array = JArray.Parse(json);
                return ParseCountries(array);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error fetching or parsing country data: {ex.Message}");
                return new List<(string, string, string)>();
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

            if (!File.Exists(localPath))
            {
                try
                {
                    using var client = new HttpClient();
                    var bytes = await client.GetByteArrayAsync(country.ImageUrl);
                    await File.WriteAllBytesAsync(localPath, bytes);
                    Console.WriteLine($"✅ Downloaded flag for {country.Code}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to download {country.Code}: {ex.Message}");
                }
            }
        }

        private string GetLocalFlagPath(string code)
        {
            var safeCode = code.ToLowerInvariant();
            var fileName = $"flag_{safeCode}.png";
            return Path.Combine(ImageFolder, $"{fileName}");
        }

        private List<Country> AllCountries => new()
            {
                new Country { Name = "Malaysia", CountryCode = "MY" },
                new Country { Name = "Singapore", CountryCode = "SG" },
                new Country { Name = "Thailand", CountryCode = "TH" },
                new Country { Name = "Vietnam", CountryCode = "VN" }
            };

        public void SearchCountries(string text)
        {
            var filtered = AllCountries
                .Where(c => string.IsNullOrWhiteSpace(text) || c.Name.ToLower().Contains(text.ToLower()))
                .ToList();

            CountryList.Clear();
            foreach (var c in filtered)
                CountryList.Add(c);
        }
    }
}
