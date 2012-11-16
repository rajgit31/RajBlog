#region Using

using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

#endregion

namespace BlogEngine.Core.Ping
{
  /// <summary>
  /// 
  /// </summary>
  public static class Trackback
  {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>    
    public static bool Send(TrackbackMessage message)
    {
			if (!BlogSettings.Instance.EnableTrackBackSend)
				return false;

      if (message == null)
        throw new ArgumentNullException("message");

      OnSending(message.UrlToNotifyTrackback);
      //Warning:next line if for local debugging porpuse please donot remove it until you need to
      //tMessage.PostURL = new Uri("http://www.artinsoft.com/webmaster/trackback.html");
      HttpWebRequest request = (HttpWebRequest)System.Net.HttpWebRequest.Create(message.UrlToNotifyTrackback); //HttpHelper.CreateRequest(trackBackItem);
      request.Credentials = CredentialCache.DefaultNetworkCredentials;
      request.Method = "POST";
      request.ContentLength = message.ToString().Length;
      request.ContentType = "application/x-www-form-urlencoded";
      request.KeepAlive = false;
      request.Timeout = 10000;

      using (StreamWriter myWriter = new StreamWriter(request.GetRequestStream()))
      {
        myWriter.Write(message.ToString());
      }

      bool result = false;
      HttpWebResponse response;
      try
      {
        response = (HttpWebResponse)request.GetResponse();
        OnSent(message.UrlToNotifyTrackback);
        string answer;
        using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
        {
          answer = sr.ReadToEnd();
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
          //todo:This could be a strict XML parsing if necesary/maybe logging activity here too
          if (answer.Contains("<error>0</error>"))
          {
            result = true;
          }
          else
          {
            result = false;
          }
        }
        else
        {
          result = false;
        }
      }
      catch //(WebException wex)
      {
        result = false;
      }
      return result;
    }

    #region

    /// <summary>
    /// Occurs just before a trackback is sent.
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
    /// Occurs when a trackback has been sent
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

	/// <summary>
	/// 
	/// </summary>
  public class TrackbackMessage
  {
    private string _Title;
		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
    public string Title
    {
      get { return _Title; }
      set { _Title = value; }
    }
    
    private Uri _PostUrl;
		/// <summary>
		/// Gets or sets the post URL.
		/// </summary>
		/// <value>The post URL.</value>
    public Uri PostUrl
    {
      get { return _PostUrl; }
      set { _PostUrl = value; }
    }

    private string _Excerpt;
		/// <summary>
		/// Gets or sets the excerpt.
		/// </summary>
		/// <value>The excerpt.</value>
    public string Excerpt
    {
      get { return _Excerpt; }
      set { _Excerpt = value; }
    }

    private string _BlogName;
		/// <summary>
		/// Gets or sets the name of the blog.
		/// </summary>
		/// <value>The name of the blog.</value>
    public string BlogName
    {
      get { return _BlogName; }
      set { _BlogName = value; }
    }

    
    private Uri _UrlToNotifyTrackback;
		/// <summary>
		/// Gets or sets the URL to notify trackback.
		/// </summary>
		/// <value>The URL to notify trackback.</value>
    public Uri UrlToNotifyTrackback
    {
      get { return _UrlToNotifyTrackback; }
      set { _UrlToNotifyTrackback = value; }
    }

		/// <summary>
		/// Initializes a new instance of the <see cref="TrackbackMessage"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="urlToNotifyTrackback">The URL to notify trackback.</param>
    public TrackbackMessage(IPublishable item, Uri urlToNotifyTrackback, Uri itemUrl)
    {
      if (item == null)
        throw new ArgumentNullException("post");

      Title = item.Title;
      PostUrl = itemUrl;
      Excerpt = item.Title;
      BlogName = BlogSettings.Instance.Name;
      UrlToNotifyTrackback = urlToNotifyTrackback;
    }

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "title={0}&url={1}&excerpt={2}&blog_name={3}", Title, PostUrl, Excerpt, BlogName);
    }

  }
}