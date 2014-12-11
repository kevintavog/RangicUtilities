using System;
using Rangic.Utilities.Geo;
using System.Collections.Generic;

namespace UnitTests.Geo
{
    public class FakeLookupProvider : IReverseLookupProvider
    {
        Dictionary<string,string> _placeNames = new Dictionary<string,string>();

        public string Lookup(double latitude, double longitude)
        {
            string placeName;
            if (_placeNames.TryGetValue(Key(latitude, longitude), out placeName))
                return placeName;
            return null;
        }

        public void Register(double latitude, double longitude, string placeName)
        {
            _placeNames[Key(latitude, longitude)] = placeName;
        }

        private string Key(double latitude, double longitude)
        {
            return String.Format("{0}x{1}", latitude, longitude);
        }
    }
}
