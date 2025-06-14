using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Location_Spoof.Model
{
    public class Country
    {
        public string Name { get; set; }
        public string CountryCode { get; set; }  // e.g. "MY"
        public string FlagImage => $"https://flagsapi.com/{CountryCode}/flat/64.png";
    }

}
