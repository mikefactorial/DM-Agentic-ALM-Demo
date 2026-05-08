using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformPackage
{
    public class Constants
    {
        public const string EnvironmentVariablePrefix = "PD_";
        public const string DataverseEnvironmentVariablePrefix = "ENVVAR";
        public const string ConnectionReferencePrefix = "CONNREF";
        public const string ManagedIdentityConfigPrefix = "MANAGEDIDENTITY";
        public const string EnvironmentSettingsPrefix = "ENVSETTING";
        
        // Solution XML
        public const string SolutionXmlFile = "solution.xml";
        public const string SolutionUniqueNameElement = "UniqueName";
        public const string SolutionVersionElement = "Version";

        // Package deployment filtering
        // Pass as a runtime setting to pac package deploy:
        //   --runtimePackageSettings "target_solutions={solutionPrefix}_{solutionName}"
        // See packageGroups[].solutions in environment-config.json for valid solution names.
        // Omit to deploy all solutions in the package.
        public const string TargetSolutionsSettingKey = "target_solutions";

        // Customizations XML
        public const string CustomizationsXmlFile = "customizations.xml";

    }
}
