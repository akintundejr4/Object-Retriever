using System;
using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace ObjectRetriever
{
    class Options
    {
        private static UnParserSettings settings = new UnParserSettings();

        static Options()
        {
            settings.PreferShortName = true;
            settings.GroupSwitches = true;
            settings.UseEqualToken = true;
        }

        /// <summary>
        /// The host name of the terminal to get the object from. 
        /// </summary>
        [Option('h', "hostname", Required = true, HelpText = "The host name of the target terminal.")]
        public string HostName { get; set; }

        /// <summary>
        /// The object to retrieve from the message store, i.e, the target object. 
        /// </summary>
        [Option('o', "object", SetName = "DirectRetrieval", Required = false, HelpText = "Optional. The object to retreive.")]
        public string TargetObject { get; set; }

        /// <summary>
        /// Flag for grabbing the contents of /Configurations/RSS/EnabledServices. 
        /// </summary>
        [Option('e', "enabledservices", SetName = "CommonRetrievals", Required = false, HelpText = "Optional. Retrieves the object denoting enabled services from the target terminal.")]
        public bool GetEnabledServices { get; set; }

        /// <summary>
        /// Flag for grabbing the contents of /Configurations/EGA/Logging. 
        /// </summary>
        [Option('l', "logging", SetName = "CommonRetrievals", Required = false, HelpText = "Optional. Retrieves the object denoting enabled logging on the target terminal.")]
        public bool GetEnabledLogging { get; set; }

        /// <summary>
        /// Flag for printing the retrieved objects contents to a file, instead of the console. 
        /// </summary>
        [Option('p', "printfile", Required = false, HelpText = "Optional. Prints the retrieved object(s) to a file, instead of the console.")]
        public bool PrintToFile { get; set; }

        /// <summary>
        /// Displays help text if invalid arguments are passed to the program. The CommandLineParser library provides default help text, so this method
        /// is just for setting custom attributes, like the width of the help text and the header, etc. 
        /// </summary>
        public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.Heading = "ObjectRetriever v1.0.0";
                h.Copyright = $"Copyright @ {DateTime.Now.Year} SegunAkinyemi.com";
                h.MaximumDisplayWidth = 120;
                h.AdditionalNewLineAfterOption = true;
                h.AddNewLineBetweenHelpSections = true;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            Console.WriteLine(helpText);
        }

        [Usage(ApplicationAlias = "GrabObject.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Standard Retrieval", settings, new Options { HostName = "SALSMOFGFC7", TargetObject = "/Configurations/RSS/Data" });
                yield return new Example("\nStandard Retrieval with Print To File", settings, new Options { HostName = "SALSMOFGFC7", TargetObject = "/Configurations/RSS/Data", PrintToFile = true });
                yield return new Example("\nRetrieving Enabled Services", settings, new Options { HostName = "SALSMOFGFC7", GetEnabledServices = true });
                yield return new Example("\nRetrieving Enabled Logging", settings, new Options { HostName = "SALSMOFGFC7", GetEnabledLogging = true });
            }
        }
    }
}

