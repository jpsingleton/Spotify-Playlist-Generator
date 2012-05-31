using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Spotify
{
  /// <summary>
  /// Class containing the results of a Spotify search
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class SearchResults<T> where T : SpotifyItem, new()
  {
    internal void Load(XmlDocument doc, string xpath)
    {
      // parse the response
      List<T> itemList = new List<T>();
      XmlNodeList items = doc.SelectNodes(xpath, NamespaceManager.Instance);
      foreach (XmlElement item in items)
      {
        T spotifyitem = new T();
        spotifyitem.Load(item);
        itemList.Add(spotifyitem);
      }

      searchResultsPage = itemList.ToArray();

      // total number of results
      //totalNumberOfResults = int.Parse(doc.SelectSingleNode("//opensearch:totalResults", NamespaceManager.Instance).InnerText);
      // results per page
      //resultsPerPage = int.Parse(doc.SelectSingleNode("//opensearch:itemsPerPage", NamespaceManager.Instance).InnerText);
      //numberOfPages = (int)Math.Ceiling((double)totalNumberOfResults / (double)resultsPerPage);

      //currentPage = int.Parse((doc.SelectSingleNode("//opensearch:Query", NamespaceManager.Instance) as XmlElement).GetAttribute("startPage"));
    }

    private T[] searchResultsPage;
    /// <summary>
    /// Gets the current page of search results page.
    /// </summary>
    public T[] SearchResultsPage
    {
      get
      {
        return searchResultsPage;
      }
    }

    private int totalNumberOfResults;
    /// <summary>
    /// Gets the total number of results available.
    /// </summary>
    public int TotalNumberOfResults
    {
      get
      {
        return totalNumberOfResults;
      }
    }

    private int resultsPerPage;
    /// <summary>
    /// Gets the number of results per page.
    /// </summary>
    public int ResultsPerPage
    {
      get { return resultsPerPage; }
    }

    private int numberOfPages;
    /// <summary>
    /// Gets the total number of pages of results available.
    /// </summary>
    public int NumberOfPages
    {
      get { return numberOfPages; }
    }

    private int currentPage;

    /// <summary>
    /// Gets the current page of the results.
    /// </summary>
    public int CurrentPage
    {
      get { return currentPage; }
    }

  }
}
