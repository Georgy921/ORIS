using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttPServer.Settings;
using HttPServer.Utils;
using MiniHttpServer;
using MiniHttpServer.Utils;

namespace HttPServer.Suka
{
    public class Global
    {
        public static HTTPServer Server { get; set; }
        public static Logger Logger { get; set; }
        public static Singletone Settings { get; set; }
        public static Dictionary<(string, string), (Type, MethodInfo)> Endpoints { get; set; }
    }
}
