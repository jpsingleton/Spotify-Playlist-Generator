using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Spotify
{
  internal static class NamespaceManager
  {
    private static XmlNamespaceManager instance;
    public static XmlNamespaceManager Instance
    {
      get
      {
        if (instance == null)
        {
          instance = new XmlNamespaceManager(new NameTable());
          instance.AddNamespace("opensearch", "http://a9.com/-/spec/opensearch/1.1/");
          instance.AddNamespace("spotify", "http://www.spotify.com/ns/music/1");
        }
        return instance;
      }
    }
  }
}
