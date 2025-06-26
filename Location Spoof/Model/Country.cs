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
        public string CountryCode { get; set; }

        public ImageSource FlagImage { get; set; }


        public string GoogleMapLink => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(Name)}";
    }


}
