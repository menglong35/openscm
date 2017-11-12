using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// 序列化 反序列化帮助类
    /// </summary>
    public class SerializationUtils
    {

        /// <summary>
        /// 将对象序列化为Xml
        /// </summary>
        /// <param name="value">需要序列化的对象</param>
        /// <returns>返回XmlString</returns>
        public static string XmlSerialize(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            var serializer = new XmlSerializer(value.GetType());
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            //XmlWriter设置 不写入Xml声明 缩进 使用UTF-8编码
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = true
            };
            using (var textWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value, ns);
                }
                return textWriter.ToString();
            }
        }

        /// <summary>
        /// 将Xml反序列化成对象
        /// </summary>
        /// <typeparam name="T">序列化类型</typeparam>
        /// <param name="xml">需要反序列化的字符串</param>
        /// <returns>反序列化后的对象</returns>
        public static T XmlDeserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }
            var serializer = new XmlSerializer(typeof(T));
            //与XmlWriter不同的是，XmlReader并不需要Setting
            using (var textReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(textReader))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        /// <summary>
        /// 将Xml反序列化成对象
        /// </summary>
        /// <typeparam name="T">序列化类型</typeparam>
        /// <param name="fs">需要反序列化的字符串</param>
        /// <returns>反序列化后的对象</returns>
        public static T XmlDeserialize<T>(Stream fs)
        {
            if (fs == Stream.Null)
            {
                return default(T);
            }
            var serializer = new XmlSerializer(typeof(T));
            //与XmlWriter不同的是，XmlReader并不需要Setting
            return (T)serializer.Deserialize(fs);
        }

        /// <summary>
        /// 把对象序列化成Json
        /// </summary>
        /// <param name="value">需要序列化的对象</param>
        /// <returns>序列化后的Json 字符串</returns>
        public static string JsonSerialization(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    //忽略空值
                    var jSetting = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatString = "yyyy-MM-dd HH:mm:ss",
                    };
                    var js = JsonSerializer.CreateDefault(jSetting);
                    js.Serialize(jw, value);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将Json反序列化成对象
        /// </summary>
        /// <typeparam name="T">序列化类型</typeparam>
        /// <param name="json">需要反序列化的字符串</param>
        /// <returns>反序列化后的对象</returns>
        public static T JsonDeserialize<T>(string json)
        {
            T value;
            if (string.IsNullOrEmpty(json))
            {
                return default(T);
            }

            using (var sReader = new StringReader(json))
            {
                using (JsonReader jReader = new JsonTextReader(sReader))
                {
                    //忽略空值
                    var jSetting = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatString = "yyyy-MM-dd HH:mm:ss",
                    };
                    var js = JsonSerializer.CreateDefault(jSetting);
                    value = js.Deserialize<T>(jReader);
                }
            }
            return value;
        }

        /// <summary>
        /// 从文件中读取数据 进行反序列化
        /// </summary>
        /// <typeparam name="T">反序列化后的类型</typeparam>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static T XmlDeserializeFromFile<T>(string fileName)
        {
            var xml = File.ReadAllText(fileName);
            return XmlDeserialize<T>(xml);
        }

    }
}
