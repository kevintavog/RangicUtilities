using System;
using System.IO;

namespace Rangic.Utilities.Os
{
    public class Platform
    {
        private static PlatformID? _platformId = null;
        public static PlatformID Id
        {
            get
            {
                if (!_platformId.HasValue)
                {
                    var hasMacFolders = Directory.Exists("/Applications") && 
                        Directory.Exists("/Users") &&
                        Directory.Exists(Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Library",
                            "Application Support"));

                    switch (Environment.OSVersion.Platform)
                    {
                        case PlatformID.MacOSX:
                        case PlatformID.Unix:
                            if (hasMacFolders)
                                _platformId = PlatformID.MacOSX;
                            else
                                _platformId = PlatformID.Unix;
                            break;

                        case PlatformID.Win32NT:
                            _platformId = PlatformID.Win32NT;
                            break;
                    }
                }

                return _platformId.Value;
            }
        }

        public static string UserDataFolder(string appName)
        {
            switch (Id)
            {
                case PlatformID.MacOSX:
                    // Probably won't work on non-English systems
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library",
                        "Application Support",
                        appName);

                case PlatformID.Unix:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "." + appName);

                case PlatformID.Win32NT:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        appName);

                default:
                    return null;
            }
        }

        private Platform() {}
    }
}
