using System;
using System.Globalization;
using System.IO;

namespace ObjectRetriever
{
    public class Logger : IDisposable
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger LoggerInstance { get { return lazy.Value; } }

        private readonly StreamWriter _fileLogger;
        private bool disposed = false;

        private Logger()
        {
            string logFileName = Path.Combine(Directory.GetCurrentDirectory(), $"ObjectRetriever-" +
                $"{DateTime.Now.ToString(new CultureInfo("en-US").DateTimeFormat)}.log"); 
            _fileLogger = new StreamWriter(logFileName, false);
        }

        internal void Log(string message)
        {
            _fileLogger.WriteLine(DateTime.Now + "         " + message);
            _fileLogger.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if(!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if(disposing)
                {
                    // Dispose managed resources.
                    _fileLogger.Dispose();
                }

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}

