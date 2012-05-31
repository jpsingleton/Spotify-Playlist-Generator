/*
 * A Spotify playlist generator using Last.fm.
 * 
 * Copyright (C) James Singleton 2011 (http://unop.co.uk)
 * 
 * Thanks to http://www.doogal.co.uk/spotify.php
 * and http://code.google.com/p/lastfm-sharp/
 * 
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Lastfm.Services;
using Spotify;
using System.Net;
using System.IO;
using System.Xml;

namespace PlaylistGenerator
{
    /// <summary>
    /// Spotify playlist generator using Last.fm.
    /// James Singleton - 2011 - unop.co.uk
    /// </summary>
    public partial class Window1 : Window
    {
        // Constants
        private const string BUTTON_IDLE_TEXT = "Generate Spotify Playlist";
        private const string BUTTON_WORKING_TEXT = "Generating Spotify Playlist... ({0}%)";
        private const string TITLE_IDLE_TEXT = "Playlist Generator";
        private const string TITLE_WORKING_TEXT = "{0}% - {1}";

        // Create background workers to do the requests on another thread.
        BackgroundWorker bw = new BackgroundWorker();
        BackgroundWorker type = new BackgroundWorker();

        // Variables for interface between background and UI threads.
        private string text = String.Empty;
        private string entryCache = String.Empty;
        private string artist = String.Empty;
        private bool requestOutOfDate = false;

        // Configuration
        private int MAX_ARTISTS = 49;
        private int TRACKS_PER_ARTIST = 3;

        // One for entered artist plus one to last.fm, one for update and one for region.
        private const int EXTRA_WEB_SERVICE_CALLS = 4;
        private int TOTAL_WEB_SERVICE_CALLS = 49 + EXTRA_WEB_SERVICE_CALLS;
        private int callCount = 0;

        // Region country code for the user.
        private string region = String.Empty;
        /// <summary>
        /// Get the local ISO country code
        /// Don't access from a UI thread
        /// </summary>
        private string Region
        {
            get
            {
                if (String.IsNullOrEmpty(region))
                    region = getCountryCode();
                return region;
            }
        }

        // URL to get latest version info from.
        private const string xmlURL = "http://unop.co.uk/spgVersion.php";
        // Version info from xml file
        private Version newVersion = null;
        // New version download link from xml file
        private string url = "http://unop.co.uk/";

        // Last.fm API key - you want to enter your own one in here (http://www.last.fm/api).
        Session s = new Session("Last.fm API key", "Last.fm API secret");

        public Window1()
        {
            InitializeComponent();

            // Setup the background workers.
            bw.DoWork += new DoWorkEventHandler(generatePlaylist);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(updateUI);
            bw.WorkerReportsProgress = true;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            type.DoWork += new DoWorkEventHandler(type_DoWork);
            type.RunWorkerCompleted += new RunWorkerCompletedEventHandler(type_RunWorkerCompleted);

            Title = TITLE_IDLE_TEXT;
            button1.Content = BUTTON_IDLE_TEXT;

            // Put the cursor in the artist text box.
            textBox1.Focus();
            textBox2.Text = "Spotify Playlist Generator using Last.fm\n\nInstructions:\n\nEnter an artist's name in the box above.\n(Start typing a name for a suggestion.)\n\nAlternatively select a few tracks that you like\nand paste the Spotify track links into this box.\n\nClick 'Generate' and wait until completion.\n\nCopy & paste the links into a Spotify playlist.\n(You can drag & drop instead if you prefer.)\n\nListen and enjoy!\n\nJames Singleton - 2011\nunop.co.uk\n";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            artist = textBox1.Text;
            if (!String.IsNullOrEmpty(artist))
            {
                // Update the UI.
                button1.IsEnabled = false;
                button1.Content = String.Format(BUTTON_WORKING_TEXT, "0");

                // Set title.
                Title = String.Format(TITLE_WORKING_TEXT, "0", artist);

                // Query in background.
                bw.RunWorkerAsync(artist);
            }
            else
            {
                string textBlob = textBox2.Text.Replace("\r", "");
                string[] lines = textBlob.Split('\n');
                List<string> artists = new List<string>();

                foreach (string line in lines)
                {
                    if (line.ToLower().Contains("http://open.spotify.com/track/")
                        || line.ToLower().Contains("spotify:track:"))
                    {
                        artists.Add(line);
                    }
                }

                // Check if there is anything to do.
                if (artists.Count < 1)
                {
                    MessageBox.Show("You didn't enter any artists", "Whoops", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TOTAL_WEB_SERVICE_CALLS = artists.Count * (MAX_ARTISTS + 2);

                if (artists.Count > 5)
                {
                    if (MessageBox.Show("You entered a lot of tracks. This could take a while. Are you sure?",
                        "Really?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return;
                }

                // Update the UI.
                button1.IsEnabled = false;
                button1.Content = String.Format(BUTTON_WORKING_TEXT, "0");

                artist = "Various";

                // Set title.
                Title = String.Format(TITLE_WORKING_TEXT, "0", artist);

                // Query in background.
                bw.RunWorkerAsync(artists.ToArray());
            }

            // Reset
            text = String.Empty;
            textBox2.Clear();
        }

        private void generatePlaylist(object sender, DoWorkEventArgs e)
        {
            // StringBuilder to store the results.
            StringBuilder sb = new StringBuilder();
            callCount = 0;

            // Get latest version info from web.
            checkVersion();

            if (e.Argument.GetType() == typeof(string))
            {
                generatePlaylistForArtist(e.Argument.ToString(), sb);
            }
            else if (e.Argument.GetType() == typeof(string[]))
            {
                // Dictionary for duplicate artist name detection.
                Dictionary<string, string> artists = new Dictionary<string, string>();
                foreach (string line in (string[])e.Argument)
                {
                    // Update progress.
                    bw.ReportProgress((int)(((float)(callCount) / (float)(TOTAL_WEB_SERVICE_CALLS)) * 100));
                    callCount++;
                    Spotify.Track st;
                    try
                    {
                        st = Search.Lookup(line.Trim()).SearchResultsPage.First();
                    }
                    catch
                    {
                        continue;
                    }
                    if (!artists.ContainsKey(st.Artist))
                    {
                        artists.Add(st.Artist, "");
                    }
                    Thread.Sleep(100);
                }
                TOTAL_WEB_SERVICE_CALLS = callCount + ((MAX_ARTISTS + 2) * artists.Count);
                foreach (KeyValuePair<string, string> kvp in artists)
                {
                    generatePlaylistForArtist(kvp.Key, sb);
                }
            }
            else
            {
                // Nothing.
            }
        }

        private void generatePlaylistForArtist(string artistName, StringBuilder sb)
        {
            // Artist and track counters.
            int aCount = 0;
            int tCount = 0;

            // Update progress.
            bw.ReportProgress((int)(((float)(callCount) / (float)(TOTAL_WEB_SERVICE_CALLS)) * 100));

            // Setup the search for similar artists to the one entered.
            Lastfm.Services.Artist a = new Lastfm.Services.Artist(artistName, s);
            List<Lastfm.Services.Artist> similarArtists = new List<Lastfm.Services.Artist>();

            // Include tracks for the artist entered.
            similarArtists.Add(a);

            // Make the call to last.fm
            callCount++;
            try
            {
                similarArtists.AddRange(a.GetSimilar(MAX_ARTISTS));
            }
            catch
            {
                text = "Error contacting Last.fm :(";
                return;
            }

            // Loop through the similar artists.
            foreach (Lastfm.Services.Artist similarArtist in similarArtists)
            {
                // Report progress to update the percentage on the UI.
                bw.ReportProgress((int)(((float)(callCount) / (float)(TOTAL_WEB_SERVICE_CALLS)) * 100));

                // Setup track list and counters. 
                SearchResults<Spotify.Track> sr = new SearchResults<Spotify.Track>();
                tCount = 0;
                callCount++;

                try
                {
                    // Throttle requests to 10/sec so as not to get blocked.
                    Thread.Sleep(100);

                    // Make a call to find tracks for the artist.
                    sr = Search.SearchTracks(Uri.EscapeDataString(similarArtist.Name));
                }
                catch
                {
                    // Something went wrong, back off and try again.
                    Thread.Sleep(3000);
                    try
                    {
                        // Make another call to find tracks for the artist.
                        sr = Search.SearchTracks(Uri.EscapeDataString(similarArtist.Name));
                    }
                    catch
                    {
                        // Something went wrong again, wait then move on.
                        Thread.Sleep(7000);
                        continue;
                    }
                }

                // Dictionary for duplicate track name detection.
                Dictionary<string, string> tracks = new Dictionary<string, string>();

                // Process tracks for this similar artist.
                foreach (Spotify.Track t in sr.SearchResultsPage)
                {
                    // Soft compare artist name.
                    if (similarArtist.Name.ToLower().Split('&').First().Trim() == t.Artist.ToLower().Trim())
                    {
                        // Duplicate track name detection.
                        // Add to dictionary with name as key.
                        string normalisedTrackName = Regex.Replace(t.Name, "\\s+", " ");
                        if (!tracks.ContainsKey(normalisedTrackName))
                        {
                            // Add the spotify track link if available.
                            if (t.AvailableInTerritories.Contains(Region) || t.AvailableInTerritories.Contains("worldwide"))
                            {
                                // Add URL to output.
                                sb.AppendLine(t.Url);

                                // Add the track to the dictionary.
                                tracks.Add(normalisedTrackName, null);

                                // Exit the loop if we have enough tracks.
                                if (++tCount >= TRACKS_PER_ARTIST) break;
                            }
                        }
                    }
                }

                // Send our results back to the UI thread.
                text = sb.ToString();

                // Exit the loop if we have enough artists.
                if (++aCount >= MAX_ARTISTS) break;
            }
        }

        private void updateUI(object sender, RunWorkerCompletedEventArgs e)
        {
            // Update UI.
            textBox2.Text = text;

            // Enable button.
            button1.IsEnabled = true;
            button1.Content = BUTTON_IDLE_TEXT;

            // Re-set title.
            Title = TITLE_IDLE_TEXT;

            // Higlight the results ready for copy & paste.
            textBox2.SelectAll();
            textBox2.Focus();

            // Message Box with instructions 
            MessageBox.Show(this, "Your Spotify playlist has been generated. Copy & paste or drag & drop all of the text in the main box into a Spotify playlist. Enjoy!",
                "Spotify Playlist Generated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Update UI.
            textBox2.Text = text;
            button1.Content = String.Format(BUTTON_WORKING_TEXT, e.ProgressPercentage.ToString());

            // Set title.
            Title = String.Format(TITLE_WORKING_TEXT, e.ProgressPercentage.ToString(), artist);

            // See if this is the most recent version.
            compareVersion();
        }

        //TODO change to combo box.
        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't do aything if the text hasn't changed.
            if (entryCache != textBox1.Text)
            {
                // Length of last entry.
                int lastCharactersEntered = entryCache.Length;
                // Length of this entry.
                int charactersEntered = textBox1.Text.Length;
                // Cache the entered text.
                entryCache = textBox1.Text;

                // Do do anything unless there is just one more character.
                if (lastCharactersEntered + 1 == charactersEntered)
                {
                    // Test if there is already a call in progress.
                    if (type.IsBusy)
                    {
                        requestOutOfDate = true;
                    }
                    else
                    {
                        // Request a suggestion in the background.
                        type.RunWorkerAsync(textBox1.Text);
                        requestOutOfDate = false;
                    }
                }
            }
        }

        private string getSuggestedArtist(string searchTerm)
        {
            try
            {
                //Throttle calls.
                Thread.Sleep(100);

                // Make a call to Spotify with the text entered and a wildcard terminator.
                foreach (Spotify.Artist a in Search.SearchArtists(Uri.EscapeDataString(searchTerm) + '*').SearchResultsPage)
                {
                    // Get the first result which begins with the search term.
                    if (a.Name.ToLower().StartsWith(searchTerm.ToLower()))
                    {
                        return a.Name;
                    }
                }
            }
            catch
            {
                // Something went wrong, wait then move on.
                Thread.Sleep(1000);
            }
            // Nothing found so return what came in.
            return searchTerm;
        }

        void type_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get a suggested artist in the background.
            e.Result = getSuggestedArtist(e.Argument.ToString());
        }

        void type_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Check if some more typing has occured whilst the call was in progress.
            if (requestOutOfDate)
            {
                // Run the call again with the new text.
                type.RunWorkerAsync(textBox1.Text);
                requestOutOfDate = false;
            }
            else
            {
                // Get the length of the currently entered text.
                int charactersEntered = textBox1.Text.Length;

                // De-register the event handler to avoid an infiite loop.
                textBox1.TextChanged -= textBox1_TextChanged;

                // Update the text to the suggestion.
                textBox1.Text = e.Result.ToString();

                // Re-register the event handler.
                textBox1.TextChanged += textBox1_TextChanged;

                // Select the text that was not entered to allow continuous typing.
                textBox1.Select(charactersEntered, textBox1.Text.Length - charactersEntered);
            }
        }

        private string getCountryCode()
        {
            string cCode = "GB";
            try
            {
                Uri u = new Uri("http://www.geoplugin.net/xml.gp");
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(u);
                myReq.KeepAlive = false;

                // Sends the HttpWebRequest and waits for the response.
                using (HttpWebResponse myHttpWebResponse = myReq.GetResponse() as HttpWebResponse)
                {
                    Stream response = myHttpWebResponse.GetResponseStream();
                    StreamReader readStream = new StreamReader(response, Encoding.UTF8);
                    string responseString = readStream.ReadToEnd();
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(responseString);
                    cCode = doc.SelectSingleNode("//geoPlugin/geoplugin_countryCode").InnerText;
                }
            }
            catch
            {
                // Ignore errors.
            }
            return cCode.ToUpper();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // get cmd line params
            string[] args = Environment.GetCommandLineArgs();
            // test for at least one parameter
            if (args.Length > 1)
            {
                // override region
                if (args[1].Length == 2)
                {
                    region = args[1].ToUpper();
                    textBox2.Text += ("\nRegion set to " + region);
                }
                // test for more than one parameter
                if (args.Length > 2)
                {
                    // override maximum artists
                    int n;
                    if (int.TryParse(args[2], out n))
                    {
                        if (n > 0)
                        {
                            MAX_ARTISTS = n;
                            TOTAL_WEB_SERVICE_CALLS = n + EXTRA_WEB_SERVICE_CALLS;
                            textBox2.Text += ("\nMaximum artists set to " + MAX_ARTISTS);
                        }
                    }
                    if (args.Length > 3)
                    {
                        // override tracks per artist
                        if (int.TryParse(args[3], out n))
                        {
                            if (n > 0)
                            {
                                TRACKS_PER_ARTIST = n;
                                textBox2.Text += ("\nTracks per artist set to " + TRACKS_PER_ARTIST);
                            }
                        }
                    }
                }
            }
        }

        private void checkVersion()
        {
            // Modified from http://themech.net/2008/05/adding-check-for-update-option-in-csharp/

            XmlTextReader reader = null;
            try
            {
                // provide the XmlTextReader with the URL of
                // our xml document
                reader = new XmlTextReader(xmlURL);
                // simply (and easily) skip the junk at the beginning
                reader.MoveToContent();
                // internal - as the XmlTextReader moves only
                // forward, we save current xml element name
                // in elementName variable. When we parse a
                // text node, we refer to elementName to check
                // what was the node name
                string elementName = "";
                // we check if the xml starts with a proper
                // "ourfancyapp" element node
                if ((reader.NodeType == XmlNodeType.Element) &&
                    (reader.Name == "SpotifyPlaylistGenerator"))
                {
                    while (reader.Read())
                    {
                        // when we find an element node,
                        // we remember its name
                        if (reader.NodeType == XmlNodeType.Element)
                            elementName = reader.Name;
                        else
                        {
                            // for text nodes...
                            if ((reader.NodeType == XmlNodeType.Text) &&
                                (reader.HasValue))
                            {
                                // we check what the name of the node was
                                switch (elementName)
                                {
                                    case "version":
                                        // thats why we keep the version info
                                        // in xxx.xxx.xxx.xxx format
                                        // the Version class does the
                                        // parsing for us
                                        newVersion = new Version(reader.Value);
                                        break;
                                    case "url":
                                        url = reader.Value;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (reader != null) reader.Close();
            }
        }

        private void compareVersion()
        {
            // get the running version
            Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            // compare the versions
            if (curVersion.CompareTo(newVersion) < 0)
            {
                // set version so as not to bug them again
                newVersion = null;
                // ask the user if they would like
                // to download the new version
                string title = "New version detected";
                string question = "Would you like to download the updated generator?";
                if (MessageBoxResult.Yes == MessageBox.Show(this, question, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes))
                {
                    // navigate the default web
                    // browser to our app
                    // homepage (the url
                    // comes from the xml content)
                    System.Diagnostics.Process.Start(url);
                }
            }
        }

    }
}
