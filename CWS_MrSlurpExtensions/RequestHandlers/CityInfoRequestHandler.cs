using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
            //LogMessages(string.Format("QueryString.Count= {0}, QueryString.AllKeys={1}", request.QueryString.Count, 
            //                                                                     string.Join(",",request.QueryString.AllKeys)));
            if (request.QueryString.HasKey("showList"))
            {
                //LogMessages("handling district list");
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
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            var districtIDs = GetDistrictsFromRequest(request);

            DistrictInfo globalDistrictInfo = null;
            List<DistrictInfo> districtInfoList = new List<DistrictInfo>();
            LogMessages(string.Format("{0} district requested", districtIDs.Count()));
            foreach (var districtID in districtIDs)
            {
                sw.Reset();
                sw.Start();
                var districtInfo = DistrictInfo.GetDistrictInfo(districtID);
                if (districtID == 0)
                {
                    globalDistrictInfo = districtInfo;
                }
                else
                {
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
            sw.Reset();
            sw.Start();
            var response = JsonResponse(cityInfo);
            sw.Stop();
            sw2.Stop();
            LogMessages(string.Format("json generation time {0} (total data generation = {1})", sw.Elapsed.TotalMilliseconds, sw2.Elapsed.TotalMilliseconds));
            return response;
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