using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    //
    // Class to write logging information to a file.
    //
    internal class Logger : IDisposable
    {
        private readonly System.Threading.Lock _lock = new();

        private readonly StreamWriter _writer;
        private bool _disposedValue;

        //private string _logFilePath = $"{System.Environment.SpecialFolder.LocalApplicationData}";

        public static Logger Instance { get; set; } = new Logger();

        //
        // Constructor.
        //
        private Logger()
        {
            string directory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

            _writer = new System.IO.StreamWriter(Path.Join(directory, "raytrace.txt"), append: false);
        }

        private void Write(string message)
        {
            lock (_lock)
            {
                //System.IO.File.AppendAllText("debug.log", $"{DateTime.Now}: {message}{Environment.NewLine}");
                _writer.Write(message);
                _writer.Flush();
            }
            System.Diagnostics.Debug.Write(message);
        }

        // Diagnostic information.
        public void Trace(string message)
        {
            Write($"TRACE {DateTime.Now} : {message}{Environment.NewLine}");
        }

        // Debugging information.
        public void Debug(string message)
        {
            Write($"DEBUG {DateTime.Now} : {message}{Environment.NewLine}");
        }

        // Info about normal operation.
        public void Info(string message)
        {
            Write($"INFO {DateTime.Now} : {message}{Environment.NewLine}");
        }

        // Warnings about possible issues.
        public void Warn(string message)
        {
            Write($"WARN {DateTime.Now} : {message}{Environment.NewLine}");
        }

        // Errors detected during program operation.
        public void Error(string message)
        {
            Write($"ERROR {DateTime.Now} : {message}{Environment.NewLine}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                }

                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Logger()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
