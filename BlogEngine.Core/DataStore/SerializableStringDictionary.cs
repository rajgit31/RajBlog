using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Collections;

namespace BlogEngine.Core.DataStore
{
  /// <summary>
  /// Serializable String Dictionary
  /// </summary>
  [CLSCompliant(true)]
  [Serializable()]
  public class SerializableStringDictionary : StringDictionary, IXmlSerializable
  {
    #region IXmlSerializable Members

    /// <summary>
    /// Get Schema
    /// </summary>
    /// <returns></returns>
    public System.Xml.Schema.XmlSchema GetSchema()
    {
      return null;
    }
    /// <summary>
    /// Generates a collection from its XML representation.
    /// </summary>
    /// <param name="reader">System.Xml.XmlReader</param>
    public void ReadXml(System.Xml.XmlReader reader)
    {
      this.Clear();
      if (reader.ReadToDescendant("SerializableStringDictionary"))
      {
        if (reader.ReadToDescendant("DictionaryEntry"))
        {
          do
          {
            reader.MoveToAttribute("Key");
            string key = reader.ReadContentAsString();
            reader.MoveToAttribute("Value");
            string value = reader.ReadContentAsString();

            this.Add(key, value);

          } while (reader.ReadToNextSibling("DictionaryEntry"));
        }
      }
    }
    /// <summary>
    /// Loads collection to XML writer
    /// </summary>
    /// <param name="writer">System.Xml.XmlWriter</param>
    public void WriteXml(System.Xml.XmlWriter writer)
    {
      writer.WriteStartElement("SerializableStringDictionary");
      foreach (DictionaryEntry entry in this)
      {
        writer.WriteStartElement("DictionaryEntry");
        writer.WriteAttributeString("Key", (string)entry.Key);
        writer.WriteAttributeString("Value", (string)entry.Value);
        writer.WriteEndElement();
      }
      writer.WriteEndElement();
    }

    #endregion
  }
}