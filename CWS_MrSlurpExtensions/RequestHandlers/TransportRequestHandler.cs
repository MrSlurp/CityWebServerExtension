using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CityWebServer.Extensibility;
using ColossalFramework;
using UnityEngine;
using JetBrains.Annotations;

namespace CWS_MrSlurpExtensions
{
    [UsedImplicitly]
    public class TransportRequestHandler : RequestHandlerBase
    {
        public TransportRequestHandler(IWebServer server)
            : base(server, new Guid("0BD895C8-AB4F-468D-8258-6201ADBC10EC"), "Slurp Transport", "MrSlurp", 100, "/SlurpUI/Transport")
        {
        }

        public override IResponseFormatter Handle(HttpListenerRequest request)
        {
            var transportManager = Singleton<TransportManager>.instance;
            if (transportManager == null) 
                return JsonResponse(new Dictionary<string, List<PublicTransportLine>>());

            var lines = transportManager.m_lines.m_buffer.ToList();
            if (lines == null) 
                return JsonResponse(new Dictionary<string, List<PublicTransportLine>>());
            var busLines = lines.Where(x => x.Info.m_class.m_subService == ItemClass.SubService.PublicTransportBus);
            var metroLines = lines.Where(x => x.Info.m_class.m_subService == ItemClass.SubService.PublicTransportMetro);
            var trainLines = lines.Where(x => x.Info.m_class.m_subService == ItemClass.SubService.PublicTransportTrain);
            var shipLines = lines.Where(x => x.Info.m_class.m_subService == ItemClass.SubService.PublicTransportShip);
            var planeLines = lines.Where(x => x.Info.m_class.m_subService == ItemClass.SubService.PublicTransportPlane);

            Dictionary<string, List<PublicTransportLine>> allTransportLines = new Dictionary<string, List<PublicTransportLine>>()
            {
                {"BusLines", MakeLinesModels(busLines)},
                {"MetroLines", MakeLinesModels(metroLines)},
                {"TrainLines", MakeLinesModels(trainLines)},
                {"ShipLines", MakeLinesModels(shipLines)},
                {"PlaneLines", MakeLinesModels(planeLines)},
            };

            return JsonResponse(allTransportLines);
        }

        private List<PublicTransportLine> MakeLinesModels(IEnumerable<TransportLine> lines)
        {
            if (lines == null)
                return new List<PublicTransportLine>();
            List<PublicTransportLine> lineModels = new List<PublicTransportLine>(); 
            foreach (var line in lines)
            {
                if ((line.m_flags & TransportLine.Flags.Complete) == TransportLine.Flags.None) { continue; }
                var passengers = line.m_passengers;
                List<PopulationGroup> passengerGroups = new List<PopulationGroup>
                {
                    new PopulationGroup("Children", (int) passengers.m_childPassengers.m_averageCount),
                    new PopulationGroup("Teen", (int) passengers.m_teenPassengers.m_averageCount),
                    new PopulationGroup("YoungAdult", (int) passengers.m_youngPassengers.m_averageCount),
                    new PopulationGroup("Adult", (int) passengers.m_adultPassengers.m_averageCount),
                    new PopulationGroup("Senior", (int) passengers.m_seniorPassengers.m_averageCount),
                    new PopulationGroup("Tourist", (int) passengers.m_touristPassengers.m_averageCount)
                };

                var stops = line.CountStops(0); // The parameter is never used.
                var vehicles = line.CountVehicles(0); // The parameter is never used.

                var lineModel = new PublicTransportLine
                {
                    Name = String.Format("{0} {1}", line.Info.name, (int)line.m_lineNumber),
                    StopCount = stops,
                    VehicleCount = vehicles,
                    LineColor = ((line.m_flags & TransportLine.Flags.CustomColor) != TransportLine.Flags.None)? line.m_color.ToString(): "default",
                    Passengers = passengerGroups.ToArray(),
                    ResidentUsers = (int)passengers.m_residentPassengers.m_averageCount,
                    CarTripSaved = GetTripsSaved(line),
                };
                lineModels.Add(lineModel);
            }
            lineModels = lineModels.OrderBy(obj => obj.Name).ToList();
            //IntegratedWebServer.LogMessage(String.Format("Transport lines, built {0} lines models", lineModels.Count));
            return lineModels;
        }

        private List<Vehicle> GetLineVehicles(TransportLine line)
        {
            List<Vehicle> lineVehicles = new List<Vehicle>();
            if (line.m_vehicles != 0)
            {
                VehicleManager instance = Singleton<VehicleManager>.instance;
                ushort currentVehicle = line.m_vehicles;
                int index = 0;
                while (currentVehicle != 0)
                {
                    lineVehicles.Add(instance.m_vehicles.m_buffer[(int)currentVehicle]);
                    ushort nextLineVehicle = instance.m_vehicles.m_buffer[(int)currentVehicle].m_nextLineVehicle;
                    currentVehicle = nextLineVehicle;
                    if (++index > VehicleManager.MAX_VEHICLE_COUNT)
                    {
                        break;
                    }
                }
            }
            return lineVehicles;
        }

        // code from https://github.com/justacid/Skylines-ExtendedPublicTransport/blob/master/ExtendedPublicTransportUI/TransportUtil.cs
        private int GetTripsSaved(TransportLine line)
        {
            // formula lifted straight from decompiled source of PublicTransportWorldInfoPanel
            // just slightly deobfuscated
            var residents = line.m_passengers.m_residentPassengers.m_averageCount;
            var tourists = line.m_passengers.m_touristPassengers.m_averageCount;
            var teens = line.m_passengers.m_teenPassengers.m_averageCount;
            var young = line.m_passengers.m_youngPassengers.m_averageCount;
            var adult = line.m_passengers.m_adultPassengers.m_averageCount;
            var senior = line.m_passengers.m_seniorPassengers.m_averageCount;
            var carOwners = line.m_passengers.m_carOwningPassengers.m_averageCount;

            uint result = 0;
            if (residents + tourists != 0)
            {
                result = teens*5 +
                         young*((15*residents + 20*tourists + ((residents + tourists)/2))/(residents + tourists)) +
                         adult*((20*residents + 20*tourists + ((residents + tourists)/2))/(residents + tourists)) +
                         senior*((10*residents + 20*tourists + ((residents + tourists)/2))/(residents + tourists));
            }

            int tripsSaved = 0;
            if (result != 0)
            {
                var tmp = (carOwners*10000L + result/2) / result;
                tripsSaved = Mathf.Clamp((int)tmp, 0, 100);
            }

            return tripsSaved;
        }
    }
}