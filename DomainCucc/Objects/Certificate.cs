using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainCucc.Objects
{
    internal class Certificate
    {
        public DateTime Expire { get; set; }
        public string filePath { get; set; }

        public Certificate(DateTime expire, string filePath)
        {
            this.Expire = expire;
            this.filePath = filePath;
        }
    
    }
}
