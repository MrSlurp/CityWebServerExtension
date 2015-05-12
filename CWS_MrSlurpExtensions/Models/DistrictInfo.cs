using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;

namespace CWS_MrSlurpExtensions
{
    public class DistrictInfo
    {
        public int DistrictID { get; set; }

        public String DistrictName { get; set; }

        public DistrictServiceData Population { get; set; }

        public int TotalBuildingCount { get; set; }

        public int TotalVehicleCount { get; set; }

        public int WeeklyTouristVisits { get; set; }

        public int AverageLandValue { get; set; }

        public Double Pollution { get; set; }

        public DistrictDoubleServiceData Jobs { get; set; }
        public DistrictDoubleServiceData Households { get; set; }
        public Dictionary<string, DistrictServiceData> Privates { get; set; }
        public DistrictServiceData Happiness { get; set; }
        public DistrictServiceData Crime { get; set; }
        public DistrictServiceData Health { get; set; }
        public Dictionary<string, DistrictServiceData> Productions { get; set; }
        public Dictionary<string, DistrictServiceData> Consumptions { get; set; }
        public Dictionary<string, DistrictServiceData> Educated { get; set; }
        public DistrictServiceData BirthDeath { get; set; }
        public Dictionary<string, DistrictServiceData> Students { get; set; }
        public Dictionary<string, DistrictServiceData> Graduations { get; set; }

        public Dictionary<string, DistrictServiceData> ImportExport { get; set; }

        public VehiclesInfo Vehicles { get; set; }

        public PolicyInfo[] Policies { get; set; }

        #region servica data structure classes
        // simple city service data with only name and current value field (most of services)
        public class ServiceData
        {
            public string Name { get; set; }
            public int Current { get; set; }
        }

        // add a second field for city serices that provide the availability value (households/jobs)
        public class DoubleServiceData : ServiceData
        {
            public int Available { get; set; }
        }

        public class DistrictServiceData 
        {
            public int TotalCurrent
            {
                get { return Categories.Sum(x=> x.Current); }
                set {}
            }
            public List<ServiceData> Categories { get; set; }
        }

        public class DistrictDoubleServiceData 
        {
            public int TotalCurrent
            {
                get { return Categories.Sum(x => x.Current); }
                set { }
            }
            // allow to use a global game value (total power/water production)
            // or automatic sum
            private int? totalAvailable;
            public int TotalAvailable
            {
                get
                {
                    if (totalAvailable.HasValue)
                        return totalAvailable.Value;
                    else
                        return Categories.Sum(x => (x.Available != 0) ? x.Available : x.Current);
                }
                set { totalAvailable = value; }
            }
            public List<DoubleServiceData> Categories { get; set; }
        }
        #endregion

        public class DistrictServiceDataCollection : Dictionary<string, DistrictServiceData>{}

        public static IEnumerable<int> GetDistricts()
        {
            var districtManager = Singleton<DistrictManager>.instance;
            return districtManager.GetDistrictIds();
        }

