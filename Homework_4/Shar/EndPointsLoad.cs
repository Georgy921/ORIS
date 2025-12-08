using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HttPServer.Core.Attributes;

namespace HttPServer.Utils
{
    public static class EndpointsRegistry
    {
        public static Dictionary<(string Route, string HttpMethod), (Type Type, MethodInfo Method)> LoadEndpoints(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .Where(t => t.GetCustomAttribute<Endpoint>() != null)
                .SelectMany(type => type
                    .GetMethods()
                    .SelectMany(method => GetHttpMappings(type, method)))
                .ToDictionary(x => (x.Route, x.HttpMethod), x => (x.Type, x.Method));
        }

        private static IEnumerable<(string Route, string HttpMethod, Type Type, MethodInfo Method)> GetHttpMappings(Type type, MethodInfo method)
        {
            var get = method.GetCustomAttribute<HttpGet>();
            if (get != null)
                yield return (get.Route, "GET", type, method);

            var post = method.GetCustomAttribute<HttpPost>();
            if (post != null)
                yield return (post.Route, "POST", type, method);
        }
    }
}
