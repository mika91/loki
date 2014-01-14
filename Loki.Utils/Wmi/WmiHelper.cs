using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using log4net;

namespace Loki.Utils.Wmi
{
    // ---------------- WMI ----------------------
    // http://dotnet.developpez.com/articles/wmi1/
    // http://stackoverflow.com/questions/2155215/get-corresponding-physical-disk-drives-of-mountpoints-with-wmi-queries
    // http://microsoft.public.win32.programmer.wmi.narkive.com/CeV5Jaai/how-to-get-physical-disk-detials-from-the-partition-using-wmi
    // http://www.activexperts.com/activmonitor/windowsmanagement/scripts/storage/diskdrives/physical/#AddVMP.htm
    // http://social.msdn.microsoft.com/Forums/en-US/97a6de38-992c-4255-8c10-c286d68c26e0/create-disk-partition-using-wmi
    // -------------------------------------------
    public class WmiHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WmiHelper));

        #region Get WMI objects

        public static IEnumerable<ManagementObject> GetWmiObjects(string wmi_class)
        {
            var query = new ObjectQuery("select * from " + wmi_class);
            return GetWmiObjects(query);
        }

        public static IEnumerable<ManagementObject> GetWmiObjects(ObjectQuery wmi_query)
        {
            // Specify connection parameters
            var connectionOptions = new ConnectionOptions()
            {
                // Username = "not need for localhost", 
                // Password = "not need for localhost"  
            };

            // Set WMI operation scope
            var scope = new ManagementScope(@"\\localhost\root\cimv2", connectionOptions);

            // Get WMI objects collection
            var searcher = new ManagementObjectSearcher(scope, wmi_query);
            var collection = searcher.Get();

            return collection.Cast<ManagementObject>();
        }

        #endregion

        #region Map from WMI objects

        public static void Map(Object obj, ManagementObject mngObj)
        {
            try
            {
                // Get all properties with WmiProperty attribute
                var props = obj.GetType().GetProperties()
                    .SelectMany(pinfo => Attribute.GetCustomAttributes(pinfo).Select(prop => new { PropertyInfo = pinfo, WmiProperty = prop as WmiPropertyAttribute }))
                    .Where(x => x.WmiProperty != null)
                    .ToList();

                // Fill property with Wmi values
                var wmi = mngObj.Properties.Cast<PropertyData>().ToDictionary(x => x.Name, x => x.Value);
                foreach (var prop in props)
                {
                    Type type = prop.PropertyInfo.PropertyType;
                    string name = prop.WmiProperty.Name ?? prop.PropertyInfo.Name;

                    var valRaw = wmi.GetOr(name, prop.WmiProperty.Default);
                    var value = (valRaw == null)
                        ? null
                        : Convert.ChangeType(valRaw, type);
                    prop.PropertyInfo.SetValue(obj, value, null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(() => "Fail to map WMI object to an object of type <{0}>", obj.GetType(), ex.Message);
            }
        }

        public static T Map<T>(ManagementObject mngObj) where T : class
        {
            try
            {
                var type = typeof(T);
                var obj = Activator.CreateInstance(type);
                Map(obj, mngObj);
                return (T)obj;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static IEnumerable<T> Map<T>(String wmi_class) where T : class
        {
            var mngObj = WmiHelper.GetWmiObjects(wmi_class);
            return mngObj.Select(Map<T>);
        }

        public static IEnumerable<T> Map<T>(ObjectQuery wmi_query) where T : class
        {
            var mngObj = WmiHelper.GetWmiObjects(wmi_query);
            return mngObj.Select(Map<T>);
        }

        #endregion
    }
}
