using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using NLog;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Rangic.Utilities.Geo
{
    public class Location
    {
        public enum PlaceNameFilter
        {
            None,
            Minimal,
            Standard
        }

        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        static private ConcurrentDictionary<string,OrderedDictionary> cachedPlaceNames = new ConcurrentDictionary<string,OrderedDictionary>();


        static private int _externalRequests = 0;
        static private int _internalLookup = 0;
        static public int ExternalRequests { get { return _externalRequests; } }
        static public int InternalRequests { get { return _internalLookup; } }

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string City { get; private set; }
        public string County { get; private set; }
        public string Country { get; private set; }


        static public IReverseLookupProvider ReverseLookupProvider { get; set; }
        static public Location None = new Location(1000, 1000);

        static Location()
        {
            ReverseLookupProvider = new StandardLookupProvider();
        }

        static public bool IsNone(Location loc)
        {
            return loc == Location.None;
        }


        static public bool IsNullOrNone(Location loc)
        {
            return loc == null || loc == Location.None;
        }

        static public Location FromDms(string dms)
        {
            if (String.IsNullOrWhiteSpace(dms))
                return Location.None;

            // 47° 36' 21" N, 122° 21' 08" W [47.6057326354544 - -122.352271373751
            var tokens = dms.Split(',');
            if (tokens.Length != 2)
                return Location.None;

            double latitude, longitude;
            if (!FromDms(tokens[0], out latitude) ||
                !FromDms(tokens[1], out longitude))
            {
                return Location.None;
            }

            return new Location(latitude, longitude);
        }

        static private bool FromDms(string dms, out double latOrLong)
        {
            latOrLong = 0;
            string[] tokens = dms.Trim().Split("°'\" ".ToCharArray());
            if (tokens.Length != 7)
                return false;
            var degrees = Int32.Parse(tokens[0]);
            var minutes = Int32.Parse(tokens[2]);
            var seconds = Double.Parse(tokens[4]);

            latOrLong = degrees + (minutes / 60.0) + (seconds / 3600.0);

            if (tokens[6][0] == 'S' || tokens[6][0] == 'W')
                latOrLong *= -1;

            return true;
        }

        public Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return string.Format("[Location: Latitude={0}, Longitude={1}]", Latitude, Longitude);
        }

        public string ToDms(bool forHumans = true)
        {
            if (this == None)
            {
                return "";
            }

            char latNS = Latitude < 0 ? 'S' : 'N';
            char longEW = Longitude < 0 ? 'W' : 'E';
            return String.Format("{0} {1}, {2} {3}", ToDms(Latitude, forHumans), latNS, ToDms(Longitude, forHumans), longEW);
        }

        private string ToDms(double l, bool forHumans)
        {
            if (l < 0)
            {
                l *= -1f;
            }
            var degrees = Math.Truncate(l);
            var minutes = (l - degrees) * 60f;
            var seconds = (minutes - (int) minutes) * 60f;
            minutes = Math.Truncate(minutes);

            if (forHumans)
                return String.Format("{0:00}° {1:00}' {2:00}\"", degrees, minutes, seconds);
            else
                return String.Format("{0:00}° {1:00}' {2:0.00000000000000}\"", degrees, minutes, seconds);
        }

        public string PlaceName(PlaceNameFilter filter)
        {
            Interlocked.Increment(ref _internalLookup);

            var parts = new List<string>();

            var siteName = GetFirstMatch(PrioritizedSiteComponent, PlaceNameComponents);
            City = GetFirstMatch(PrioritizedCityNameComponent, PlaceNameComponents);
            if (filter == PlaceNameFilter.Standard)
            {
                if (siteName != null)
                    parts.Add(siteName);
                if (City != null)
                    parts.Add(City);
            }

            foreach (var key in PlaceNameComponents.Keys)
            {
                var keyString = (string) key;
                var valString = (string) PlaceNameComponents[key];

                if ("country_code" == keyString)
                {
                    Country = valString;
                    continue;
                }

                if ("county" == keyString)
                    County = valString;

                switch (filter)
                {
                    case PlaceNameFilter.Standard:
                        if (AcceptedComponents.Contains(keyString))
                        {
                            if (!IsExcluded(keyString, valString))
                                parts.Add(valString);
                        }
                        break;

                    case PlaceNameFilter.None:
                        if ("DisplayName" != keyString)
                            parts.Add(valString);
                        break;

                    case PlaceNameFilter.Minimal:
                        if ("country_code" != keyString)
                        {
                            if ("county" == keyString)
                            {
                                if (!PlaceNameComponents.Contains("city"))
                                {
                                    parts.Add(valString);
                                }
                            }
                            else
                            {
                                parts.Add(valString);
                            }
                        }
                        break;
                }
            }

            if (!parts.Any() && PlaceNameComponents.Contains("DisplayName"))
                parts.Add((string) PlaceNameComponents["DisplayName"]);

            return String.Join(", ", parts);
        }

        private bool IsExcluded(string key, string val)
        {
            IList<string> excludedValues;
            if (FieldExclusions.TryGetValue(key, out excludedValues))
            {
                return excludedValues.Contains(val);
            }

            return false;
        }

        private OrderedDictionary placeNameComponents;
        public OrderedDictionary PlaceNameComponents
        {
            get
            {
                if (placeNameComponents == null)
                {
                    placeNameComponents = CachedPlaceNameComponents();
                }

                return placeNameComponents;
            }
        }

        private OrderedDictionary CachedPlaceNameComponents()
        {
            var key = ToDms();
            OrderedDictionary pnc;
            if (cachedPlaceNames.TryGetValue(key, out pnc))
                return pnc;

            pnc = RetrievePlaceNameComponents();
            cachedPlaceNames.TryAdd(key, pnc);
            return pnc;
        }

        private OrderedDictionary RetrievePlaceNameComponents()
        {
            Interlocked.Increment(ref _externalRequests);
            var pnc = new OrderedDictionary();

            var data = ReverseLookupProvider.Lookup(Latitude, Longitude);
            if (data != null)
            {
                try
                {
                    dynamic response = JObject.Parse(data);
                    if (response["error"] != null)
                    {
                        logger.Warn("GeoLocation error: {0}", response.error);
                    }

                    if (response["display_name"] != null)
                    {
                        pnc.Add("DisplayName", (string) response.display_name);
                    }

                    if (response["address"] != null)
                    {
                        foreach (var kv in response["address"])
                        {
                            pnc.Add(kv.Name, (string) kv.Value);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Warn("Exception parsing '{0}': {1}", data, e);
                }
            }

            return pnc;
        }

        private static string GetFirstMatch(List<string> prioritizedList, OrderedDictionary components)
        {
            foreach (var key in prioritizedList)
                if (components.Contains(key))
                    return (string) components[key];

            return null;
        }

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

        static private IDictionary<string,IList<string>> FieldExclusions = new Dictionary<string, IList<string>>
        {
            { "country", new List<string> { "United States of America" } }
        };
    }
}
