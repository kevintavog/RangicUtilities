using System.IO;
using NLog;

namespace Rangic.Utilities.Process
{
    public class JheadInvoker : ProcessInvoker
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        override protected string ProcessName { get { return "jhead"; } }
        override protected string ProcessPath 
        { 
            get 
            { 
                return Path.Combine(BaseAppFolder, "Dependencies", "jhead");
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
