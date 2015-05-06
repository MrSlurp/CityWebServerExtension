using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CWS_MrSlurpExtensions
{
    public class PublicTransportLine
    {
        public String Name { get; set; }

        public int VehicleCount { get; set; }

        public int StopCount { get; set; }

        public PopulationGroup[] Passengers { get; set; }

        public int TotalPassengers 
        {
            get
            {
                if (Passengers != null)
                {
                    var tmpList = new List<PopulationGroup>(Passengers);
                    return tmpList.Sum(obj => obj.Amount);
                }
                else
                    return 0;
            }
            set{}
        }

        public string LineColor { get; set; }

        public int CarTripSaved { get; set; }

        public int ResidentUsers { get; set; }
    }
}