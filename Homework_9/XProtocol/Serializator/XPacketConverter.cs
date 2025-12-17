using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XProtocol.Serializator
{
    public class XPacketConverter
    {
        public static XPacket Serialize(XPacketType type, object obj, bool strict = false)
        {
            var t = XPacketTypeManager.GetType(type);
            return Serialize(t.Item1, t.Item2, obj, strict);
        }

        public static XPacket Serialize(byte type, byte subtype, object obj, bool strict = false)
        {
            var fields = GetFields(obj.GetType());

            if (strict)
            {
                var usedUp = new List<byte>();

                foreach (var field in fields)
                {
                    if (usedUp.Contains(field.Item2))
                    {
                        throw new Exception("One field used two times.");
                    }

                    usedUp.Add(field.Item2);
                }
            }

            var packet = XPacket.Create(type, subtype);

            foreach (var field in fields)
            {
                packet.SetValue(field.Item2, field.Item1.GetValue(obj));
            }

            return packet;
        }

        public static T Deserialize<T>(XPacket packet, bool strict = false)
        {
            var fields = GetFields(typeof(T));
            var instance = Activator.CreateInstance<T>();

            if (fields.Count == 0)
            {
                return instance;
            }

            foreach (var tuple in fields)
            {
                var field = tuple.Item1;
                var packetFieldId = tuple.Item2;

                if (!packet.HasField(packetFieldId))
                {
                    if (strict)
                    {
                        throw new Exception($"Couldn't get field[{packetFieldId}] for {field.Name}");
                    }

                    continue;
                }

                var value = typeof(XPacket)
                    .GetMethod("GetValue")?
                    .MakeGenericMethod(field.FieldType)
                    .Invoke(packet, new object[] { packetFieldId });

                if (value == null)
                {
                    if (strict)
                    {
                        throw new Exception($"Couldn't get value for field[{packetFieldId}] for {field.Name}");
                    }

                    continue;
                }

                field.SetValue(instance, value);
            }

            return instance;
        }

        private static List<Tuple<FieldInfo, byte>> GetFields(Type t)
        {
            return t.GetFields(BindingFlags.Instance |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Public)
                .Where(field => field.GetCustomAttribute<XFieldAttribute>() != null)
                .Select(field => Tuple.Create(field, field.GetCustomAttribute<XFieldAttribute>().FieldID))
                .ToList();
        }

        public static T ConvertBytesToValueType<T>(byte[] bytes)
        {
            var targetType = typeof(T);

            if (targetType == typeof(int))
            {
                return (T)(object)BitConverter.ToInt32(bytes, 0);
            }
            if (targetType == typeof(short))
            {
                return (T)(object)BitConverter.ToInt16(bytes, 0);
            }
            if (targetType == typeof(float))
            {
                return (T)(object)BitConverter.ToSingle(bytes, 0);
            }
            if (targetType == typeof(double))
            {
                return (T)(object)BitConverter.ToDouble(bytes, 0);
            }
            if (targetType == typeof(long))
            {
                return (T)(object)BitConverter.ToInt64(bytes, 0);
            }
            if (targetType == typeof(byte))
            {
                return (T)(object)bytes[0];
            }
            if (targetType == typeof(bool))
            {
                return (T)(object)BitConverter.ToBoolean(bytes, 0);
            }
            if (targetType.IsLayoutSequential || targetType.IsExplicitLayout)
            {
                T structure;
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

                try
                {
                    structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), targetType);
                }
                finally
                {
                    handle.Free();
                }
                return structure;
            }

            throw new NotSupportedException($"Cannot convert byte array to unsupported value type: {targetType.FullName}");
        }
    }
}
