using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;


namespace CWS_MrSlurpExtensions
{
    public class VehiclesInfo
    {
        private DistrictManager districtManager = Singleton<DistrictManager>.instance;

        Dictionary<string, int> general = new Dictionary<string, int>{
                                                    {"ActiveVehicles",0},
                                                    {"WaitCounter",0},
                                                    {"BlockCounter",0},
                                                    {"Delay",0},
                                                    {"HighestWait",0},
                                                    {"HighestBlock",0},
                                                    {"HighestDelay",0},
                                                };
        public Dictionary<string, int> General
        {
            get { return general; }
        }
        Dictionary<string, int> cityMaintenanceVehicle = new Dictionary<string, int>{
                                                    {"GarbageTrucks",0},
                                                    {"FireTrucks",0},
                                                    {"PoliceCars",0},
                                                    {"Ambulances",0},
                                                    {"Hearses",0},
                                                    {"BodiesInTransit",0},
                                                };
        public Dictionary<string, int> CityMaintenanceVehicle
        {
            get { return cityMaintenanceVehicle; }
        }

        Dictionary<string, Dictionary<string, object>> cityServicesVehicles = new Dictionary<string, Dictionary<string, object>>();

        public Dictionary<string, Dictionary<string, object>> CityServicesVehicles
        {
            get { return cityServicesVehicles; }
        }

        #region methods

        public bool checkbuilding(Building b, ushort buildingIndex)
        {
            bool isEmptying = false;
            if ((b.m_flags & Building.Flags.Downgrading) != Building.Flags.None) isEmptying = true;

            return b.m_flags.IsFlagSet(Building.Flags.Created)
                && !b.m_flags.IsFlagSet(Building.Flags.Deleted)
                && !b.m_flags.IsFlagSet(Building.Flags.Untouchable)
                && !b.Info.m_buildingAI.IsFull(buildingIndex, ref b) // is full
                && !isEmptying; // is emptying
        }

        private IEnumerable<Vehicle> GetValidVehicles(VehicleInfo.VehicleType vType, int? districId)
        {
            Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            if (vehicles == null)
                yield break;

            foreach (var vehicle in vehicles)
            {
                if (!((vehicle.Info.m_vehicleType & vType) == vType))
                {
                    continue;
                }

                if (!((vehicle.m_flags & Vehicle.Flags.Created) == Vehicle.Flags.Created))
                {
                    continue;
                }

                if (!((vehicle.m_flags & Vehicle.Flags.Spawned) == Vehicle.Flags.Spawned))
                {
                    continue;
                }

                if (vehicle.m_leadingVehicle != 0)
                {
                    continue;
                }

                
                if (districId.HasValue)
                {
                    var vehicleDistrictID = (int)districtManager.GetDistrict(vehicle.GetLastFramePosition());
                    if (vehicleDistrictID != districId.Value)
                        continue;
                }

                yield return vehicle;
            }
        }

        private void UpdateGeneral(Vehicle vehicle)
        {
            General["ActiveVehicles"]++;
            General["WaitCounter"] += vehicle.m_waitCounter;
            if (vehicle.m_waitCounter > General["HighestWait"]) 
                General["HighestWait"] = vehicle.m_waitCounter;

            General["BlockCounter"] += vehicle.m_blockCounter;
            if (vehicle.m_blockCounter > General["HighestBlock"]) 
                General["HighestBlock"] = vehicle.m_blockCounter;

            General["Delay"] += vehicle.m_waitCounter + vehicle.m_blockCounter;
            if ((vehicle.m_waitCounter + vehicle.m_blockCounter) > General["HighestDelay"]) 
                General["HighestDelay"] = vehicle.m_waitCounter + vehicle.m_blockCounter;
        }

