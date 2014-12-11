using System;
using Kekiri;
using Rangic.Utilities.Geo;
using FluentAssertions;
using System.Collections.Generic;

namespace UnitTests.Geo
{
    [Scenario(Feature.Location, "A location is converted to the correct placename")]
    [Example("Salisbury, England, United Kingdom")]                     // city
    [Example("Hood River, Oregon")]                                     // strip United States of America
    [Example("Stonehenge Down, Amesbury CP, England, United Kingdom")]  // archeological site, village
    [Example("Voinovich Park, Cleveland, Ohio")]                        // park
    public class PlacenameTests : FluentTest
    {
        static private double latitude = 1;
        static private double longitude = 1;

        public PlacenameTests(string expectedName)
        {
            // Ensure each example has a unique lat/long, since it's cached otherwise.
            latitude += 1;

            Location.ReverseLookupProvider = new FakeLookupProvider();
            ((FakeLookupProvider)Location.ReverseLookupProvider).Register(latitude, longitude, expectedToRaw[expectedName]);

            Given(A_location, latitude, longitude);
            When(The_placename_is_retrieved);
            Then(The_placename_is_correct, expectedName);
        }

        private void A_location(double latitude, double longitude)
        {
            Context.Location = new Location(latitude, longitude);
        }

        private void The_placename_is_retrieved()
        {
            Context.PlaceName = Context.Location.PlaceName(Location.PlaceNameFilter.Standard);
        }

        private void The_placename_is_correct(string expectedname)
        {
            ((string)Context.PlaceName).Should().Be(expectedname);
        }

        private IDictionary<string,string> expectedToRaw = new Dictionary<string,string>
        {
            {   "Salisbury, England, United Kingdom",
                @"{""DisplayName"":""A303, Salisbury, Wiltshire, South West England, England, United Kingdom"",""address"":{""road"":""A303"",""city"":""Salisbury"",""county"":""Wiltshire"",""state_district"":""South West England"",""state"":""England"",""country"":""United Kingdom"",""country_code"":""gb""}}" 
            },
            {   "Hood River, Oregon",
                @"{""DisplayName"":""East 2nd Street, Hood River, Hood River County, Oregon, 97031, United States of America"",""address"":{""road"":""East 2nd Street"",""city"":""Hood River"",""county"":""Hood River County"",""state"":""Oregon"",""postcode"":""97031"",""country"":""United States of America"",""country_code"":""us""}}"
            },
            {   "Stonehenge Down, Amesbury CP, England, United Kingdom",
                @"{""DisplayName"":""Stonehenge Down, A303, Amesbury CP, Larkhill, Wiltshire, South West England, England, SP3 4DX, United Kingdom"",""address"":{""archaeological_site"":""Stonehenge Down"",""road"":""A303"",""suburb"":""Amesbury CP"",""village"":""Larkhill"",""county"":""Wiltshire"",""state_district"":""South West England"",""state"":""England"",""postcode"":""SP3 4DX"",""country"":""United Kingdom"",""country_code"":""gb""}}"
            },
            {   "Voinovich Park, Cleveland, Ohio",
                @"{""DisplayName"":""Voinovich Park, East 9th Street, East 4th Street, Warehouse District, Cleveland, Cuyahoga County, Ohio, 44114, United States of America"",""address"":{""park"":""Voinovich Park"",""road"":""East 9th Street"",""neighbourhood"":""East 4th Street"",""suburb"":""Warehouse District"",""city"":""Cleveland"",""county"":""Cuyahoga County"",""state"":""Ohio"",""postcode"":""44114"",""country"":""United States of America"",""country_code"":""us""}}"
            },
        };
    }
}
