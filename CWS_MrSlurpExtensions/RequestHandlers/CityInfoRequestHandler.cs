using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CityWebServer.Extensibility;
using ICities;
using ColossalFramework;
using UnityEngine;
using JetBrains.Annotations;
using System.Collections;
using System.Diagnostics;

namespace CWS_MrSlurpExtensions
{
    [UsedImplicitly]
    public class CityInfoRequestHandler : RequestHandlerBase
    {
        private static CityInfoRequestHandler _instance;

        private static bool EnableLog = false;
        public static CityInfoRequestHandler Instance 
        { 
            get
            {
                return _instance; 
            }
        }

        public static void LogMessages(params string[] messages)
        {
            if (_instance != null && EnableLog)
            {
                string result=string.Empty;
                foreach (var message in messages)
                    result += " / " +message;
                _instance.OnLogMessage(result);
            }
        }


        public CityInfoRequestHandler(IWebServer server)
            : base(server, new Guid("2ABCA7D3-E71C-40A5-BD32-50C08D5D4855"), "Slurp City Info", "MrSlurp", 100, "/SlurpUI/CityInfo")
        {
            _instance = this;
#if DEBUG
            EnableLog = true;
#endif
        }

        public override IResponseFormatter Handle(HttpListenerRequest request)
        {
            if (request.QueryString.HasKey("showList"))
            {
                return HandleDistrictList();
            }

            return HandleDistrict(request);
        }

        private IResponseFormatter HandleDistrictList()
        {
            var districtIDs = Singleton<DistrictManager>.instance.GetDistrictIds();

            return JsonResponse(districtIDs);
        }

        private IResponseFormatter HandleDistrict(HttpListenerRequest request)
        {
            Stopwatch sw = new Stopwatch();
            var districtIDs = GetDistrictsFromRequest(request);

            DistrictInfo globalDistrictInfo = null;
            List<DistrictInfo> districtInfoList = new List<DistrictInfo>();
            LogMessages(string.Format("{0} district requested", districtIDs.Count()));
            sw.Start();
            var buildings = GetBuildingBreakdownByDistrict();
            LogMessages(string.Format("building breakdown took {0}", sw.Elapsed.TotalMilliseconds));
            var vehicles = GetVehicleBreakdownByDistrict();
            LogMessages(string.Format("vehicle breakdown took {0}", sw.Elapsed.TotalMilliseconds));

            foreach (var districtID in districtIDs)
            {
                var districtInfo = DistrictInfo.GetDistrictInfo(districtID);
                if (districtID == 0)
                {
                    districtInfo.TotalBuildingCount = buildings.Sum(obj => obj.Value);
                    districtInfo.TotalVehicleCount = vehicles.Sum(obj => obj.Value);
                    globalDistrictInfo = districtInfo;
                }
                else
                {
                    districtInfo.TotalBuildingCount = buildings.Where(obj => obj.Key == districtID).Sum(obj => obj.Value);
                    districtInfo.TotalVehicleCount = vehicles.Where(obj => obj.Key == districtID).Sum(obj => obj.Value);
                    districtInfoList.Add(districtInfo);
                }
                LogMessages(string.Format("district {1} total generation time {0}", sw.Elapsed.TotalMilliseconds,districtID));
            }

            var simulationManager = Singleton<SimulationManager>.instance;

            var cityInfo = new CityInfo
            {
                Name = simulationManager.m_metaData.m_CityName,
                Time = simulationManager.m_currentGameTime.Date,
                GlobalDistrict = globalDistrictInfo,
                Districts = districtInfoList.ToArray(),
            };
            var response = JsonResponse(cityInfo);
            LogMessages(string.Format("json generation time {0} ",sw.Elapsed.TotalMilliseconds));
            return response;
        }

        private Dictionary<int, int> GetBuildingBreakdownByDistrict()
        {
            var districtManager = Singleton<DistrictManager>.instance;

            Dictionary<int, int> districtBuildings = new Dictionary<int, int>();
            BuildingManager instance = Singleton<BuildingManager>.instance;
            foreach (Building building in instance.m_buildings.m_buffer)
            {
                if (building.m_flags == Building.Flags.None) { continue; }
                var districtID = (int)districtManager.GetDistrict(building.m_position);
                if (districtBuildings.ContainsKey(districtID))
                {
                    districtBuildings[districtID]++;
                }
                else
                {
                    districtBuildings.Add(districtID, 1);
                }
            }
            return districtBuildings;
        }

        private Dictionary<int, int> GetVehicleBreakdownByDistrict()
        {
            var districtManager = Singleton<DistrictManager>.instance;

            Dictionary<int, int> districtVehicles = new Dictionary<int, int>();
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            foreach (Vehicle vehicle in vehicleManager.m_vehicles.m_buffer)
            {
                if (vehicle.m_flags != Vehicle.Flags.None)
                {
                    var districtID = (int)districtManager.GetDistrict(vehicle.GetLastFramePosition());
                    if (districtVehicles.ContainsKey(districtID))
                    {
                        districtVehicles[districtID]++;
                    }
                    else
                    {
                        districtVehicles.Add(districtID, 1);
                    }
                }
            }
            return districtVehicles;
        }

        private IEnumerable<int> GetDistrictsFromRequest(HttpListenerRequest request)
        {
            IEnumerable<int> districtIDs;
            if (request.QueryString.HasKey("districtID"))
            {
                List<int> districtIDList = new List<int>();
                var districtID = request.QueryString.GetInteger("districtID");
                if (districtID.HasValue)
                {
                    districtIDList.Add(districtID.Value);
                }
                districtIDs = districtIDList;
            }
            else
            {
                districtIDs = Singleton<DistrictManager>.instance.GetDistrictIds();
            }
            return districtIDs;
        }
    }
}