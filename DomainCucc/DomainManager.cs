using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCucc
{
    internal class DomainManager
    {
        private readonly string path = "Data/DomainDatValues.dat";
        public List<Domain> domains { get; set; }

        public DomainManager()
        {
            domains = new List<Domain>();

            foreach(var i in File.ReadAllLines(path))
            {
                domains.Add(new Domain(i));
            }
        }

        public struct Domain
        {
            public string _Domain;
            public DomainType Type;
            public string Value;
            public bool isEnabled;
            public Domain(string s)
            {
                string[] db = s.Split(';');
                _Domain = db[0];
                if (db[1] == "0")
                {
                    Type = DomainType.FOLDER;
                } else if (db[1] == "1")
                {
                    Type = DomainType.TRANSFER;
                }
                else
                {
                    throw new ArgumentException("Not found r type");
                }
                Value = db[2];
                isEnabled = bool.Parse(db[3]);
            }
        }
        public enum DomainType
        {
            TRANSFER = 0, FOLDER = 1
        }
        public Dictionary<string, string> DecodeBody(string body)
        {
            Dictionary<string, string> b = new Dictionary<string, string>();

            string[] strings = body.Split('&');
            foreach (string s in strings)
            {
                b.Add(s.Split('=')[0], s.Split('=')[1]);
            }
            return b;
        }
    }

}
