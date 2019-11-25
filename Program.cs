using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ObjectRetriever
{
    //TODO: Bundle PSExec as a resource in the Executable 
    //TODO: Make it so that all you need to run this is a single executable. No dependcies B.
    //TODO: Add support for grabbing a list of objects. So Maybe a verb and then options for that. 

    static internal class Program
    {
        private static Logger Logger = Logger.LoggerInstance;
        private static string CurrentDirectory = Directory.GetCurrentDirectory();
        private static string PSExecFilePath = Path.Combine(CurrentDirectory, "PSExec.exe");

        static void Main(string[] args)
        {
            using(var parser = new Parser(config => config.HelpWriter = null))
            {
                var parserResult = parser.ParseArguments<Options>(args);

                parserResult.WithParsed(parsedArgs => Run(parsedArgs))
                            .WithNotParsed(errs => Options.DisplayHelp(parserResult, errs));
            }
        }

        private static void Run(Options options)
        {
            Logger.Log("Application start");
            string hostName = options.HostName;
            string targetObject = options.TargetObject;
            bool printToFile = options.PrintToFile;

            var retrievalData = new List<(string objectName, bool retrievalFlag)>
            {
                (Proprietary.EnabledServicesObject, options.GetEnabledServices),
                (Proprietary.EnabledLoggingObject, options.GetEnabledLogging)
            };

            // This is extracting only the boolean flags from the tuple retrievalData into a list.  
            List<bool> retrievalFlags = retrievalData.Select(items => items.retrievalFlag).ToList();

            if (!HostNameIsValid(hostName) || !RetrievalOptionsAreValid(targetObject, retrievalFlags))
            {
                FatalError($"Invalid arguments provided. Is {hostName} a valid host name? If so, check if you have access to the terminal." +
                                    $" If the host name is not the problem, run the program again with --help to see proper usage information.");
            }

            CreatePsExec(PSExecFilePath);

            if (!string.IsNullOrEmpty(targetObject))
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
            else
            {
                // If no target object is provided (-t /Object/To/Grab) then one of the common retrieval flags must be set 
                // for the program to be here. 
                RunCommonObjectRetrievals(hostName, printToFile, retrievalData);
            }

            Console.ReadKey();
            Logger.Log("Application end");
            DisposePsExec(PSExecFilePath);
            Logger.Dispose();
        }

        private static void CreatePsExec(string directoryForExecutable)
        {
            File.WriteAllBytes(directoryForExecutable, ObjectRetriever.Properties.Resources.PsExec);
        }

        private static void DisposePsExec(string psExecFilePath)
        {
            if (File.Exists(psExecFilePath)) File.Delete(psExecFilePath);
        }

        private static bool RetrievalOptionsAreValid(string targetObject, List<bool> retrievalFlags)
        {
            // If you don't provide a target object (-t) then at least one of the common object retrieval flags must be true
            // or else there's nothing to retrieve!
            return !(string.IsNullOrEmpty(targetObject) && Truth(retrievalFlags) == 0);
        }

        public static string CreateOutputFileName(string retrievedObject)
        {

            retrievedObject = retrievedObject.Substring(0, retrievedObject.IndexOf(Environment.NewLine)) // Get the first line of the object output.
                                             .Replace("Object", "") // Get rid of the "Object:" title in the output. 
                                             .Replace("Path", "") // Get rid of the "Path:" title in the output. 
                                             + "-" + DateTime.Now.ToString(new CultureInfo("en-US").DateTimeFormat); // Append the current time for a unique file name. 


            // Remove all invalid characters for a final file name. 
            return string.Join("", retrievedObject.Split(Path.GetInvalidFileNameChars()));
        }

        private static void PrintObject(bool printToFile, string hostName, string retrievedObject)
        {
            Console.Write(retrievedObject);

            if (printToFile)
            {
                string fileName = CreateOutputFileName(retrievedObject);
                string outputFile = CreateFile(CurrentDirectory, $"{hostName.ToUpper()}-{fileName}.txt");

                // The last 3 lines after running WebRiposteObjectFile List Tree [OBJECT] have command output
                // information (Command ran succesfully, etc.). We don't need that stuff in the final product.
                string[] lines = retrievedObject.Split('\n');
                Array.Resize(ref lines, lines.Length - 3);
                retrievedObject = string.Join("", lines).TrimEnd('\r', '\n');

                using (StreamWriter sw = new StreamWriter(outputFile, false))
                {
                    sw.Write(retrievedObject);
                }
            }

        }

        private static void ObjectNotFound(string hostName, string objectPath)
        {
            string message = $"Unable to retrieve the {objectPath} object from the {hostName} terminal. Either the object doesn't exist or a connection with the terminal" +
                $" was unable to be established.";

            Logger.Log(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Creates a file and puts it in the directory you specify. 
        /// </summary>
        /// <param name="rootDirectory">The folder where the file should be placed</param>
        /// <param name="fileName">Desired name of the file</param>
        /// <returns></returns>
        private static string CreateFile(string rootDirectory, string fileName)
        {
            Logger.Log("Creating the " + fileName + " file in the " + rootDirectory + " folder");

            string theFile = $@"{rootDirectory}\{fileName}";

            if (!File.Exists(theFile))
            {
                File.Create(theFile).Dispose();
                Logger.Log(fileName + " file created successfully");
            }
            else
            {
                Logger.Log(fileName + " already exists as a file");
            }

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

        private static string RetrieveObject(string hostName, string targetObject)
        {
            string retrievedObject = "";

            ProcessStartInfo info = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "PSExec", //TODO: Put the below command in a diferent place and don't commit it in repo. 
                Arguments = $@"/s -nobanner \\{hostName} {Proprietary.ListObjectTreeCommand} {targetObject}"
            };

            using (Process proc = new Process())
            {
                proc.StartInfo = info;
                proc.Start();
                retrievedObject = proc.StandardOutput.ReadToEnd();
            }

            return retrievedObject;
        }

        private static bool HostNameIsValid(string hostname)
        {
            return Directory.Exists($"\\\\{hostname}\\c$");
        }

        private static void FatalError(string message)
        {
            Logger.Log($"Fatal Error: {message}");
            Logger.Log("Application end");
            Logger.Dispose();
            DisposePsExec(PSExecFilePath);

            Console.WriteLine($"Fatal Error: {message}\n\nPress any key to exit.".Replace("\n", "\n\t"));
            Console.ReadKey();
            Environment.Exit(1);
        }

    }
}
