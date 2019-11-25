using System;
using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace ObjectRetriever
{
    class Options
    {
        /// <summary>
        /// The host name of the terminal to get the object from. 
        /// </summary>
        [Option('h', "hostname", Required = true, HelpText = "The host name of the target terminal.")]
        public string HostName { get; set; }

        /// <summary>
        /// The object to retrieve from the message store, i.e, the target object. 
        /// </summary>
        [Option('t', "targetobject", SetName = "DirectRetrieval", Required = false, HelpText = "Optional: The object to retreive. Exclusive: This option is not compatible with the common object retrieval flags.")]
        public string TargetObject { get; set; }
        
        /// <summary>
        /// Flag for grabbing the contents of /Configurations/RSS/EnabledServices. 
        /// </summary>
        [Option('e', "enabledservices", SetName="CommonRetrievals", Required = false, HelpText = "Optional: Retrieves the object denoting enabled services from the target terminal. Exclusive: This flag is not compatible with -t, but can be used with other common retrieval flags.")]
        public bool GetEnabledServices { get; set; }

        /// <summary>
        /// Flag for grabbing the contents of /Configurations/EGA/Logging. 
        /// </summary>
        [Option('l', "logging", SetName = "CommonRetrievals", Required = false, HelpText = "Optional: Retrieves the object denoting enabled logging on the target terminal. Exclusive: This flag is not compatible with -t, but can be used with other common retrieval flags.")]
        public bool GetEnabledLogging { get; set; }

        /// <summary>
        /// Flag for printing the retrieved objects contents to a file, instead of the console. 
        /// </summary>
        [Option('p', "printfile", Required = false, HelpText = "Optional: Prints the retrieved object(s) to a file, instead of the console.")]
        public bool PrintToFile { get; set; }

        /// <summary>
        /// Display's help text if invalid arguments are passed to the program. The CommandLineParser library provides default help text, so this method
        /// is just for setting custom attributes, like the width of the help text and the header, etc. 
        /// </summary>
        public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.Heading = "ObjectRetriever v1.0.0";
                h.Copyright = "Copyright @ 2019 SegunAkinyemi.com";
                h.MaximumDisplayWidth = 120;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);

            Console.WriteLine(helpText);
        }
    }
}

