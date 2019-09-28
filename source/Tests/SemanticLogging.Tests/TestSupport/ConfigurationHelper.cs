// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Configuration;
using Microsoft.Win32;

namespace EnterpriseLibrary.SemanticLogging.Tests.TestSupport
{
    internal class ConfigurationHelper
    {
        public static string GetSetting(string settingName)
        {
            string value = null;
            using (var subKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\EntLib") ?? Registry.CurrentUser)
            {
                var keyValue = subKey.GetValue(settingName);
                if (keyValue != null)
                {
                    value = keyValue.ToString();
                }
            }

            if (string.IsNullOrEmpty(value))
            {
                var configuration =
#if NETCOREAPP
                    ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = "EnterpriseLibrary.SemanticLogging.Tests.dll.config" }, ConfigurationUserLevel.None);
#else
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
#endif
                value = configuration.AppSettings.Settings[settingName]?.Value;
            }

            return value;
        }

        public static string GetConnectionString(string connectionStringName)
        {
            var configuration =
#if NETCOREAPP
                    ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = "EnterpriseLibrary.SemanticLogging.Tests.dll.config" }, ConfigurationUserLevel.None);
#else
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
#endif
            return configuration.ConnectionStrings.ConnectionStrings[connectionStringName]?.ConnectionString;
        }
    }
}
