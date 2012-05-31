using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Spotify
{
  /// <summary>
  /// Base class for any Spotify item (track, album and artist)
  /// </summary>
  public abstract class SpotifyItem
  {
    internal virtual void Load(XmlElement element)
    {
      name = element.SelectSingleNode("spotify:name", NamespaceManager.Instance).InnerText;
      popularity = double.Parse(element.SelectSingleNode("spotify:popularity", NamespaceManager.Instance).InnerText);
      url = element.GetAttribute("href");
    }

    private string name;
    /// <summary>
    /// Gets the name of the item.
    /// </summary>
    public string Name
    {
      get
      {
        return name;
      }
    }

    private double popularity;
    /// <summary>
    /// Gets a number between 0 and 1 that signifies how popular the item is.
    /// </summary>
    public double Popularity
    {
      get
      {
        return popularity;
      }
    }

    private string url;
    /// <summary>
    /// Gets the Spotify URL for the item.
    /// </summary>
    public string Url
    {
      get
      {
        return url;
      }
    }
  }
}
