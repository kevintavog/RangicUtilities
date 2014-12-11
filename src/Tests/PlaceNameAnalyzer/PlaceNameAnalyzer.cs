using System;
using System.IO;
using NLog;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Rangic.Utilities.Geo;

namespace PlaceNameAnalyzer
{
    class MainClass
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static string Filename;
        private static Dictionary<string,Dictionary<string,string>> LocationToName;
        private static HashSet<string> UniquePlaceNames = new HashSet<string>();
        private static Dictionary<string,Dictionary<string,string>> AbbreviatedPlaceNames = new Dictionary<string, Dictionary<string, string>>();
        private static HashSet<string> UnhandledComponentName = new HashSet<string>();

        // For 'cityname'
        private static List<string> PrioritizedCityNameComponent = new List<string>
        {
            "city",
            "city_district",    // Perhaps shortest of this & city?
            "town",
            "hamlet",
            "locality",
            "neighbourhood",
            "suburb",
            "village",
            "county",
        };

        private static List<string> AcceptedComponents = new List<string>
        {
            "state",
            "country",
        };

        private static HashSet<string> ExcludedCountries = new HashSet<string>
        {
            "United States of America",
        };

        // For the point of interest / site / building
        private static List<string> PrioritizedSiteComponent = new List<string>
        {
            "playground",

            "aerodrome",
            "archaeological_site",
            "arts_centre",
            "attraction",
            "bakery",
            "bar",
            "basin",
            "building",
            "cafe",
            "car_wash",
            "chemist",
            "cinema",
            "cycleway",
            "department_store",
            "fast_food",
            "furniture",
            "garden",
            "garden_centre",
            "golf_course",
            "grave_yard",
            "hospital",
            "hotel",
            "house",
            "information",
            "library",
            "mall",
            "marina",
            "memorial",
            "military",
            "monument",
            "motel",
            "museum",
            "park",
            "parking",
            "path",
            "pedestrian",
            "pitch",
            "place_of_worship",
            "pub",
            "public_building",
            "restaurant",
            "roman_road",
            "school",
            "slipway",
            "sports_centre",
            "stadium",
            "supermarket",
            "theatre",
            "townhall",
            "viewpoint",
            "water",
            "zoo",

            "footway",
            "nature_reserve",
        };

        private static HashSet<string> IgnoredComponentNames = new HashSet<string>
        {
            "address26",
            "address29",
            "artwork",
            "atm",
            "bank",
            "bicycle",
            "bicycle_parking",
            "bus_station",
            "bus_stop",
            "clothes",
            "commercial",
            "community_centre",
            "construction",
            "courthouse",
            "country_code",
            "fire_station",
            "forest",
            "fuel",
            "house_number",
            "mobile_phone",
            "newsagent",
            "picnic_site",
            "postcode",
            "post_box",
            "post_office",
            "residential",
            "road",
            "scrub",
            "state_district",
            "telephone",
            "track",
            "tree",
            "yes",
        };


        public static void Main(string[] args)
        {
            if (!CheckArgs(args))
                return;

            ReadFile(Filename);
            foreach (var v in LocationToName.Values)
            {
                UniquePlaceNames.Add(v["DisplayName"]);

                foreach (var key in v.Keys)
                {
                    if (key != "DisplayName"
                        && !PrioritizedSiteComponent.Contains(key)
                        && !PrioritizedCityNameComponent.Contains(key)
                        && !AcceptedComponents.Contains(key)
                        && !IgnoredComponentNames.Contains(key))
                    {
                        UnhandledComponentName.Add(key);
                    }
                }

                var abbrPlaceName = BuildAbbreviatedPlaceName(v);
                AbbreviatedPlaceNames[abbrPlaceName] = v;
            }

            if (UnhandledComponentName.Count > 0)
            {
                logger.Info("Unhandled component names:");
                foreach (var key in UnhandledComponentName)
                    logger.Info("  {0}", key);
            }

//            foreach (var abbr in AbbreviatedPlaceNames.Keys)
//                logger.Info("{0}\n  {1}", abbr, AbbreviatedPlaceNames[abbr]["DisplayName"]);

            logger.Info("Read {0} unique locations, which resulted in {1} unique place names.", 
                LocationToName.Keys.Count, 
                UniquePlaceNames.Count);
            logger.Info("There are {0} unique abbreviated place names", AbbreviatedPlaceNames.Count);


            foreach (var key in LocationToName.Keys)
            {
                var location = Location.FromDms(key);
                var abbrPlaceName = BuildAbbreviatedPlaceName(LocationToName[key]);
                if (location.PlaceName(Location.PlaceNameFilter.Standard) != abbrPlaceName)
                    logger.Info("Mismatch: {0} --- {1}", abbrPlaceName, location.PlaceName(Location.PlaceNameFilter.Standard));
            }
        }

        private static string BuildAbbreviatedPlaceName(Dictionary<string,string> components)
        {
            var parts = new List<string>();

            var siteName = GetFirstMatch(PrioritizedSiteComponent, components);
            if (siteName != null)
                parts.Add(siteName);

            var cityName = GetFirstMatch(PrioritizedCityNameComponent, components);
            if (cityName != null)
                parts.Add(cityName);

            foreach (var key in AcceptedComponents)
            {
                if (components.ContainsKey(key))
                {
                    if (key == "country")
                    {
                        if (!ExcludedCountries.Contains(components[key]))
                            parts.Add(components[key]);
                    }
                    else
                    {
                        parts.Add(components[key]);
                    }
                }
            }

            if (!parts.Any() && components.ContainsKey("DisplayName"))
                return components["DisplayName"];

            return String.Join(", ", parts);
        }

        private static string GetFirstMatch(List<string> prioritizedList, Dictionary<string,string> components)
        {
            foreach (var key in prioritizedList)
                if (components.ContainsKey(key))
                    return components[key];

            return null;
        }

        private static void ReadFile(string filename)
        {
            LocationToName = JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(filename));
        }

        private static bool CheckArgs(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Provide a filename");
                return false;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File does not exist: {0}", args[0]);
                return false;
            }

            Filename = args[0];
            return true;
        }
    }
}
