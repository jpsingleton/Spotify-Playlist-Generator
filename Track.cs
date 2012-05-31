using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Spotify
{
    /// <summary>
    /// Class providing details of a track available on Spotify
    /// </summary>
    public class Track : SpotifyItem
    {
        internal override void Load(XmlElement trackElement)
        {
            base.Load(trackElement);
            artist = trackElement.SelectSingleNode("spotify:artist/spotify:name", NamespaceManager.Instance).InnerText;
            length = TimeSpan.FromSeconds(double.Parse(trackElement.SelectSingleNode("spotify:length", NamespaceManager.Instance).InnerText));

            // *** JS addition - get where this is available so we can filter out tracks that won't play.
            string availability = trackElement.SelectSingleNode("spotify:album/spotify:availability/spotify:territories", NamespaceManager.Instance).InnerText;
            availableInTerritories = availability.Split(' ');
        }

        private TimeSpan length;
        /// <summary>
        /// Gets the length of the track.
        /// </summary>
        public TimeSpan Length
        {
            get
            {
                return length;
            }
        }

        private string artist;
        /// <summary>
        /// Gets the name of the artist who recorded the track.
        /// </summary>
        public string Artist
        {
            get
            {
                return artist;
            }
        }

        // *** JS addition - get where this is available so we can filter out tracks that won't play.
        private string[] availableInTerritories;
        /// <summary>
        /// Gets the list of territories that the track is available in.
        /// </summary>
        public string[] AvailableInTerritories
        {
            get { return availableInTerritories; }
        }
    }
}