        private bool UpdateCityMaintenance(Vehicle vehicle, bool buildingisvalid, uint transfertSize)
        {
            switch (vehicle.Info.m_class.m_service)
            {
                case ItemClass.Service.Garbage:
                    if (buildingisvalid) 
                        CityMaintenanceVehicle["GarbageTrucks"]++;
                    return true;
                case ItemClass.Service.FireDepartment:
                    CityMaintenanceVehicle["FireTrucks"]++;
                    return true;
                case ItemClass.Service.PoliceDepartment:
                    CityMaintenanceVehicle["PoliceCars"]++;
                    return true;
                case ItemClass.Service.HealthCare:
                    if (vehicle.Info.m_vehicleAI.GetType() == typeof(AmbulanceAI))
                        CityMaintenanceVehicle["Ambulances"]++;
                    else if (vehicle.Info.m_vehicleAI.GetType() == typeof(HearseAI))
                        if (buildingisvalid)
                        {
                            CityMaintenanceVehicle["Hearses"]++;
                            CityMaintenanceVehicle["BodiesInTransit"] += (int)transfertSize;
                        }
                    return true;
            }
            return false;
        }

        private void UpdateService(Vehicle vehicle, string serviceName, uint transfertSize)
        {
            if (!CityServicesVehicles.Keys.Contains(serviceName))
            {
                CityServicesVehicles.Add(serviceName, new Dictionary<string, object>{
                                                        {"SrcDistricts", new Dictionary<string, int>()},
                                                        {"DstDistricts", new Dictionary<string, int>()}}
                                        );
            }
            var svcData = CityServicesVehicles[serviceName];
            addOrIncrement(ref svcData, "Total");
            bool importing = (vehicle.m_flags & Vehicle.Flags.Importing) == Vehicle.Flags.Importing;
            bool exporting = (vehicle.m_flags & Vehicle.Flags.Exporting) == Vehicle.Flags.Exporting;
            if (importing) addOrIncrement(ref svcData, "Imports");
            if (exporting) addOrIncrement(ref svcData, "Exports");
            if ((!exporting && !importing)) addOrIncrement(ref svcData, "Intra");

            Dictionary<string, int> srcDistrict = (Dictionary<string, int>)CityServicesVehicles[serviceName]["SrcDistricts"];
            Dictionary<string, int> dstDistrict = (Dictionary<string, int>)CityServicesVehicles[serviceName]["DstDistricts"];

            var reason = (TransferManager.TransferReason)vehicle.m_transferType;
            string strReason = reason.ToString();
            VehicleAI vehicleAi = ((VehicleAI)vehicle.Info.GetAI());
            InstanceID target = vehicleAi.GetTargetID(vehicle.Info.m_instanceID.Vehicle, ref vehicle);
            InstanceID owner = vehicleAi.GetOwnerID(vehicle.Info.m_instanceID.Vehicle, ref vehicle);
            var srcName = getBuildingDistrictName(owner);
            var dstName = getBuildingDistrictName(target);
            if (reason == TransferManager.TransferReason.None)
            {
                var _buildingManager = Singleton<BuildingManager>.instance;
                strReason = _buildingManager.m_buildings.m_buffer[target.Building].Info.GetService().ToString();
                srcName = getVehiclePositionDistrictName(vehicle);
            }

            addOrIncrement(ref srcDistrict, srcName);
            addOrIncrement(ref dstDistrict, dstName);

            var svcDictionary = CityServicesVehicles[serviceName];
            string storeCategory = importing ? "ImportingReasons" : (exporting? "ExportingReasons": "IntraReasons" );
            var cat = (Dictionary<string, int>)createOrGetKey(ref svcDictionary, storeCategory);
            addOrIncrement(ref cat, strReason);
        }

        private void addOrIncrement(ref Dictionary<string, int> src, string name)
        {
            if (!src.Keys.Contains(name))
                src.Add(name, 1);
            else
                src[name]++;
        }

        private void addOrIncrement(ref Dictionary<string, object> src, string name)
        {
            if (!src.Keys.Contains(name))
                src.Add(name, 1);
            else
                src[name] = (int)src[name]+ 1;
        }

        private Dictionary<string, int> createOrGetKey(ref Dictionary<string, object> src, string name)
        {
            if (!src.Keys.Contains(name))
                src.Add(name, new Dictionary<string, int>());
            return (Dictionary<string, int>)src[name];
        }

