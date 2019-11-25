using System;
using System.IO;
using System.Linq;
using CommandLine;
using System.Text;
using System.Resources;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en")]
namespace ObjectRetriever
{
    static internal class Program
    {
        private static readonly string CurrentDirectory = Directory.GetCurrentDirectory();
        private static readonly string PSExecFilePath = Path.Combine(CurrentDirectory, "PSExec.exe");

        static void Main(string[] args)
        {
            using (var parser = new Parser(config => config.HelpWriter = null))
            {
                var parserResult = parser.ParseArguments<Options>(args);

                parserResult.WithParsed(parsedArgs => Run(parsedArgs))
                            .WithNotParsed(errs => Options.DisplayHelp(parserResult, errs));
            }
        }

        /// <summary>
        /// Main entry point. Runs the program with the parameter options. 
        /// </summary>
        private static void Run(Options options)
        {
            string hostName = options.HostName;
            string targetObject = options.TargetObject;
            bool printToFile = options.PrintToFile;

            var retrievalData = new List<(string objectName, bool retrievalFlag)>
            {
                (Strings.servicesObject, options.GetEnabledServices),
                (Strings.loggingObject, options.GetEnabledLogging)
            };

            // This is extracting only the boolean flags from the tuple retrievalData into a list.  
            List<bool> retrievalFlags = retrievalData.Select(items => items.retrievalFlag).ToList();

            if (!HostNameIsValid(hostName) || !RetrievalOptionsAreValid(targetObject, retrievalFlags))
            {
                FatalError($"Invalid arguments provided. Is {hostName} a valid host name? If so, check if you have access to the terminal." +
                                    $" If the host name is not the problem, run the program again with --help to see proper usage information.");
            }

            CreatePsExec(PSExecFilePath);

            if (string.IsNullOrEmpty(targetObject))
            {
                // If no target object is provided (-t /Object/To/Grab) then one of the common retrieval flags must be set 
                // for the program to be here. 
                RunCommonObjectRetrievals(hostName, printToFile, retrievalData);
            }
            else
            {
                string retrievedObject = RetrieveObject(hostName, targetObject);

                if (!string.IsNullOrEmpty(retrievedObject))
                {
                    PrintObject(printToFile, hostName, retrievedObject);
                }
                else
                {
                    ObjectNotFound(hostName, targetObject);
                }
            }

            DisposePsExec(PSExecFilePath);
        }

        /// <summary>
        /// Creates PsExec executable. 
        /// </summary>
        private static void CreatePsExec(string directoryForExecutable)
        {
            File.WriteAllBytes(directoryForExecutable, ObjectRetriever.Properties.Resources.PsExec);
        }

        /// <summary>
        /// Disposes PsExec executable. 
        /// </summary>
        private static void DisposePsExec(string psExecFilePath)
        {
            if (File.Exists(psExecFilePath)) File.Delete(psExecFilePath);
        }

        /// <summary>
        /// Validates that the arguments passed to the program make sense. 
        /// </summary>
        private static bool RetrievalOptionsAreValid(string targetObject, List<bool> retrievalFlags)
        {
            // If you don't provide a target object (-t) then at least one of the common object retrieval flags must be true
            // or else there's nothing to retrieve!
            return !(string.IsNullOrEmpty(targetObject) && Truth(retrievalFlags) == 0);
        }

        /// <summary>
        /// Creates a file name for the output using the objects name. 
        /// </summary>
        public static string CreateOutputFileName(string retrievedObject)
        {

            retrievedObject = retrievedObject.Substring(0, retrievedObject.IndexOf(Environment.NewLine, StringComparison.CurrentCulture)) // Get the first line of the object output.
                                             .Replace("Object", "") // Get rid of the "Object:" title in the output. 
                                             .Replace("Path", "") // Get rid of the "Path:" title in the output. 
                                             + $"-{DateTime.Now.ToString("dd-MM-yyyy", new CultureInfo("en-US"))}";  // Append the current time for a unique file name. 


            // Remove all invalid characters for a final file name. 
            return string.Join("", retrievedObject.Split(Path.GetInvalidFileNameChars()));
        }

