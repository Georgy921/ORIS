using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XProtocol.Serializator;

namespace XProtocol
{
    public class XPacketPoint
    {
        [XField(1)] public int X;
        [XField(2)] public int Y;
        [XField(3)] public string Username;
        [XField(4)] public string ColorHex;
    }
    public class XPacketMapState
    {
        [XField(1)] public string SerializedPointsJson;
    }
}
