using System;
using System.IO;
using System.Xml.Linq;
using NLog;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml;
using Rangic.Utilities.Geo;
using System.Linq;
using System.Globalization;

namespace Rangic.Utilities.Image
{
    public class XmpReader
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string fullPath;

        public string[] Keywords { get; private set; }
        public Location Location { get; private set; }
        public DateTime? CreatedTime { get; private set; }


        public XmpReader(string fullPath)
        {
            this.fullPath = fullPath;

            LoadXml();
        }


        private void LoadXml()
        {
            var list = new List<string>();
            try
            {
                string xmpXml = null;
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var bufferedStream = new BufferedStream(fileStream))
                using (var reader = new BinaryReader(bufferedStream))
                {
                    // Is it a JPEG?
                    if (ReadUshort(reader) == 0xFFD8)
                    {
                        xmpXml = GetJpegXmp(bufferedStream, reader);
                    }
                    else
                    {
                        // Perhaps a video?
                        using (var videoParser = new VideoAtomParser(fullPath))
                        {
                            xmpXml = videoParser.Xml;
                        }
                    }
                }

                if (xmpXml != null)
                {
                    var xmpDoc = XDocument.Parse(xmpXml);

                    var namespaceManager = new XmlNamespaceManager(new NameTable());
                    namespaceManager.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                    namespaceManager.AddNamespace("exif", "http://ns.adobe.com/exif/1.0/");
                    namespaceManager.AddNamespace("x", "adobe:ns:meta/");
                    namespaceManager.AddNamespace("xmp", "http://ns.adobe.com/xap/1.0/");
                    namespaceManager.AddNamespace("tiff", "http://ns.adobe.com/tiff/1.0/");
                    namespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");


                    // Created time
                    var timeElement = xmpDoc.Root.XPathSelectElement("//xmp:CreateDate", namespaceManager);
                    if (timeElement != null)
                    {
                        // Unlike EXIF time, the XMP times are UTC. This means that viewing the same images/videos in
                        // different time zones is going to come up with different times - that's only a problem for
                        // sorting / comparing by time if other images/videos have dates from EXIF only.
                        DateTime dt;
                        if (DateTime.TryParseExact(timeElement.Value, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                            CreatedTime = dt.ToLocalTime();
                        }
                    }


                    // Keywords
                    var subjectBag = xmpDoc.Root.XPathSelectElement(".//dc:subject/rdf:Bag", namespaceManager);
                    if (subjectBag != null)
                    {
                        foreach (var e in subjectBag.Elements())
                        {
                            list.Add(e.Value);
                        }
                    }


                    // Location
                    string gpsLatitude = null;
                    string gpsLongitude = null;
                    var exifElements = xmpDoc.Root.XPathSelectElements("//exif:*", namespaceManager);
                    foreach (var ele in exifElements)
                    {
                        if (ele.Name.LocalName == "GPSLatitude")
                            gpsLatitude = ele.Value;
                        if (ele.Name.LocalName == "GPSLongitude")
                            gpsLongitude = ele.Value;
                    }

                    if (gpsLatitude != null && gpsLongitude != null)
                    {
                        var latitude = convertXmpGeoLocation(gpsLatitude);
                        var longitude = convertXmpGeoLocation(gpsLongitude);

                        if (latitude.HasValue && longitude.HasValue)
                        {
                            Location = new Location(latitude.Value, longitude.Value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error reading XMP from '{0}': {1}", fullPath, e);
            }
            Keywords = list.ToArray();
        }

        private string GetJpegXmp(BufferedStream bufferedStream, BinaryReader reader)
        {
            // There can be multiple EXIF start markers; walk each one until we run out or we find XMP
            while (FindNextExifStart(bufferedStream, reader))
            {
                var startPosition = bufferedStream.Position;
                var dataLength = ReadUshort(reader);
                var signature = new String(reader.ReadChars(29));
                if (signature.Equals("http://ns.adobe.com/xap/1.0/\0"))
                {
                    // Parse the XML
                    var content = new String(reader.ReadChars(dataLength - signature.Length - 2));
                    var firstLen = content.Length;

                    int pos = content.LastIndexOf("?>");
                    if (pos > 0)
                    {
                        content = content.Substring(0, pos + 2);
                    }
                    return content;
                }

                bufferedStream.Seek(startPosition + dataLength, SeekOrigin.Begin);
            }

            return null;
        }

        private double? convertXmpGeoLocation(string geo)
        {
            // Split "47,33.366000N" into ["47", "33.366000", "N"]
            int posComma = geo.IndexOf(",");
            if (posComma < 0)
                return null;
            
            var pieces = new string[3];
            pieces[0] = geo.Substring(0, posComma);
            pieces[1] = geo.Substring(posComma + 1, geo.Length - 1 - posComma - 1);
            pieces[2] = geo.Last().ToString();

            double degrees;
            double minutesAndSeconds;
            if (!Double.TryParse(pieces[0], out degrees) || !Double.TryParse(pieces[1], out minutesAndSeconds))
                return null;

            var val = degrees + (minutesAndSeconds / 60.0);
            if (pieces[2] == "S" || pieces[2] == "W")
                val *= -1.0;

            return val;
        }

        private bool FindNextExifStart(Stream stream, BinaryReader reader)
        {
            byte markerStart;
            byte markerNumber = 0;
            while (((markerStart = reader.ReadByte()) == 0xFF) && (markerNumber = reader.ReadByte()) != 0xE1)
            {
                var dataLength = ReadUshort(reader);
                stream.Seek(dataLength - 2, SeekOrigin.Current);
            }

            return markerStart == 0xFF && markerNumber == 0xE1;
        }

        private ushort ReadUshort(BinaryReader reader)
        {
            var data = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }
    }

    class VideoAtomParser : IDisposable
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public string Xml { get; private set; }

        private FileStream fileStream;
        private int offset = 0;

        public VideoAtomParser(string filename)
        {
            fileStream = File.OpenRead(filename);
            Parse();
            Close();
        }

        private void Parse()
        {
            var rootAtoms = new List<VideoAtom>();
            do
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                var nextAtom = GetNextAtom();
                if (nextAtom == null)
                    break;

                rootAtoms.Add(nextAtom);
                offset += nextAtom.Length;

            } while (offset < fileStream.Length);

            ParseUuidAtom(rootAtoms);
        }

        private void ParseUuidAtom(List<VideoAtom> rootAtoms)
        {
            var uuidAtom = rootAtoms.FirstOrDefault( a => a.Type == "uuid");
            if (uuidAtom != null)
            {
                // The uuid atom offset points to the location with:
                //      atom length (4 bytes)
                //      atom type (4 bytes) "uuid"
                //      16 bytes of unknown data
                //      XML!
                fileStream.Seek(uuidAtom.Offset + 4 + 4 + 16, SeekOrigin.Begin);

                var xmlBytes = new byte[uuidAtom.Length - 4 - 4 - 16];
                var bytesRead = fileStream.Read(xmlBytes, 0, xmlBytes.Length);
                if (bytesRead != xmlBytes.Length)
                {
                    logger.Info("Didn't read the proper amount of data: {0}, expected {1}", bytesRead, xmlBytes.Length);
                    return;
                }

                Xml = System.Text.Encoding.Default.GetString(xmlBytes);
            }
        }

        private void Close()
        {
            if (fileStream != null)
            {
                fileStream.Close();
                fileStream = null;
            }
        }

        public void Dispose()
        {
            Close();
        }

        private VideoAtom GetNextAtom()
        {
            var atomLengthBytes = new byte[4];
            if (4 != fileStream.Read(atomLengthBytes, 0, 4))
                return null;

            var typeBytes = new byte[4];
            if (4 != fileStream.Read(typeBytes, 0, 4))
                return null;

            Array.Reverse(atomLengthBytes);
            int length = BitConverter.ToInt32(atomLengthBytes, 0);
            if (length < 0)
                return null;
            return new VideoAtom(System.Text.Encoding.Default.GetString(typeBytes), offset, length);
        }
    }

    class VideoAtom
    {
        public int Offset { get; private set; }
        public int Length { get; private set; }
        public string Type { get; private set; }
        public IList<VideoAtom> Children { get; private set; }

        public VideoAtom(string type, int offset, int length)
        {
            Type = type;
            Offset = offset;
            Length = length;
        }
    }
}
