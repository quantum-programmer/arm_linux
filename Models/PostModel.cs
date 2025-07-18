using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARM.Models
{
    public class PostModel
    {
        public int PostNumber { get; set; }
        public int id { get; set; }
        public string VehicleNumber { get; set; }
        public string DriverName { get; set; }
        public string FuelType { get; set; }
        public int Volume { get; set; }
        public int Dose { get; set; }
        public int Side { get; set; }
        public int Earth { get; set; }
        public int MachineType { get; set; }

        public string VolumeInfo => $"{Volume}л / {Dose}л";

        public IRelayCommand? SelectPostCommand { get; set; }


    }
}
