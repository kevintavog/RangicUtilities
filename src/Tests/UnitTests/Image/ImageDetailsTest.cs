using System;
using Kekiri;
using Rangic.Utilities.Image;
using FluentAssertions;

namespace UnitTests
{
    [Scenario(Feature.Image)]
    public class ImageDetailsTest : FluentTest
    {
        public ImageDetailsTest()
        {
            Given(An_image_file);
            When(The_image_details_are_retrieved);
            Then(The_details_are_correct);
        }

        private void An_image_file()
        {
            Context.ImageFile = "image/AlpineSingleTrack.jpg";
        }

        private void The_image_details_are_retrieved()
        {
            Context.Details = new ImageDetails(Context.ImageFile);
        }

        private void The_details_are_correct()
        {
            var details = (ImageDetails) Context.Details;
            details.Location.ToDms().Should().Be(@"47° 35' 09"" N, 122° 19' 43"" W");
            details.CreatedTime.Should().Be(new DateTime(2014, 06, 05, 18, 19, 27));
            details.Keywords.ShouldBeEquivalentTo(new string[] { "mountain biking", "snow" });
        }
    }
}
