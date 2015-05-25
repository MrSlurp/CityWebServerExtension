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
                                                    {"activeVehicles",0},
                                                    {"waitCounter",0},
                                                    {"blockCounter",0},
                                                    {"delay",0},
                                                    {"highestWait",0},
                                                    {"highestBlock",0},
                                                    {"highestDelay",0},
                                                };
        public Dictionary<string, int> General
        {
            get { return general; }
        }
        Dictionary<string, int> cityMaintenanceVehicle = new Dictionary<string, int>{
                                                    {"garbageTrucks",0},
                                                    {"fireTrucks",0},
                                                    {"policeCars",0},
                                                    {"Ambulances",0},
                                                    {"hearses",0},
                                                    {"bodiesInTransit",0},
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
            General["activeVehicles"]++;
            General["waitCounter"] += vehicle.m_waitCounter;
            if (vehicle.m_waitCounter > General["highestWait"]) 
                General["highestWait"] = vehicle.m_waitCounter;

            General["blockCounter"] += vehicle.m_blockCounter;
            if (vehicle.m_blockCounter > General["highestBlock"]) 
                General["highestBlock"] = vehicle.m_blockCounter;

            General["delay"] += vehicle.m_waitCounter + vehicle.m_blockCounter;
            if ((vehicle.m_waitCounter + vehicle.m_blockCounter) > General["highestDelay"]) 
                General["highestDelay"] = vehicle.m_waitCounter + vehicle.m_blockCounter;
        }

        private bool UpdateCityMaintenance(Vehicle vehicle, bool buildingisvalid, uint transfertSize)
        {
            switch (vehicle.Info.m_class.m_service)
            {
                case ItemClass.Service.Garbage:
                    if (buildingisvalid) 
                        CityMaintenanceVehicle["garbageTrucks"]++;
                    return true;
                case ItemClass.Service.FireDepartment:
                    CityMaintenanceVehicle["fireTrucks"]++;
                    return true;
                case ItemClass.Service.PoliceDepartment:
                    CityMaintenanceVehicle["policeCars"]++;
                    return true;
                case ItemClass.Service.HealthCare:
                    if (vehicle.Info.m_vehicleAI.GetType() == typeof(AmbulanceAI))
                        CityMaintenanceVehicle["Ambulances"]++;
                    else if (vehicle.Info.m_vehicleAI.GetType() == typeof(HearseAI))
                        if (buildingisvalid)
                        {
                            CityMaintenanceVehicle["hearses"]++;
                            CityMaintenanceVehicle["bodiesInTransit"] += (int)transfertSize;
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
                                                        {"total",0},
                                                        {"srcDistricts", new Dictionary<string, int>()},
                                                        {"dstDistricts", new Dictionary<string, int>()}}
                                                        /*
                                                        {"intra",0},
                                                        {"imports",0},
                                                        {"exports",0},*/
                                                        /* following element are added only if required
                                                        {"importReason", new Dictionary<string, int>()},
                                                        {"exportReason", new Dictionary<string, int>()},
                                                        {"otherReason", new Dictionary<string, int>()}}
                                                         * */
                                        );
            }
            var svcData = CityServicesVehicles[serviceName];
            bool importing = (vehicle.m_flags & Vehicle.Flags.Importing) == Vehicle.Flags.Importing;
            bool exporting = (vehicle.m_flags & Vehicle.Flags.Exporting) == Vehicle.Flags.Exporting;
            if (importing) addOrIncrement(ref svcData, "Imports");
            if (exporting) addOrIncrement(ref svcData, "Exports");
            if ((!exporting && !importing)) addOrIncrement(ref svcData, "Intra");
            //CityServicesVehicles[serviceName]["imports"] = (int)CityServicesVehicles[serviceName]["imports"] + (importing ? 1 : 0);
            //CityServicesVehicles[serviceName]["exports"] = (int)CityServicesVehicles[serviceName]["exports"] + (exporting ? 1 : 0);
            //CityServicesVehicles[serviceName]["intra"] = (int)CityServicesVehicles[serviceName]["intra"] + ((!exporting && !importing) ? 1 : 0);
            CityServicesVehicles[serviceName]["Total"] = (int)CityServicesVehicles[serviceName]["Total"] + 1;

            var srcName = getBuildingDistrictName(vehicle.m_sourceBuilding);
            var dstName = getBuildingDistrictName(vehicle.m_targetBuilding);
            Dictionary<string, int> srcDistrict = (Dictionary<string, int>)CityServicesVehicles[serviceName]["SrcDistricts"];
            Dictionary<string, int> dstDistrict = (Dictionary<string, int>)CityServicesVehicles[serviceName]["DstDistricts"];
            addOrIncrement(ref srcDistrict, srcName);
            addOrIncrement(ref dstDistrict, dstName);

            var reason = (TransferManager.TransferReason)vehicle.m_transferType;
            string strReason = reason.ToString();
            if (reason == TransferManager.TransferReason.None)
            {
                VehicleAI vehicleAi = ((VehicleAI)vehicle.Info.GetAI());
                Vehicle dummy = vehicle;
                InstanceID target = vehicleAi.GetTargetID(vehicle.Info.m_instanceID.Vehicle, ref dummy);
                var _buildingManager = Singleton<BuildingManager>.instance;

                strReason = _buildingManager.m_buildings.m_buffer[target.Building].Info.GetService().ToString();

            }
            var svcDictionary = CityServicesVehicles[serviceName];
            string storeCategory = importing ? "ImportingReasons" : (exporting? "ExportingReasons": "IntraReasons" );
            var cat = (Dictionary<string, int>)createOrGetKey(ref svcDictionary, storeCategory);
            addOrIncrement(ref cat, storeCategory);
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

        private string getBuildingDistrictName(int buildingId)
        {
            BuildingManager bm = Singleton<BuildingManager>.instance;
            Building building = bm.m_buildings.m_buffer[buildingId];
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
                        /*
                        switch (myv.Info.m_class.m_service)
                        {
                            #region public transports
                            case ItemClass.Service.PublicTransport:
                                if (myv.Info.m_class.m_subService == ItemClass.SubService.PublicTransportBus)
                                {
                                    (CityServicesVehicles["PublicTransport"]["bus"]) = (int)CityServicesVehicles["PublicTransport"]["bus"] +1;
                                }
                                break;
                            #endregion
                                
                            case ItemClass.Service.:
                                UpdateService(myv, "Citizen", transfersize);
                                break;
                                
                            case ItemClass.Service.Commercial:
                                UpdateService(myv, "Commercial", transfersize);
                                break;
                                
                            case ItemClass.Service.Office:
                                UpdateService(myv, "Office", transfersize);
                                break;
                            case ItemClass.Service.Industrial:
                                UpdateService(myv, "Industrial", transfersize);
                                break;

                        }*/
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
