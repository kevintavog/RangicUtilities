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
            Longitude += 1;

            Location.ReverseLookupProvider = new FakeLookupProvider();
            Console.WriteLine("Register {0},{1} as {2}", Latitude, Longitude, expectedName);
            ((FakeLookupProvider)Location.ReverseLookupProvider).Register(Latitude, Longitude, ExpectedToRaw[expectedName]);
        }

        static public IDictionary<string,string> ExpectedToRaw = new Dictionary<string,string>
        {
            {   "Salisbury, England",
                @"{""DisplayName"":""A303, Salisbury, Wiltshire, South West England, England, United Kingdom"",""address"":{""road"":""A303"",""city"":""Salisbury"",""county"":""Wiltshire"",""state_district"":""South West England"",""state"":""England"",""country"":""United Kingdom"",""country_code"":""gb""}}" 
            },
            {   "Hood River, Oregon",
                @"{""DisplayName"":""East 2nd Street, Hood River, Hood River County, Oregon, 97031, United States of America"",""address"":{""road"":""East 2nd Street"",""city"":""Hood River"",""county"":""Hood River County"",""state"":""Oregon"",""postcode"":""97031"",""country"":""United States of America"",""country_code"":""us""}}"
            },
            {   "Stonehenge Down, Amesbury CP, England",
                @"{""DisplayName"":""Stonehenge Down, A303, Amesbury CP, Larkhill, Wiltshire, South West England, England, SP3 4DX, United Kingdom"",""address"":{""archaeological_site"":""Stonehenge Down"",""road"":""A303"",""suburb"":""Amesbury CP"",""village"":""Larkhill"",""county"":""Wiltshire"",""state_district"":""South West England"",""state"":""England"",""postcode"":""SP3 4DX"",""country"":""United Kingdom"",""country_code"":""gb""}}"
            },
            {   "Voinovich Park, Cleveland, Ohio",
                @"{""DisplayName"":""Voinovich Park, East 9th Street, East 4th Street, Warehouse District, Cleveland, Cuyahoga County, Ohio, 44114, United States of America"",""address"":{""park"":""Voinovich Park"",""road"":""East 9th Street"",""neighbourhood"":""East 4th Street"",""suburb"":""Warehouse District"",""city"":""Cleveland"",""county"":""Cuyahoga County"",""state"":""Ohio"",""postcode"":""44114"",""country"":""United States of America"",""country_code"":""us""}}"
            },
            {   "Reykjavik, Iceland",
                @"{""DisplayName"": ""5, Sóleyjargata, Austurbær, Reykjavik, Capital Region, 101, Iceland"",""address"":{""house_number"": ""5"",""road"": ""Sóleyjargata"",""suburb"": ""Austurbær"",""city"": ""Reykjavik"",""state_district"": ""Capital Region"",""postcode"": ""101"",""country"": ""Iceland"",""country_code"": ""is""}}"
            },
            {   "Annecy, France",
                @"{""DisplayName"": ""Lac d'Annecy, Chemin de la Vallière, Talloires, Annecy, Haute-Savoie, Rhône-Alpes, Metropolitan France, 74290, France"",""address"":{""water"": ""Lac d'Annecy"",""path"": ""Chemin de la Vallière"",""village"": ""Talloires"",""county"": ""Annecy"",""state"": ""Rhône-Alpes"",""country"": ""France"",""postcode"": ""74290"",""country_code"": ""fr""}}"
            },
            {   "Puerto Peñasco, Mexico",
                @"{""DisplayName"":""Plutarco Elias Calles, Puerto Peñasco, Sonora, 83550, Mexico"",""address"":{""road"":""Plutarco Elias Calles"",""city"":""Puerto Peñasco"",""state"":""Sonora"",""postcode"":""83550"",""country"":""Mexico"",""country_code"":""mx""}}"
            },
        };
    }

    [Scenario(Feature.Location, "A location is converted to the correct placename")]
    [Example("Salisbury, England")]                     // city
    [Example("Hood River, Oregon")]                     // strip United States of America
    [Example("Stonehenge Down, Amesbury CP, England")]  // archeological site, village
    [Example("Voinovich Park, Cleveland, Ohio")]        // park
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
    [Example("Salisbury, England", null)]
    [Example("Hood River, Oregon", null)]
    [Example("Stonehenge Down, Amesbury CP, England", "Stonehenge Down")]
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


    [Scenario(Feature.Location, "The detailed name of a location is properly returned")]
    [Example("Salisbury, England", "United Kingdom, England, Wiltshire, Salisbury")]
    [Example("Hood River, Oregon", "United States of America, Oregon, Hood River County, Hood River")]
    [Example("Stonehenge Down, Amesbury CP, England", "United Kingdom, England, Wiltshire, Larkhill, Amesbury CP, Stonehenge Down")]
    [Example("Voinovich Park, Cleveland, Ohio", "United States of America, Ohio, Cuyahoga County, Cleveland, Warehouse District, East 4th Street, Voinovich Park")]
    [Example("Reykjavik, Iceland", "Iceland, Reykjavik, Austurbær")]
    [Example("Annecy, France", "France, Rhône-Alpes, Annecy, Talloires, Chemin de la Vallière, Lac d'Annecy")]
    [Example("Puerto Peñasco, Mexico", "Mexico, Sonora, Puerto Peñasco")]
    public class DetailedNameTests : BaseLocationTests
    {
        public DetailedNameTests(string expectedName, string expectedDetails) : base(expectedName)
        {
            Given(A_location, Latitude, Longitude);
            When(The_placename_is_retrieved);
            Then(The_details_are_correct, expectedDetails);
        }

        private void A_location(double latitude, double longitude)
        {
            Context.Location = new Location(latitude, longitude);
        }

        private void The_placename_is_retrieved()
        {
            Context.DetailedName = Context.Location.PlaceName(Location.PlaceNameFilter.Detailed, true);
        }

        private void The_details_are_correct(string expectedDetails)
        {
            string detailedName = (string) Context.DetailedName;
            Console.WriteLine(detailedName);
            detailedName.Should().Be(expectedDetails);
        }
    }
}
