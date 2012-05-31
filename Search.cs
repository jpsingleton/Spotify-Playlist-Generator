using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;

namespace Spotify
{
    /// <summary>
    /// Class providing static methods to search the Spotify music database
    /// </summary>
    public static class Search
    {
        /// <summary>
        /// Searches Spotify for artists matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The artist to search for.</param>
        /// <returns>A list of matching artists and information about the total number of results available</returns>
        public static SearchResults<Artist> SearchArtists(string searchFor)
        {
            return SearchArtists(searchFor, 1);
        }

        /// <summary>
        /// Searches Spotify for artists matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The artist to search for.</param>
        /// <param name="page">The page of results to return.</param>
        /// <returns>
        /// A list of matching artists and information about the total number of results available
        /// </returns>
        public static SearchResults<Artist> SearchArtists(string searchFor, int page)
        {
            string url = string.Format("http://ws.spotify.com/search/1/artist?q={0}&page={1}", searchFor, page);
            return MakeHtpRequest<Artist>(url, "//spotify:artist");
        }

        /// <summary>
        /// Searches Spotify for albums matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The album to search for.</param>
        /// <returns>
        /// A list of matching albums and information about the total number of results available
        /// </returns>
        public static SearchResults<Album> SearchAlbums(string searchFor)
        {
            return SearchAlbums(searchFor, 1);
        }

        /// <summary>
        /// Searches Spotify for albums matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The album to search for.</param>
        /// <param name="page">The page of results to return.</param>
        /// <returns>
        /// A list of matching albums and information about the total number of results available
        /// </returns>
        public static SearchResults<Album> SearchAlbums(string searchFor, int page)
        {
            string url = string.Format("http://ws.spotify.com/search/1/album?q={0}&page={1}", searchFor, page);
            return MakeHtpRequest<Album>(url, "//spotify:album");
        }

        /// <summary>
        /// Searches Spotify for tracks matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The track to search for.</param>
        /// <returns>
        /// A list of matching tracks and information about the total number of results available
        /// </returns>
        public static SearchResults<Track> SearchTracks(string searchFor)
        {
            return SearchTracks(searchFor, 1);
        }

        /// <summary>
        /// Searches Spotify for tracks matching the specified search string.
        /// </summary>
        /// <param name="searchFor">The track to search for.</param>
        /// <param name="page">The page of results to return.</param>
        /// <returns>
        /// A list of matching tracks and information about the total number of results available
        /// </returns>
        public static SearchResults<Track> SearchTracks(string searchFor, int page)
        {
            string url = string.Format("http://ws.spotify.com/search/1/track?q={0}&page={1}", searchFor, page);
            return MakeHtpRequest<Track>(url, "//spotify:track");
        }

        private static SearchResults<T> MakeHtpRequest<T>(string request, string xpath) where T : SpotifyItem, new()
        {
            // *** JS Bug Fix - prevents re-encoding of escape characters (there is probably a better way to do this)
            Uri u = new Uri(request, true);

            HttpWebRequest myReq =
              (HttpWebRequest)WebRequest.Create(u);

            // *** JS Bug Fix - prevents Spotify from rejecting due to too many connections
            myReq.KeepAlive = false;

            // Sends the HttpWebRequest and waits for the response.
            using (HttpWebResponse myHttpWebResponse = myReq.GetResponse() as HttpWebResponse)
            {
                Stream response = myHttpWebResponse.GetResponseStream();
                StreamReader readStream = new StreamReader(response, Encoding.UTF8);
                string responseString = readStream.ReadToEnd();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(responseString);
                SearchResults<T> results = new SearchResults<T>();
                results.Load(doc, xpath);
                return results;
            }
        }

        //JS - quick new method for track details lookup
        public static SearchResults<Track> Lookup(string link)
        {
            string trackID = link.Replace("http://open.spotify.com/track/", "");
            trackID = trackID.Replace("spotify:track:", "");
            string url = string.Format("http://ws.spotify.com/lookup/1/?uri=spotify%3Atrack%3A{0}", trackID);
            return MakeHtpRequest<Track>(url, "//spotify:track");
        }
    }
}
