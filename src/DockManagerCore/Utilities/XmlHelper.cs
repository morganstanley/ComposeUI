using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;

namespace DockManagerCore.Utilities
{
    static class XmlHelper
    {
        public static string ReadAttribute(XmlNode node_, string name_, string defaultValue_ = null)
        {
            XmlAttribute attribute = node_.Attributes[name_];
            if (attribute != null)
            {
                return attribute.Value;
            }
            return defaultValue_;
        }
        public static DateTime ReadAttribute(XmlNode node_, string name_, DateTime defaultValue_)
        {
            string s = ReadAttribute(node_, name_);
            if (s == null)
            {
                return defaultValue_;
            }
            return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Utc);
        }
        public static T ReadAttributeWithConverter<T>(XmlNode node_, string name_, T defaultValue_)
        {
            string text = ReadAttribute(node_, name_);
            if (text == null)
            {
                return defaultValue_;
            }
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }
        public static T ReadEnumAttribute<T>(XmlNode node_, string name_, T defaultValue_)
        {
            string str = ReadAttribute(node_, name_);
            if (str == null)
            {
                return defaultValue_;
            }
            return (T)Enum.Parse(defaultValue_.GetType(), str);
        }


        public static bool ReadAttribute(XmlNode node_, string name_, bool defaultValue_)
        {
            string s = ReadAttribute(node_, name_);
            if (s == null)
            {
                return defaultValue_;
            }
            return XmlConvert.ToBoolean(s);
        }

        public static void WriteAttributeWithConverter<T>(XmlWriter writer_, string name_, T value_)
        {
            writer_.WriteAttributeString(name_, TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value_) ?? string.Empty);

        }
         
        public static void WriteAttribute(XmlWriter writer_, string name_, DateTime value_)
        {
            writer_.WriteAttributeString(name_, XmlConvert.ToString(value_, XmlDateTimeSerializationMode.Utc));
        }
 
    }
}
