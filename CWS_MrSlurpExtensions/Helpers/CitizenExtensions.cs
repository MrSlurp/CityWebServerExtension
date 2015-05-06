using System;
using ColossalFramework;

namespace CWS_MrSlurpExtensions
{
    public static class CitizenExtensions
    {
        public static String GetName(this Citizen citizen)
        {
            return Singleton<CitizenManager>.instance.GetCitizenName(citizen.m_instance);
        }
    }
}