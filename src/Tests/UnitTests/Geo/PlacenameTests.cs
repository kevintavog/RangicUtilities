using System;
using Kekiri;
using Rangic.Utilities.Geo;
using FluentAssertions;
using System.Collections.Generic;

namespace UnitTests.Geo
{
    public class BaseLocationTests : FluentTest
    {
        static public double Latitude = 1;
        static public double Longitude = 1;

        public BaseLocationTests(string expectedName)
        {
            // Ensure each example has a unique lat/long, since it's cached
            Latitude += 1;

            Location.ReverseLookupProvider = new FakeLookupProvider();
            ((FakeLookupProvider)Location.ReverseLookupProvider).Register(Latitude, Longitude, ExpectedToRaw[expectedName]);
        }

        static public IDictionary<string,string> ExpectedToRaw = new Dictionary<string,string>
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

    [Scenario(Feature.Location, "A location is converted to the correct placename")]
    [Example("Salisbury, England, United Kingdom")]                     // city
    [Example("Hood River, Oregon")]                                     // strip United States of America
    [Example("Stonehenge Down, Amesbury CP, England, United Kingdom")]  // archeological site, village
    [Example("Voinovich Park, Cleveland, Ohio")]                        // park
    public class PlacenameTests : BaseLocationTests
    {
        public PlacenameTests(string expectedName) : base(expectedName)
        {
            Given(A_location, Latitude, Longitude);
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

        private void The_placename_is_correct(string expectedName)
        {
            ((string)Context.PlaceName).Should().Be(expectedName);
        }
    }

    [Scenario(Feature.Location, "A location sitename is properly returned")]
    [Example("Salisbury, England, United Kingdom", null)]
    [Example("Hood River, Oregon", null)]
    [Example("Stonehenge Down, Amesbury CP, England, United Kingdom", "Stonehenge Down")]
    [Example("Voinovich Park, Cleveland, Ohio", "Voinovich Park")]
    public class SitenameTests : BaseLocationTests
    {
        public SitenameTests(string expectedName, string expectedSite) : base(expectedName)
        {
            Given(A_location, Latitude, Longitude);
            When(The_placename_is_retrieved);
            Then(The_sitename_is_correct, expectedSite);
        }

        private void A_location(double latitude, double longitude)
        {
            Context.Location = new Location(latitude, longitude);
        }

        private void The_placename_is_retrieved()
        {
            Context.PlaceName = Context.Location.PlaceName(Location.PlaceNameFilter.Standard);
            Context.SiteName = Context.Location.SiteName;
        }

        private void The_sitename_is_correct(string expectedSite)
        {
            var location = (Location) Context.Location;
            ((string) Context.SiteName).Should().Be(expectedSite);
            location.SiteName.Should().Be(expectedSite);
        }
    }
}
