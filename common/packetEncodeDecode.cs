using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;

namespace common
{
    public static class packetEncodeDecode
    {
        public static string encodeID(int id)
        {
            return "$I:" + id.ToString();
        }
        public static string encodeUpdate(int playerCount, bool lobby)
        {
            return "$U:" + (lobby ? "1:": "0:") + playerCount.ToString();
        }
        public static string encodeStart<T>(T info)
        {
            return "$S:" + JsonConvert.SerializeObject(info);
        }

        public static bool tryDecodeID(string encodedID, out int id)
        {
            if (encodedID.StartsWith("$I:"))
            {
                return int.TryParse(encodedID.Substring(3), out id);
            }

            id = 0;
            return false;
        }
        public static bool tryDecodeUpdate(string encodedUpdate, out int playerCount, out bool lobby)
        {
            if (encodedUpdate.StartsWith("$U:"))
            {
                lobby = encodedUpdate.Split(":")[1] == "1";
                return int.TryParse(encodedUpdate.Substring(5), out playerCount);
            }

            playerCount = 0;
            lobby = false;
            return false;
        }
        public static bool tryDecodeStart<T>(string encodedStart, out T info)
        {
            if (encodedStart.StartsWith("$S:"))
            {
                string json = encodedStart.Substring(3);
                info = JsonConvert.DeserializeObject<T>(json);
                return true;
            }

            info = default(T);
            return false;
        }

        public static string encodeObject<T>(T obj, int playerID, string label)
        {
            return playerID.ToString() + "^" + label + "^" + JsonConvert.SerializeObject(obj);
        }

        public static bool tryDecodeObject<T>(string encodedObject, out int playerID, out T obj, string label)
        {
            string[] packet = encodedObject.Split('^');

            if (!string.IsNullOrEmpty(encodedObject) && int.TryParse(packet[0], out playerID) && packet[1] == label)
            {
                string json = packet[2];
                obj = JsonConvert.DeserializeObject<T>(json);

                return true;
            }

            playerID = 0;
            obj = default(T);
            return false;
        }
    }
}
