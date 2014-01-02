using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SingleInstanceApplication
{
    static public class CommandLineArguments
    {
        /// <summary>
        /// Provides a function to emulate command-line arguments with a ClickOnce application
        /// or fall back to normal ones if not a ClickOnce.
        /// </summary>
        /// <returns></returns>
        public static string[] GetArguments()
        {
            // Check if this application was started using ClickOnce deployment
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (activationData != null && activationData.Length > 0)
                {
                    // Here I assume the arguments were given using ',' spaces do not really work... #@%!
                    // Luckily we only care about one argument.
                    string[] activationArgs = activationData[0].Split(new char[] { ',' });
                    string[] commandLineArgs = new string[activationArgs.Length + 1];

                    commandLineArgs[0] = Environment.GetCommandLineArgs()[0];
                    activationArgs.CopyTo(commandLineArgs, 1);

                    return commandLineArgs;
                }
            }
            // Otherwise use normal command-line args
            return Environment.GetCommandLineArgs();
        }
    }
}
