using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ShortcutLauncher
{
    public class JsonUtil
    {
        public static T ReadToObject<T>(string json)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var ser = new DataContractJsonSerializer(typeof(T));
            T deserializedObject = (T)ser.ReadObject(ms);
            ms.Close();

            return deserializedObject;
        }

        public static string ReadToString<T>(T jsonObject)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, jsonObject);
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);

            string strJSON = sr.ReadToEnd();

            return strJSON;
        }
    }
    [DataContract(Name = "ShortcutsJson")]
    public class ShortcutsJson
    {
        [DataMember(IsRequired = true)]
        public List<ShortcutJson> ShortcutJsonList { get; set; }
        public ShortcutsJson()
        {
            ShortcutJsonList = new List<ShortcutJson>();
        }
    }

    [DataContract(Name = "ShortcutJson")]
    public class ShortcutJson
    {
        [DataMember(IsRequired =true, Name ="FullPath")]
        public string Path { get; set; }
        [DataMember(IsRequired =true, Name ="Caption")]
        public string Caption { get; set; }
        [DataMember(IsRequired = true, Name = "TargetPath")]
        public string TargetPath { get; set; }
    }
}