        public static DistrictInfo GetDistrictInfo(int districtID)
        {
            var districtManager = Singleton<DistrictManager>.instance;
            var district = GetDistrict(districtID);

            if (!district.IsValid()) { return null; }

            String districtName = String.Empty;

            if (districtID == 0)
            {
                // The district with ID 0 is always the global district.
                // It receives an auto-generated name by default, but the game always displays the city name instead.
                districtName = "City";
            }
            else
            {
                districtName = districtManager.GetDistrictName(districtID);
            }

            var pollution = Math.Round((district.m_groundData.m_finalPollution / (Double) byte.MaxValue), 2);

            #region data model familly to game service/zone type list & dictionary
            //
            // warning these strings match object field names in game models and should not be changed
            // see specified class to understand
            // in DistrictPrivateData
            List<string> ServiceZoneTypes = new List<string> { "Residential", "Commercial", "Industrial", "Office", "Player" };
            List<string> NoPlayerZoneTypes = new List<string> { "Residential", "Commercial", "Industrial", "Office" };
            List<string> JobServiceZoneTypes = new List<string> { "Commercial", "Industrial", "Office", "Player" };
            List<string> ImportExportTypes = new List<string> { "Agricultural", "Forestry", "Goods", "Oil", "Ore" };

            // in DistrictProductionData 
            Dictionary<string, string> ProductionsTypes = new Dictionary<string, string> { 
                {"Electricity", "ElectricityCapacity"}, 
                {"Water","WaterCapacity"}, 
                {"Sewage", "SewageCapacity"}, 
                {"GarbageA", "GarbageCapacity"} ,
                {"GarbageC", "GarbageAmount"} ,
                {"Incineration", "IncinerationCapacity"},
                {"Cremate", "CremateCapacity"}, 
                {"DeadA", "DeadAmount"}, 
                {"DeadC", "DeadCapacity"}, 
                {"Heal", "HealCapacity"},
                {"LowEducation", "Education1Capacity"}, 
                {"MediumEducation", "Education2Capacity"}, 
                {"HighEducation", "Education3Capacity"}, 
            };
            // in DistrictConsumptionData
            // NOTE : consumptions are stored in each kind of service, cool we can create consumption distributions pie
            Dictionary<string, string> ConsumptionsTypes = new Dictionary<string, string> { 
                {"Dead", "DeadCount"},
                {"Sick", "SickCount"},
                {"Electricity","ElectricityConsumption"},
                {"Water","WaterConsumption"},
                {"Sewage","SewageAccumulation"},
                {"Garbage","GarbageAccumulation"},
                {"Income", "IncomeAccumulation"},
                {"WaterPollution", "WaterPollution"},
                {"Building", "BuildingCount"},
                {"GarbagePiles", "GarbagePiles"},
                {"ImportAmount", "ImportAmount"},
            };

            Dictionary<string, string> PrivateDataTypes = new Dictionary<string, string> { 
                {"Abandoned","AbandonedCount"}, 
                {"BuildingArea","BuildingArea"}, 
                //{"BuildingCount","BuildingCount"}, same data as in DistrictConsumptionData
                {"Burned","BurnedCount"},
                {"EmptyCount","EmptyCount"},
                //{"Happiness","Happiness"}, do in diffferent way to be easyly able to display hapiness by type
                //{"CrimeRate","CrimeRate"},
                //{"Health","Health"},
                /* to re add if a day I understand usage
                {"Level","Level"},
                {"Level1","Level1"},
                {"Level2","Level2"},
                {"Level3","Level3"},
                {"Level4","Level4"},
                {"Level5","Level5"},*/
            };

            // in DistrictEducationData
            Dictionary<string, string> EducatedLevels = new Dictionary<string, string> { 
                {"No","educated0"}, 
                {"Low","educated1"}, 
                {"Medium","educated2"}, 
                {"High","educated3"}
            };
            Dictionary<string, string> EducatedDataTypes = new Dictionary<string, string> { 
                {"Total", "Count"}, 
                {"EligibleWorkers", "EligibleWorkers"},
                {"Homeless", "Homeless"},
                {"Unemployed","Unemployed"},
            };

            // in DistrictAgeData
            Dictionary<string, string> GraduationTypes = new Dictionary<string, string> 
            { 
                {"LowEducation","education1"}, 
                {"MediumEducation","education2"},
                {"HighEducation","education3"}, 
            };
            Dictionary<string, string> StudentTypes = new Dictionary<string, string> 
            { 
                {"LowStudent","student1"},
                {"MediumStudent","student2"}, 
                {"HighStudent","student3"}, 
            };
            Dictionary<string, string> BirthDeathTypes = new Dictionary<string, string> 
            { 
                {"Births","birth"}, 
                {"Deaths","death"} 
            };

            Dictionary<string, string> PopulationType = new Dictionary<string,string>{
                {"Childs","child"}, 
                {"Teens","teen"}, 
                {"Youngs","young"}, 
                {"Adults","adult"}, 
                {"Seniors","senior"}, 
            };
            #endregion
            var model = new DistrictInfo
            {
                DistrictID = districtID,
                DistrictName = districtName,
                #region service data generation
                Population = new DistrictServiceData{
                    Categories = new List<ServiceData>(PopulationType.Keys.Select(x => district.GetAgeServiceData(PopulationType[x], x))),
                },
                Happiness = new DistrictServiceData{
                    Categories = new List<ServiceData>(NoPlayerZoneTypes.Select(y => district.GetPrivateServiceData(y, "Happiness")) )
                },
                Crime = new DistrictServiceData{
                    Categories = new List<ServiceData>(NoPlayerZoneTypes.Select(y => district.GetPrivateServiceData(y, "CrimeRate")))
                },
                Health = new DistrictServiceData{
                    Categories = new List<ServiceData>(NoPlayerZoneTypes.Select(y => district.GetPrivateServiceData(y, "Health")))
                },
                // warning double lambda, but pretty magical effect
                Privates = new Dictionary<string, DistrictServiceData>(
                    PrivateDataTypes.Keys.ToDictionary(x => x,
                        x => new DistrictServiceData { Categories = new List<ServiceData>(ServiceZoneTypes.Select(y => district.GetPrivateServiceData(y, PrivateDataTypes[x]))) }
                    )
                ),
                Households = new DistrictDoubleServiceData{Categories = new List<DoubleServiceData>{district.GetCountAndAliveServiceData("residential"),},},
                Jobs = new DistrictDoubleServiceData{Categories = new List<DoubleServiceData>(JobServiceZoneTypes.Select(x => district.GetCountAndAliveServiceData(x)))},
                ImportExport = new Dictionary<string, DistrictServiceData>{
                    {"Import", new DistrictServiceData{Categories = new List<ServiceData>(ImportExportTypes.Select(x => district.GetImportExportServiceData("import", x)))}},
                    {"Export", new DistrictServiceData{Categories = new List<ServiceData>(ImportExportTypes.Select(x => district.GetImportExportServiceData("export", x)))}},
                },
                Productions = new Dictionary<string,DistrictServiceData>(
                    ProductionsTypes.Keys.ToDictionary(x => x, 
                        x => new DistrictServiceData { Categories = new List<ServiceData> { district.GetProductionServiceData(x, ProductionsTypes[x]) } }
                    )
                ),
                // warning double lambda, but pretty magical effect
                Consumptions = new Dictionary<string,DistrictServiceData>(
                    ConsumptionsTypes.Keys.ToDictionary(x => x, 
                        x => new DistrictServiceData { Categories = new List<ServiceData>(ServiceZoneTypes.Select(y => district.GetConsumptionServiceData(y, ConsumptionsTypes[x]))) }
                    )
                ),
                // warning double lambda, but pretty magical effect
                Educated = new Dictionary<string, DistrictServiceData>(
                    EducatedDataTypes.Keys.ToDictionary(x => x,
                        x => new DistrictServiceData { Categories = new List<ServiceData>(EducatedLevels.Keys.Select(y => district.GetEducatedServiceData(EducatedLevels[y], EducatedDataTypes[x], y))) }
                    )
                ),
                BirthDeath = new DistrictServiceData{
                    Categories = new List<ServiceData>(BirthDeathTypes.Keys.Select(x => district.GetAgeServiceData(BirthDeathTypes[x], x))),
                },
                Graduations = new Dictionary<string, DistrictServiceData>(
                    GraduationTypes.Keys.ToDictionary(x => x,
                        x => new DistrictServiceData { Categories = new List<ServiceData> { district.GetAgeServiceData(GraduationTypes[x]) } }
                    )
                ),
                Students= new Dictionary<string, DistrictServiceData>(
                    StudentTypes.Keys.ToDictionary(x => x,
                        x => new DistrictServiceData { Categories = new List<ServiceData> { district.GetAgeServiceData(StudentTypes[x]) } }
                    )
                ),

                #endregion
                AverageLandValue = district.GetLandValue(),
                Pollution = pollution,
                WeeklyTouristVisits = (int)district.m_tourist1Data.m_averageCount + (int)district.m_tourist2Data.m_averageCount + (int)district.m_tourist3Data.m_averageCount,
                Policies = GetPolicies().ToArray(),

            };
            if (districtID != 0)
            {
                CityInfoRequestHandler.LogMessages("Building vehicles for", districtID.ToString());
                model.Vehicles = new VehiclesInfo(districtID);
            }
            else
            {
                CityInfoRequestHandler.LogMessages("Building vehicles for city");
                model.Vehicles = new VehiclesInfo();
            }
            return model;
        }

        private static District GetDistrict(int? districtID = null)
        {
            if (districtID == null) { districtID = 0; }
            var districtManager = Singleton<DistrictManager>.instance;
            var district = districtManager.m_districts.m_buffer[districtID.Value];
            return district;
        }

        private static IEnumerable<PolicyInfo> GetPolicies()
        {
            var policies = EnumHelper.GetValues<DistrictPolicies.Policies>();
            var districtManager = Singleton<DistrictManager>.instance;

            foreach (var policy in policies)
            {
                String policyName = Enum.GetName(typeof(DistrictPolicies.Policies), policy);
                Boolean isEnabled = districtManager.IsCityPolicySet(DistrictPolicies.Policies.AlligatorBan);
                yield return new PolicyInfo
                {
                    Name = policyName,
                    Enabled = isEnabled
                };
            }
        }
    }
}