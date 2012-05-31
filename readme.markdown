# Spotify Playlist Generator

I’ve made a Spotify Playlist Generator that uses Last.fm to find songs by similar artists.

It’s Windows only (WPF) and it requires the .NET framework v3.5 or greater. The source code is available under the GPLv3 if you would like to build on it.

## Instructions:
1. Enter an artist’s name in the artist box.
2. Click the button and wait until completion.
3. Select all of the links then drag into a Spotify playlist.
4. Listen and enjoy!

## Advanced: Select a few tracks and paste the Spotify track links into the large box. Leave the artist box empty and then click for a compilation.

## Super Advanced: The generator will try to auto detect your region but you can override it and other settings. Create a shortcut to the executable, go to the target in the properties and add the 2 character ISO country code after a space: 
* e.g. PlaylistGenerator.exe GB. 
You can also modify the maximum number of artists: 
* e.g. PlaylistGenerator.exe GB 49. 
And even the maximum number of tracks per artist: 
*e.g. PlaylistGenerator.exe GB 49 3. 
If you modify these then they are displayed in the main window when you first run it.

Thanks to http://www.doogal.co.uk/spotify.php and http://code.google.com/p/lastfm-sharp/

“This product uses a SPOTIFY API but is not endorsed, certified or otherwise approved in any way by Spotify. Spotify is the registered trade mark of the Spotify Group.”
