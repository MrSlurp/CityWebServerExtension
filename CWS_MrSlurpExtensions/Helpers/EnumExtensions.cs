using System;
using System.Collections.Generic;

namespace CWS_MrSlurpExtensions
{
    public static class EnumHelper
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}