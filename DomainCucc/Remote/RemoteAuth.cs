﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCucc.Remote
{
    internal class RemoteAuth
    {
        private readonly string filePath = "Data/RemoteLoginValues.dat";
        public Dictionary<string, string> loginValues {get; set;}
        public RemoteAuth() { 
            this.loginValues = new Dictionary<string, string>();     
            string[] strings = File.ReadAllLines(filePath);
            foreach (string s in strings)
            {
                loginValues.Add(s.Split(';')[0], s.Split(';')[1]);
            }
        }
        public bool Login(string username, string password)
        {
            bool success = false;
            if (loginValues.TryGetValue(username, out string storedPassword))
            {
                if (storedPassword == password)
                {
                    success = true;         
                }
            }
            return success;
        }
        public bool Register(string username, string password)
        {
            File.AppendAllLinesAsync(filePath, new List<string>() { username+";"+password }); 
            return true;
        
        }
       
    }
}
