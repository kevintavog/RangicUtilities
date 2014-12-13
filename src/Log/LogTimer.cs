using System;
using NLog;
using System.Diagnostics;
using System.Collections.Generic;

namespace Rangic.Utilities.Log
{
    public class LogTimer : IDisposable
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private Logger _logger;
        private LogLevel _logLevel;
        private string _message;
        private object[] _args;
        private SortedDictionary<string,string> _additional = new SortedDictionary<string,string>();


        public LogTimer(Logger logger, LogLevel logLevel) : this(logger, logLevel, null, null)
        {
        }

        public LogTimer(Logger logger, LogLevel logLevel, string message, params object[] args)
        {
            _logLevel = logLevel;
            _args = args;
            _message = message;
            _logger = logger;
            _stopwatch.Start();
        }

        public void Message(string message, params object[] args)
        {
            _message = message;
            _args = args;
        }

        public void Set(string fieldName, string val)
        {
            _additional[fieldName] = val;
        }

        public void Set(string fieldName, object val)
        {
            Set(fieldName, val != null ? val.ToString() : "<null>");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            string m = "";
            if (_message != null)
                m = String.Format(_message, _args);

            foreach (var kvp in _additional)
                m += String.Format("; {0}={1}", kvp.Key, kvp.Value);

            _logger.Log(_logLevel, String.Format(m + "; elapsedMsecs={0:F1}", _stopwatch.Elapsed.TotalMilliseconds));
        }
    }
}
