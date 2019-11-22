using System;
using System.IO;
using System.Xml.Serialization;
using Grouper2.Utility;
using Newtonsoft.Json;

namespace Grouper2.Host.SysVol.Files
{
    public class XmlSysvolFile : SysvolFile
    {
        
        public XmlSysvolFile(string path) : base(path)
        {
            this.Type = SysvolObjectType.XmlFile;
        }
        public FakeXmlBullshit ReadData()
        {
            try
            {
                string xml;
                FileStream fs = new FileStream(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(fs))
                {
                    xml = sr.ReadToEnd();
                }
                return new FakeXmlBullshit(xml);
            }
            catch (Exception e)
            {
                Log.Degub("TODO:", e, this);
                // fail silently and move on
                // we know not all files will be able to be read
                return null;
            }
        }
    }
}