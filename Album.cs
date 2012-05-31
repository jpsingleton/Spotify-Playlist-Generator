using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Spotify
{
  /// <summary>
  /// Class providing details of an album available on Spotify
  /// </summary>
  public class Album : SpotifyItem
  {
    internal override void Load(XmlElement albumElement)
    {
      base.Load(albumElement);
      artist = albumElement.SelectSingleNode("spotify:artist/spotify:name", NamespaceManager.Instance).InnerText;

      // availability in countries
      string availability = albumElement.SelectSingleNode("spotify:availability/spotify:territories", NamespaceManager.Instance).InnerText;
      availableInTerritories = availability.Split(' ');
    }

    private string artist;
    /// <summary>
    /// Gets the artist who recorded this album.
    /// </summary>
    /// <value>The artist.</value>
    public string Artist
    {
      get
      {
        return artist;
      }
    }

    private string[] availableInTerritories;
    /// <summary>
    /// Gets the list of territories that the album is available in.
    /// </summary>
    public string[] AvailableInTerritories
    {
      get { return availableInTerritories; }
    }
  }
}
