using System;
using ExifLib;
using NLog;
using System.IO;
using Rangic.Utilities.Geo;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace Rangic.Utilities.Image
{
    public class ImageDetails
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Location Location { get; private set; }
        public DateTime CreatedTime { get; private set; }
        public string[] Keywords { get; private set; }
        public string FullPath { get; private set; }


        public ImageDetails(string fullPath)
        {
            FullPath = fullPath;
            Initialize();
        }

        public void ReloadKeywords()
        {
            Keywords = new XmpReader(FullPath).Keywords;
        }

        private void Initialize()
        {
            CreatedTime = new FileInfo(FullPath).CreationTime;

            try
            {
                using (var exif = new ExifLib.ExifReader(FullPath))
                {
                    DateTime dt;
                    exif.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out dt);
                    if (dt == DateTime.MinValue)
                        exif.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out dt);
                    if (dt == DateTime.MinValue)
                        exif.GetTagValue<DateTime>(ExifTags.DateTime, out dt);
                    if (dt != DateTime.MinValue)
                        CreatedTime = dt;

                    string latRef, longRef;
                    double[] latitude, longitude;
                    exif.GetTagValue<string>(ExifTags.GPSLatitudeRef, out latRef);
                    exif.GetTagValue<string>(ExifTags.GPSLongitudeRef, out longRef);
                    exif.GetTagValue<double[]>(ExifTags.GPSLatitude, out latitude);
                    exif.GetTagValue<double[]>(ExifTags.GPSLongitude, out longitude);

                    if (latRef != null && longRef != null)
                        Location = new Location(
                            ConvertLocation(latRef, latitude),
                            ConvertLocation(longRef, longitude));
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Exception reading EXIF data from '{0}': {1}", FullPath, ex);
            }

            ReloadKeywords();
        }

        static private double ConvertLocation(string geoRef, double[] val)
        {
            var v = val[0] + val[1] / 60 + val[2] / 3600;
            if (geoRef == "S" || geoRef == "W")
            {
                v *= -1;
            }
            return v;
        }
    }
}
