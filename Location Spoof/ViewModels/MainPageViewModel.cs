using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location_Spoof.ViewModels
{
    using Location_Spoof.Model;
    using System.Diagnostics;
    using System.Windows.Input;

    public class MainPageViewModel
    {
        public ICommand SearchCommand { get; set; }

        public MainPageViewModel()
        {
            var countries = GetCountryList(); // 假设这是你自己定义的方法
            foreach (var country in countries)
            {
                Debug.WriteLine(country);
            }

        }

        private List<Country> GetCountryList()
        {
            return new List<Country>
            {
                new Country { Name = "Malaysia", CountryCode = "MY" },
                new Country { Name = "Singapore", CountryCode = "SG" },
                new Country { Name = "Thailand", CountryCode = "TH" },
                new Country { Name = "Vietnam", CountryCode = "VN" }
            };
        }



    }
}
