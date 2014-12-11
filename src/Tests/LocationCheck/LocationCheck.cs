using System;
using System.IO;
using NLog;
using Rangic.Utilities.Geo;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Rangic.Utilities.Image;

namespace LocationCheck
{
    class MainClass
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        static private IList<string> Folders = new List<string>();

        static private ConcurrentDictionary<string,string> PlaceNameComponents = new ConcurrentDictionary<string, string>();
        static private int FilesChecked = 0;
        static private int FilesWithLocations = 0;
        static private bool VisitingFiles = true;

        static private ConcurrentDictionary<string,int> UniqueLocations = new ConcurrentDictionary<string, int>();
        static private ConcurrentQueue<Location> QueuedLocations = new ConcurrentQueue<Location>();
        static private ConcurrentBag<Location> ResolvedLocations = new ConcurrentBag<Location>();


        public static void Main(string[] args)
        {
            if (!CheckArgs(args))
            {
                return;
            }

            var tasks = new List<Task>();
            for (int idx = 0; idx < 2; ++idx)
                tasks.Add(Task.Run( () => DequeueLocations() ));

            foreach (var folder in Folders)
            {
                VisitEachFile(folder, FileLocation);
            }

            logger.Info("Found {0} unique locations - waiting for lookups", UniqueLocations.Count);

            VisitingFiles = false;
            Task.WaitAll(tasks.ToArray());

            logger.Info("Unique place name components:");
            var components = PlaceNameComponents.Keys.ToList();
            components.Sort();
            foreach (var k in components)
            {
                logger.Info("  {0} = {1}", k, PlaceNameComponents[k]);
            }

            logger.Info("Checked {0} files, {1} with locations for {2} unique locations. There were {3} internal lookups and {4} external lookups were done",
                FilesChecked, FilesWithLocations, UniqueLocations.Count, Location.InternalRequests, Location.ExternalRequests);

            var locationToNames = new Dictionary<string,Dictionary<string,string>>();
            foreach (var location in ResolvedLocations)
            {
                var pn = new Dictionary<string,string>();
                locationToNames[location.ToDms()] = pn;

                foreach (var key in location.PlaceNameComponents.Keys)
                {
                    pn[(string) key] = (string) location.PlaceNameComponents[key];
                }
            }

            File.WriteAllText("locationToNames.json", JsonConvert.SerializeObject(locationToNames));
        }

        private static void FileLocation(string filename)
        {
            Interlocked.Increment(ref FilesChecked);
            var location = ImageDetailsReader.GetLocation(filename);
            if (location == null)
                return;

            Interlocked.Increment(ref FilesWithLocations);

            lock(UniqueLocations)
            {
                if (!UniqueLocations.ContainsKey(location.ToDms()))
                {
                    UniqueLocations[location.ToDms()] = 1;
                    QueuedLocations.Enqueue(location);
                }
            }
        }

        private static void GetPlaceName(Location location)
        {
            ResolvedLocations.Add(location);

//            logger.Info("Looking up {0}", location.ToDms());

            var fromDms = Location.FromDms(location.ToDms());
            logger.Info("original: {0} [{2} - {3}; other: {1}", location.ToDms(), fromDms.ToDms(), location.Latitude, location.Longitude);

            var buffer = new StringBuilder(1024);
            foreach (var k in location.PlaceNameComponents.Keys)
            {
                var s = (string) k;
                PlaceNameComponents[s] = location.PlaceNameComponents[k].ToString();
                if ("DisplayName" != s)
                    buffer.AppendFormat("{0}={1}; ", k, location.PlaceNameComponents[k]);
            }
            logger.Info("placename: {0}", buffer);
            logger.Debug("  Minimal   placename: {0}", location.PlaceName(Location.PlaceNameFilter.Minimal));
            logger.Debug("  Standard  placename: {0}", location.PlaceName(Location.PlaceNameFilter.Standard));
            logger.Debug("  All of it placename: {0}", location.PlaceName(Location.PlaceNameFilter.None));
        }

        static private void DequeueLocations()
        {
            try
            {
                while (VisitingFiles || QueuedLocations.Count > 0)
                {
                    Location location;
                    if (QueuedLocations.Count > 0 && QueuedLocations.TryDequeue(out location))
                    {
                        GetPlaceName(location);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error dequeuing locations: {0}", e.ToString());
            }
        }


        private static void VisitEachFile(string folder, Action<string> fileAction)
        {
            string[] allFiles;
            try
            {
                allFiles = Directory.GetFiles(folder);
            }
            catch (Exception e)
            {
                logger.Error("Unable to get files for {0}: {1}", folder, e);
                return;
            }

            foreach (var file in allFiles)
            {
                try
                {
                    fileAction(file);
                }
                catch (Exception e)
                {
                    logger.Error("Error handling {0}: {1}", file, e);
                }
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                VisitEachFile(dir, fileAction);
            }
        }

        private static bool CheckArgs(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Provide one or more folder names");
                return false;
            }

            var folderList = args;
            // Is it a file with directories in it?
            if (args.Length == 1 && File.Exists(args[0]))
            {
                folderList = File.ReadAllLines(args[0]);
            }

            var allDirsExist = true;
            foreach (var dir in folderList)
            {
                if (String.IsNullOrWhiteSpace(dir))
                    continue;

                if (!Directory.Exists(dir))
                {
                    Console.WriteLine("Folder does not exist: '{0}'", dir);
                    allDirsExist = false;
                }
                Folders.Add(dir);
            }

            return allDirsExist;
        }
    }
}
