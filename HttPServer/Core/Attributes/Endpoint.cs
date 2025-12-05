using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttPServer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Endpoint : Attribute
    {
        public Endpoint() { }
    }
}
