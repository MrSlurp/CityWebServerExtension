using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CWS_MrSlurpExtensions
{
    public static class DistrictExtension
    {
        #region district manager extension
        /// <summary>
        /// extension method that return a list of int based on districts ids
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetDistrictIds(this DistrictManager manager)
        {
            // This is the value used in Assembly-CSharp, so I presume that's the maximum number of districts allowed.
            const int count = DistrictManager.MAX_DISTRICT_COUNT;
            var districts = manager.m_districts.m_buffer;
            for (int i = 0; i < count; i++)
            {
                if (!districts[i].IsAlive()) { continue; }
                yield return i;
            }
        }
        #endregion

        public static Boolean IsValid(this District district)
        {
            return (district.m_flags != District.Flags.None);
        }

        public static Boolean IsAlive(this District district)
        {
            // Get the flags on the district, to ensure we don't access garbage memory if it doesn't have a flag for District.Flags.Created
            Boolean alive = ((district.m_flags & District.Flags.Created) == District.Flags.Created);
            return alive;
        }

        #region extension method to extract data with reflection
        /// <summary>
        /// retrieved DistrictInfo.ServiceData from a given serviceObjectFieldName (public field in district)
        /// and serviceObject sub fields names 
        /// </summary>
        /// <param name="district"></param>
        /// <param name="serviceTypeName">only used by web UI, name for css class</param>
        /// <param name="serviceObjectFieldName">serice field name in district object (m_commercialData)</param>
        /// <param name="serviceDataCurrent">service current value field name</param>
        /// <param name="serviceDataTotal">service total value field name</param>
        /// <returns>Servica data with values intiialized from district object datas</returns>
        private static DistrictInfo.ServiceData DistrictToServiceData<TServiceObj>(this District district, string serviceTypeName, string serviceObjectFieldName, string serviceDataCurrent)
        {
            //CityInfoRequestHandler.LogMessages(serviceTypeName, serviceObjectFieldName, serviceDataCurrent, serviceDataTotal != null ? serviceDataTotal : "null", typeof(TServiceObj).ToString());
            var serviceDataObject = new DistrictInfo.ServiceData();
            serviceDataObject.Name = serviceTypeName;

            try
            {
                FieldInfo serviceDataFieldInfo = ReflectionUtil.FindField(district, serviceObjectFieldName);
                if (serviceDataFieldInfo == null)
                {
                    CityInfoRequestHandler.LogMessages("Primary field not found, looking for", serviceObjectFieldName);
                    return serviceDataObject;
                }
                if (serviceDataFieldInfo.FieldType != typeof(TServiceObj))
                {
                    CityInfoRequestHandler.LogMessages("UnexpectedObjectType", serviceDataFieldInfo.FieldType.ToString(), "expecting", typeof(TServiceObj).ToString());
                    return serviceDataObject;
                }
                var districtServiceData = ReflectionUtil.GetFieldValue<TServiceObj>(serviceDataFieldInfo, district);
                var currentValue = ReflectionUtil.GetIntegerFieldValue(districtServiceData, serviceDataCurrent);
                if (!currentValue.HasValue)
                {
                    CityInfoRequestHandler.LogMessages("Second field not found, looking for", serviceDataCurrent, "in", serviceObjectFieldName);
                    return serviceDataObject;
                }
                else
                    serviceDataObject.Current = currentValue.Value;
            }
            catch (Exception ex)
            {
                CityInfoRequestHandler.LogMessages(ex.Message);
            }
            return serviceDataObject;
        }

        private static DistrictInfo.DoubleServiceData DistrictToDoubleServiceData<TServiceObj>(this District district, string serviceTypeName, string serviceObjectFieldName, string serviceDataCurrent, string serviceDataTotal)
        {
            //CityInfoRequestHandler.LogMessages(serviceTypeName, serviceObjectFieldName, serviceDataCurrent, serviceDataTotal != null ? serviceDataTotal : "null", typeof(TServiceObj).ToString());
            var serviceDataObject = new DistrictInfo.DoubleServiceData();
            serviceDataObject.Name = serviceTypeName;

            try
            {
                FieldInfo serviceDataFieldInfo = ReflectionUtil.FindField(district, serviceObjectFieldName);
                if (serviceDataFieldInfo == null)
                {
                    CityInfoRequestHandler.LogMessages("Primary field not found, looking for", serviceObjectFieldName);
                    return serviceDataObject;
                }

                if (serviceDataFieldInfo.FieldType != typeof(TServiceObj))
                {
                    CityInfoRequestHandler.LogMessages("UnexpectedObjectType", serviceDataFieldInfo.FieldType.ToString(), "expecting", typeof(TServiceObj).ToString());
                    return serviceDataObject;
                }
                var districtServiceData = ReflectionUtil.GetFieldValue<TServiceObj>(serviceDataFieldInfo, district);

                var currentValue = ReflectionUtil.GetIntegerFieldValue(districtServiceData, serviceDataCurrent);
                if (!currentValue.HasValue)
                {
                    CityInfoRequestHandler.LogMessages("Second field not found, looking for", serviceDataCurrent, "in", serviceObjectFieldName);
                    return serviceDataObject;
                }
                else
                    serviceDataObject.Current = currentValue.Value;

                if (serviceDataTotal != null)
                {
                    var totalValue = ReflectionUtil.GetIntegerFieldValue(districtServiceData, serviceDataTotal);
                    if (!totalValue.HasValue)
                    {
                        CityInfoRequestHandler.LogMessages("Second field not found, looking for", serviceDataTotal, "in", serviceObjectFieldName);
                        return serviceDataObject;
                    }
                    else
                        serviceDataObject.Available = totalValue.Value;
                }
            }
            catch (Exception ex)
            {
                CityInfoRequestHandler.LogMessages(ex.Message);
            }
            return serviceDataObject;
        }
        #endregion

        #region specialized functions to extract various district data types
        /// <summary>
        /// 
        /// </summary>
        /// <param name="district"></param>
        /// <param name="serviceTypeName"></param>
        /// <returns></returns>
        public static DistrictInfo.DoubleServiceData GetCountAndAliveServiceData(this District district, string serviceTypeName)
        {
            return district.DistrictToDoubleServiceData<DistrictPrivateData>(serviceTypeName, string.Format("m_{0}Data", serviceTypeName.ToLower()), "m_finalAliveCount", "m_finalHomeOrWorkCount");
        }
        public static DistrictInfo.ServiceData GetPrivateServiceData(this District district, string serviceTypeName, string privateTypeName)
        {
            return district.DistrictToServiceData<DistrictPrivateData>(serviceTypeName, string.Format("m_{0}Data", serviceTypeName.ToLower()), string.Format("m_final{0}", privateTypeName));
        }

        public static DistrictInfo.ServiceData GetConsumptionServiceData(this District district, string consumptionTypeName, string consumptionName)
        {
            return district.DistrictToServiceData<DistrictConsumptionData>(consumptionTypeName,string.Format("m_{0}Consumption", consumptionTypeName.ToLower()), string.Format("m_final{0}", consumptionName));
        }

        public static DistrictInfo.ServiceData GetImportExportServiceData(this District district, string ressourceDataName, string ressourceTypeName)
        {
            return district.DistrictToServiceData<DistrictResourceData>(ressourceTypeName,string.Format("m_{0}Data", ressourceDataName),string.Format("m_average{0}", ressourceTypeName));
        }

        public static DistrictInfo.DoubleServiceData GetProductionServiceData(this District district, string productionTypeName, string productionName)
        {
            return district.DistrictToDoubleServiceData<DistrictProductionData>(productionTypeName, "m_productionData",
                                                             string.Format("m_final{0}", productionName),
                                                             string.Format("m_final{0}", productionName));
        }
        public static DistrictInfo.ServiceData GetEducatedServiceData(this District district, string educatedTypeName, string categoryName, string name)
        {
            return district.DistrictToServiceData<DistrictEducationData>(name,string.Format("m_{0}Data", educatedTypeName),string.Format("m_final{0}", categoryName));
        }

        public static DistrictInfo.ServiceData GetAgeServiceData(this District district, string ageTypeName, string nameOverride = null)
        {
            return district.DistrictToServiceData<DistrictAgeData>(nameOverride == null ? ageTypeName : nameOverride,string.Format("m_{0}Data", ageTypeName),"m_finalCount");
        }
        #endregion

    }
}
