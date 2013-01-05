using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using OSM.API.v6;

namespace OsmBulkTools
{
    public class OsmFile
    {
        public static Osm Read(TextReader reader)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Osm));
            Osm osm = (Osm)ser.Deserialize(reader);
            return osm;
        }

        public static void Write(Osm osm, TextWriter writer)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Osm));
            ser.Serialize(writer, osm);
        }
    }
}
