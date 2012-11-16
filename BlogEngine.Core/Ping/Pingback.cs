#region Using

using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BlogEngine.Core;

#endregion

namespace BlogEngine.Core.Ping
{
  /// <summary>
  /// Sends pingbacks to website that the blog links to.
  /// </summary>
  public static class Pingback
  {

    /// <summary>
    /// Sends pingbacks to the targetUrl.
    /// </summary>
    public static void Send(Uri sourceUrl, Uri targetUrl)
    {
			if (!BlogSettings.Instance.EnablePingBackSend)
				return;

      if (sourceUrl == null || targetUrl == null)
        return;

      try
      {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
        request.Credentials = CredentialCache.DefaultNetworkCredentials;
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string pingUrl = null;

        int pingUrlKeyIndex = Array.FindIndex(response.Headers.AllKeys,
            delegate(string k)
            {
                return k.Equals("x-pingback", StringComparison.OrdinalIgnoreCase) ||
                       k.Equals("pingback", StringComparison.OrdinalIgnoreCase);
            });

        if (pingUrlKeyIndex != -1)
            pingUrl = response.Headers[pingUrlKeyIndex];

        Uri url;
        if (!string.IsNullOrEmpty(pingUrl) && Uri.TryCreate(pingUrl, UriKind.Absolute, out url))
        {
          OnSending(url);
          request = (HttpWebRequest)HttpWebRequest.Create(url);
          request.Method = "POST";
          //request.Timeout = 10000;
          request.ContentType = "text/xml";
          request.ProtocolVersion = HttpVersion.Version11;
					request.Headers["Accept-Language"] = "en-us";
          AddXmlToRequest(sourceUrl, targetUrl, request);
					HttpWebResponse response2 = (HttpWebResponse)request.GetResponse();
					response2.Close();
          OnSent(url);
        }
      }
      catch (Exception ex)
      {
				ex = new Exception();
				
        // Stops unhandled exceptions that can cause the app pool to recycle
      }
    }

    /// <summary>
    /// Adds the XML to web request. The XML is the standard
    /// XML used by RPC-XML requests.
    /// </summary>
    private static void AddXmlToRequest(Uri sourceUrl, Uri targetUrl, HttpWebRequest webreqPing)
    {
      Stream stream = (Stream)webreqPing.GetRequestStream();
      using (XmlTextWriter writer = new XmlTextWriter(stream, Encoding.ASCII))
      {
        writer.WriteStartDocument(true);
        writer.WriteStartElement("methodCall");
        writer.WriteElementString("methodName", "pingback.ping");
        writer.WriteStartElement("params");

        writer.WriteStartElement("param");
        writer.WriteStartElement("value");
        writer.WriteElementString("string", sourceUrl.ToString());
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("param");
        writer.WriteStartElement("value");
        writer.WriteElementString("string", targetUrl.ToString());
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndElement();
      }
    }

    #region Events

    /// <summary>
    /// Occurs just before a pingback is sent.
    /// </summary>
    public static event EventHandler<EventArgs> Sending;
    private static void OnSending(Uri url)
    {
      if (Sending != null)
      {
        Sending(url, new EventArgs());
      }
    }

    /// <summary>
    /// Occurs when a pingback has been sent
    /// </summary>
    public static event EventHandler<EventArgs> Sent;
    private static void OnSent(Uri url)
    {
      if (Sent != null)
      {
        Sent(url, new EventArgs());
      }
    }

    #endregion

  }
}
