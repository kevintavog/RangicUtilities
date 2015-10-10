using System.IO;
using Rangic.Utilities.Process;
using Rangic.Utilities.Os;
using System;
using NLog;

namespace Rangic.Utilities.Process
{
    public class FfmpegInvoker : ProcessInvoker
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();


        static public byte[] GenerateVideoFrame(string filename, int timeoutInSeconds)
        {
            // Invoke ffmpeg, load file as byte array, delete file, return array
            byte[] frameAsBytes;
            var tempFrameFile = Path.ChangeExtension(Path.GetTempFileName(), ".JPG");
            try
            {
                var invoker = new FfmpegInvoker { TimeoutSeconds = timeoutInSeconds };

                // This grabs the frame at the first second - if the video is shorter, this won't 
                // work so back off to the start of the file.
                invoker.Run("-i \"{0}\" -ss 00:00:01.0 -vframes 1 \"{1}\"", filename, tempFrameFile);
                if (!File.Exists(tempFrameFile))
                {
                    invoker.Run("-i \"{0}\" -ss 00:00:00.0 -vframes 1 \"{1}\"", filename, tempFrameFile);
                }

                frameAsBytes = File.ReadAllBytes(tempFrameFile);
            }
            finally
            {
                File.Delete(tempFrameFile);
            }

            return frameAsBytes;
        }


        protected override bool CheckProcessPath(string path)
        {
            if (!File.Exists(path))
            {
                logger.Info("File does not exist: {0}", path);
                return false;
            }

            Invoke("-version");
            return true;
        }

        override protected string ProcessName { get { return "ffmpeg"; } }

        override protected string ProcessPath
        { 
            get
            {
                switch (Platform.Id)
                {
                    case PlatformID.Unix:
                        return "/usr/bin/ffmpeg";

                    case PlatformID.MacOSX:
                        return "/usr/local/bin/ffmpeg";
                }

                throw new NotSupportedException("No ExifTool path for " + Platform.Id);
            }
        }
    }
}