        private string getVehiclePositionDistrictName(Vehicle v)
        {
            var districtId = (int)districtManager.GetDistrict(v.GetLastFramePosition());
            return districtManager.GetDistrictName(districtId);
        }

        private string getCitizenHomeDistrictName(InstanceID citizenId)
        {
            BuildingManager bm = Singleton<BuildingManager>.instance;
            InstanceManager im = Singleton<InstanceManager>.instance;
            CitizenManager cm = Singleton<CitizenManager>.instance;
            var citizen = cm.m_citizens.m_buffer[citizenId.Citizen];
            Building building = bm.m_buildings.m_buffer[citizen.m_homeBuilding];
            bool buildingisplayer = building.m_flags.IsFlagSet(Building.Flags.Untouchable) ? false : true;
            var buildingDistrictId = (int)districtManager.GetDistrict(building.m_position);
            return buildingisplayer? (buildingDistrictId == 0 ? "City" : districtManager.GetDistrictName(buildingDistrictId)) : "Outside";
        }

        private string getBuildingDistrictName(InstanceID buildingId)
        {
            BuildingManager bm = Singleton<BuildingManager>.instance;
            InstanceManager im = Singleton<InstanceManager>.instance;
            InstanceID id = InstanceManager.GetLocation(buildingId);
            Building building = bm.m_buildings.m_buffer[id.Building];
            bool buildingisplayer = building.m_flags.IsFlagSet(Building.Flags.Untouchable) ? false : true;
            var buildingDistrictId = (int)districtManager.GetDistrict(building.m_position);
            return buildingisplayer? (buildingDistrictId == 0 ? "City" : districtManager.GetDistrictName(buildingDistrictId)) : "Outside";
        }

        public VehiclesInfo(int? districId = null)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                VehicleManager vm = Singleton<VehicleManager>.instance;
                BuildingManager bm = Singleton<BuildingManager>.instance;

                var Cars = GetValidVehicles(VehicleInfo.VehicleType.Car, districId);
                if (Cars == null)
                {
                    CityInfoRequestHandler.LogMessages("Car List Empty!");
                    return;
                }
                //CityInfoRequestHandler.LogMessages(string.Format("Car List with {0} items", Cars.Count()));
                foreach (var myv in Cars)
                {
                    UpdateGeneral(myv);

                    Building b = bm.m_buildings.m_buffer[myv.m_sourceBuilding];
                    bool buildingisvalid = checkbuilding(b, myv.m_sourceBuilding);
                    uint transfersize = myv.m_transferSize;
                    var reason = (TransferManager.TransferReason)myv.m_transferType;

                    if (!UpdateCityMaintenance(myv, buildingisvalid, transfersize))
                    {
                        bool importing = (myv.m_flags & Vehicle.Flags.Importing) == Vehicle.Flags.Importing;
                        bool exporting = (myv.m_flags & Vehicle.Flags.Exporting) == Vehicle.Flags.Exporting;
                        UpdateService(myv, myv.Info.m_class.m_service.ToString(), transfersize);
                    }
                }
                // convert reason & district src/dest to object list in order to be able to apply filter through angular
                string[] convertibleSubCollections = new string[]{"SrcDistricts", "DstDistricts", "ImportingReasons", "ExportingReasons", "IntraReasons"};
                foreach (var svcVehicles in CityServicesVehicles.Values)
                {
                    foreach(var elem in convertibleSubCollections)
                    {
                        if (svcVehicles.Keys.Contains(elem))
                        {
                            svcVehicles[elem] = ((Dictionary<string, int>)svcVehicles[elem]).Select(x=> new Dictionary<string, object>{{"Name",x.Key}, {"Count",x.Value}}).ToList();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                CityInfoRequestHandler.LogMessages("Error while building vehicles info", ex.Message, ex.StackTrace);
            }
            CityInfoRequestHandler.LogMessages(string.Format("District vehicles info generation time {0} ", sw.Elapsed.TotalMilliseconds));
        }
        #endregion
    }
}