        /// <summary>
        /// Prints a retrieved object, to the console, and to a file if requested. 
        /// </summary>
        private static void PrintObject(bool printToFile, string hostName, string retrievedObject)
        {
            Console.Write(retrievedObject);

            if (!printToFile) return;

            string fileName = CreateOutputFileName(retrievedObject);
            string outputFile = CreateFile(CurrentDirectory, hostName.ToUpper(new CultureInfo("en-US")) + "-" + fileName + ".txt");

            // The last 3 lines after running the retrieval command have output information
            // (Command ran succesfully, etc.). We don't need that stuff in the final product.
            string[] lines = retrievedObject.Split('\n');
            Array.Resize(ref lines, lines.Length - 3);
            retrievedObject = string.Join("", lines).TrimEnd('\r', '\n');

            using (StreamWriter sw = new StreamWriter(outputFile, false))
            {
                sw.Write(retrievedObject);
            }

        }

        /// <summary>
        /// Error handling for a situation where an object was not found on the target. 
        /// </summary>
        private static void ObjectNotFound(string hostName, string objectPath)
        {
            string message = $"Unable to retrieve the {objectPath} object from the {hostName} terminal. Either the object doesn't exist or a connection with the terminal" +
                $" was unable to be established.";

            Console.WriteLine(message);
        }

        /// <summary>
        /// Creates a file and puts it in the directory you specify. 
        /// </summary>
        private static string CreateFile(string rootDirectory, string fileName)
        {
            string theFile = $@"{rootDirectory}\{fileName}";

            if (!File.Exists(theFile)) File.Create(theFile).Dispose();

            return theFile;
        }

        /// <summary>
        /// Da Truth. You give this method a list of booleans. It returns an integer telling you how many of them are 
        /// true. 
        /// </summary>
        public static int Truth(List<bool> booleans)
        {
            return booleans.Count(indvidualBoolean => indvidualBoolean);
        }

        /// <summary>
        /// Runs the flagged retrieval options, which are preset. Common configuration object locations built into the program and 
        /// called by passing a flag. 
        /// </summary>
        private static void RunCommonObjectRetrievals(string hostName, bool printToFile, List<(string objectName, bool retrievalFlag)> retrievalData)
        {
            foreach (var item in retrievalData)
            {
                if (item.retrievalFlag)
                {
                    string retrievedObject = RetrieveObject(hostName, item.objectName);

                    if (string.IsNullOrEmpty(retrievedObject))
                    {
                        ObjectNotFound(hostName, item.objectName);
                    }
                    else
                    {
                        PrintObject(printToFile, hostName, retrievedObject);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a configuration object from the specified terminal. 
        /// </summary>
        private static string RetrieveObject(string hostName, string targetObject)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "PSExec",
                Arguments = $@"/s -nobanner \\{hostName} {Strings.listObjectTreeCommand} {targetObject}"
            };

            using (Process proc = new Process())
            {
                proc.StartInfo = info;
                proc.Start();
                string standardOutput = proc.StandardOutput.ReadToEnd();
                // There's some junk in the error messaging of the retrieval command so pulling that out. 
                string errorOutput = new Regex(@"[^a-zA-Z0-9 \\\/]").Replace(proc.StandardError.ReadToEnd(), "");
                return string.IsNullOrEmpty(standardOutput) ? errorOutput : standardOutput;
            }
        }

        /// <summary>
        /// Validates a hostname. 
        /// </summary>
        private static bool HostNameIsValid(string hostname)
        {
            return Directory.Exists($"\\\\{hostname}\\c$");
        }

        /// <summary>
        /// An error so bad we need to leave the program. 
        /// </summary>m>
        private static void FatalError(string message)
        {
            DisposePsExec(PSExecFilePath);
            Console.WriteLine($"Fatal Error: {message}\n\nPress any key to exit.".Replace("\n", "\n\t"));
            Console.ReadKey();
            Environment.Exit(1);
        }

    }
}
