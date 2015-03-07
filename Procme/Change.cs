using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Procme
{
    public sealed class Change
    {
        public Dictionary<int, string> Changed = new Dictionary<int, string>();
        internal static Change Load(string pFile)
        {
            Change derp;
            using (XmlReader xr = XmlReader.Create(pFile))
            {
                XmlSerializer xs = new XmlSerializer(typeof(Change));
                derp = xs.Deserialize(xr) as Change;
            }
            return derp;
        }

        internal void Save(string pFile)
        {
            XmlWriterSettings xws = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = true,
                OmitXmlDeclaration = true
            };
            using (XmlWriter xw = XmlWriter.Create(pFile, xws))
            {
                XmlSerializer xs = new XmlSerializer(typeof(Change));
                xs.Serialize(xw, this);
            }
        }
    }
}
