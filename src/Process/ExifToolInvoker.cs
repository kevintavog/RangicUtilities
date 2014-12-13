using System;
using NLog;
using System.IO;
using Rangic.Utilities.Os;

namespace Rangic.Utilities.Process
{
    public class ExifToolInvoker : ProcessInvoker
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        override protected string ProcessName { get { return "ExifTool"; } }
        override protected string ProcessPath
        { 
            get
            {
                switch (Platform.Id)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        // Mac/Unix specific path to exiftool; it may not be installed...
                        return "/usr/bin/exiftool";

                    case PlatformID.Win32NT:
                        return Path.Combine(BaseAppFolder, "Dependencies", "exiftool.exe");
                }

                throw new NotSupportedException("No ExifTool path for " + Platform.Id);
            }
        }

        override protected bool CheckProcessPath(string path)
        {
            if (!File.Exists(path))
            {
                logger.Info("File does not exist: {0}", path);
                return false;
            }

            Invoke("-V");
            return true;
        }
    }
}
