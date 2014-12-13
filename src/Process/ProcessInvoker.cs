using System;
using NLog;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Rangic.Utilities.Process
{
    abstract public class ProcessInvoker
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public int TimeoutSeconds { get; set; }

        public string OutputString { get { return outputBuffer.ToString(); } }
        public string ErrorString { get { return errorBuffer.ToString(); } }

        protected ProcessInvoker()
        {
            TimeoutSeconds = 5;
        }

        public void Run(string commandLine, params object[] args)
        {
            if (autoCheckProcessPath)
            {
                bool canRun = false;
                try
                {
                    canRun = CheckProcessPath(ProcessPath);
                }
                catch (Exception e)
                {
                    logger.Warn("Error invoking {0}: {1}", ProcessName, e);
                }

                if (!canRun)
                {
                    throw new InvalidOperationException("Unable to run " + ProcessName);
                }
            }

            Invoke(commandLine, args);
        }

        abstract protected string ProcessName { get; }
        abstract protected string ProcessPath { get; }
        abstract protected bool CheckProcessPath(string path);

        static private bool autoCheckProcessPath = true;
        private StringBuilder outputBuffer = new StringBuilder(1024);
        private StringBuilder errorBuffer = new StringBuilder(1024);



        protected void Invoke(string commandLine, params object[] args)
        {
            int timeout = Math.Max(1 * 1000, TimeoutSeconds * 1000);

            var psi = new ProcessStartInfo 
            {
                FileName = ProcessPath,
                Arguments = String.Format(commandLine, args),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = psi;
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            outputWaitHandle.Set();
                        else
                            outputBuffer.AppendLine(e.Data);
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            errorWaitHandle.Set();
                        else
                            errorBuffer.AppendLine(e.Data);
                    };

                    process.Start();
                    var startTime = DateTime.Now;
                    var endTime = DateTime.Now + TimeSpan.FromMilliseconds(timeout);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    while (!process.HasExited && DateTime.Now < endTime)
                    {
                        if (!process.WaitForExit(timeout) ||
                            !outputWaitHandle.WaitOne(timeout) ||
                            !errorWaitHandle.WaitOne(timeout))
                        {
                            logger.Error("Waiting for exit... {0} - {1}", process.HasExited, outputBuffer.Length);
                        }
                    }

                    if (process.HasExited)
                    {
                        var duration = (DateTime.Now - startTime).TotalMilliseconds;
                        if (duration > (timeout * 0.80))
                        {
                            logger.Warn(
                                "{2} took a long time processing \"{0}\": {1:N0} seconds", 
                                String.Format(commandLine, args),
                                duration / 1000,
                                ProcessName);
                        }

                        var exitCode = process.ExitCode;
                        if (exitCode != 0)
                        {
                            if (exitCode != 1)
                            {
                                logger.Warn("{0} exit code: {1}", ProcessName, exitCode);
                            }
                            throw new InvalidOperationException(ProcessName + " returned an error: " + exitCode + "; " + ErrorString);
                        }
                    }
                    else
                    {
                        try
                        {
                            var duration = (DateTime.Now - startTime).TotalMilliseconds;
                            logger.Warn("Killing {0} after {1} msecs ({2})", ProcessName, duration, String.Format(commandLine, args));
                            process.Kill();
                        }
                        catch (Exception e)
                        {
                            logger.Warn("Error killing {0}: {1}", ProcessName, e);
                        }

                        throw new InvalidOperationException(ProcessName + " took too long");
                    }
                }
            }
        }

        static public string BaseAppFolder
        {
            get
            {
                var uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
                return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            }
        }
    }
}
