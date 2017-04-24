using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using WMPLib;
using SpotifyAPI.Local; //Base Namespace
using SpotifyAPI.Local.Enums; //Enums
using SpotifyAPI.Local.Models; //Models for the JSON-responses
using SpotifyAPI.Web; //Base Namespace
using SpotifyAPI.Web.Auth; //All Authentication-related classes
using SpotifyAPI.Web.Enums; //Enums
using SpotifyAPI.Web.Models; //Models for the JSON-responses
using System.Threading;
using System.Diagnostics;

namespace SongQuizlet
{
    class Program
    {
        private static SpotifyLocalAPI _spotify;
        private static StatusResponse status;
        private static SpotifyWebAPI _spotifyWeb;
        static void Main(string[] args)
        {

            string answer;
            String userID = "";
            String playlistID = ""; 
            bool validURI = true;
            List<URIName> recentList;
            int numberChoice;
            Console.WriteLine("Welcome to Song Quizlet");
            _spotify = new SpotifyLocalAPI();
            _spotifyWeb = new SpotifyWebAPI();
            doAuthorization();
            while (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                Console.WriteLine("Spotify needs to be open for this program to work.  Type Q to quit, or anything else and enter to open spotify.");
                answer = Console.ReadLine();
                if (answer == "Q" || answer == "q")
                    return;
                else
                {
                    SpotifyLocalAPI.RunSpotify();
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                    Console.WriteLine("Type enter when spotify is running.");
                    Console.ReadLine();
                }
            }
            while (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                Console.WriteLine("Spotify WebHelper needs to be open for this program to work.  Type Q to quit, or anything else and enter to open SpotifyWebHelper.");
                answer = Console.ReadLine();
                if (answer == "Q" || answer == "q")
                    return;
                else
                {
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                    Console.WriteLine("Type enter after a bit to see if it is running.");
                    Console.ReadLine();
                }
            }

            while (!_spotify.Connect())
            {
                Console.WriteLine("Could not connect to Spotify.  Type Q to quit, or anything else and enter to check connectivity.");
                answer = Console.ReadLine();
                if (answer == "Q" || answer == "q")
                    return;
            }
            if (!File.Exists("recentURIs.txt"))
                File.Create("recentURIs.txt");
            status = _spotify.GetStatus(); //status contains infos
            while (true)
            {
                
                recentList = fillRecentList();
                validURI = true;

                while (true)
                {
                    Console.WriteLine("Enter the spotify playlist URI to begin.");
                    Console.WriteLine("U R I, to get this Open your Spotify client, go to the playlist you want to use, press the ..., then Share..., then click URI.");
                    Console.WriteLine("If you need more help, type help");
                    Console.WriteLine("To quit this program, type Q");
                    Console.WriteLine("Recent URIs, type the number to load the URI:");
                    for (int i = 0; i < recentList.Count; i++)
                    {
                        Console.WriteLine(i + 1 + ": " + recentList[i].Name);
                    }
                    answer = Console.ReadLine();
                    if (answer == "Q" || answer == "q")
                        return;
                    else if (answer.ToLower() == "help")
                    {
                        Process.Start("https://github.com/adrotter/SongQuiz/blob/master/README.md");
                    }
                    else if (Int32.TryParse(answer, out numberChoice))
                    {
                        if (numberChoice > recentList.Count)
                        {
                            Console.WriteLine("There's only " + recentList.Count + " URI entries in your recent URIs.");
                            break;
                        }
                        else if (numberChoice < 1)
                        {
                            Console.WriteLine("You can't pick a number lower than 1.");
                            break;
                        }
                        else
                        {
                            answer = recentList[numberChoice-1].URI;
                        }
                        userID = getUserID(answer);
                        if (userID != "")
                            break;
                    }
                    else
                    {
                        userID = getUserID(answer);
                        if (userID != "")
                            break;
                    }
                }
                playlistID = getPlaylistID(answer);
                FullPlaylist playlist = _spotifyWeb.GetPlaylist(userID, playlistID);

                if (playlist.Error != null)
                {
                    Console.WriteLine(playlist.Error.Message);
                    validURI = false;
                }
                else
                {
                    recentList = addToRecentList(recentList, answer, playlist.Name);
                    writeRecentList(recentList);
                }
                

                while (validURI)
                {
                    Console.WriteLine("Type MC to do multiple choice quiz or SA to do short answer quiz.  Type B to go back");
                    string quizType = Console.ReadLine();
                    if (quizType == "B" || quizType == "b")
                        break;
                    else if (quizType == "MC" || quizType == "mc")
                    {
                        doQuiz(userID, playlistID, true);
                        break;
                    }
                    else if (quizType == "SA" || quizType == "sa")
                    {
                        doQuiz(userID, playlistID, false);
                        break;
                    }
                    else
                        Console.WriteLine("Invalid input.");
                }
            }
        }

        private static void writeRecentList(List<URIName> recentList)
        {
            string[] lines = new string[recentList.Count];
            for (int i = 0; i < recentList.Count; i++)
            {
                lines[i] = recentList[i].Name + "\t" + recentList[i].URI;
            }
            File.WriteAllLines("recentURIs.txt", lines);
        }

        private static List<URIName> addToRecentList(List<URIName> recentList, string uri, string name)
        {
            int positionToInsert = recentList.Count;
            for (int i = 0; i < recentList.Count; i++)
            {
                if (recentList[i].URI == uri)
                    return recentList;
            }
            //didnt find this uri, add to end
            if (recentList.Count == 10)
            {
                positionToInsert = 0;
                recentList[0].Name = name;
                recentList[0].URI = uri;
            }
            recentList.Add(new URIName(name, uri));
            return recentList;
        }

        private static List<URIName> fillRecentList()
        {
            List<URIName> uriList = new List<URIName>();
            string[] lines = File.ReadAllLines("recentURIs.txt");
            foreach (string line in lines)
            {
                string[] splitString = line.Split('\t');
                URIName uriname = new URIName(splitString[0], splitString[1]);
                uriList.Add(uriname);
            }
            return uriList;
        }

        private static async void doAuthorization()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory("http://localhost", 8888, "a35bc60673e543638646d303f79fb2c8", Scope.UserReadPrivate, TimeSpan.FromSeconds(20));

            try
            {
                //This will open the user's browser and returns once
                //the user is authorized.
                _spotifyWeb = await webApiFactory.GetWebApi();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (_spotifyWeb == null)
                return;
        }


        static void doQuiz(string userID, string playListID, bool MC)
        {

            int questionsDone = 0;
            int Authorscore = 0;
            int Namescore = 0;
            int Albumscore = 0;
            Random rng = new Random();
            List<string> songNames;
            List<string> songAuthors;
            List<string> songAlbums;
            List<string> songURLS;
            int songCount = getSongNamesAndAuthorsWithWebAPI(out songNames, out songAuthors, out songURLS, out songAlbums, userID, playListID);
            int beatlesAlbumCount = getBeatlesAlbumCount(songAuthors);
            _spotify.Pause();
            songCount = removeDuplicates(songNames, songAuthors, songURLS);
            bool[] tested = new bool[songCount];
            while (true)
            {//quiz time
                int songNum;
                while (true)
                {//get a random song that hasn't been played before
                    if (questionsDone == songCount)
                    {
                        showScore(Authorscore, Namescore, Albumscore, songCount, beatlesAlbumCount);
                        return;
                    }
                    songNum = rng.Next(0, songCount);
                    if (!tested[songNum])
                    {
                        tested[songNum] = true;
                        break;
                    }
                }
                //songNum has the index of the next song to be quizzed on.
                Console.WriteLine("Authors score: " + Authorscore + "/" + songCount);
                Console.WriteLine("Song name score: " + Namescore + "/" + songCount);
                Console.WriteLine("Album name score: " + Albumscore + "/" + beatlesAlbumCount);
                Console.WriteLine("Question " + (questionsDone + 1) + "/" + songCount + "\nType anything and then enter to continue..");
                Console.ReadLine();
                if (MC)
                    Console.WriteLine("What is the name of the artist of the song?  Type the number on the left to answer");
                else
                    Console.WriteLine("What is the name of the artist of the song?  Type the name");
                int correct = 0;
                if (MC)
                    correct = generateMC(songAuthors, songNum, songCount);
                _spotify.PlayURL(songURLS[songNum]);
                _spotify.Pause();
                _spotify.Play();
                string response;
                int numChosen;
                while (true)
                {//name of author
                    response = Console.ReadLine();
                    if (Int32.TryParse(response, out numChosen))
                    {
                        if (numChosen == correct)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Correct.");
                            Console.ResetColor();
                            Authorscore++;
                            break;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Incorrect.");
                            Console.ResetColor();
                            Console.Write("The answer is :");
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine(songAuthors[songNum]);
                            Console.ResetColor();
                            Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                            response = Console.ReadLine();
                            break;
                        }

                    }
                    else
                    {
                        if (MC)
                            Console.WriteLine("Type the number to the left of the name to answer");
                        else if (!MC && response == "")
                            Console.WriteLine("You didn't type an answer");

                        else if (songAuthors[songNum].ToLower() == response.ToLower() && !MC)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Correct.");
                            Console.ResetColor();
                            Authorscore++;
                            break;
                        }
                        else if (!MC)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Incorrect");
                            Console.ResetColor();
                            Console.Write(", however if you made a spelling error or it was close enough type ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("F3");
                            Console.ResetColor();
                            Console.Write("The answer is :");
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine(songAuthors[songNum]);
                            Console.ResetColor();
                            if (Console.ReadKey(true).Key == ConsoleKey.F3)
                            {
                                Console.Write("Changed score to ");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Correct");
                                Console.ResetColor();
                                Authorscore++;
                                break;
                            }

                            else
                            {
                                Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                                response = Console.ReadLine();
                                break;

                            }
                        }
                    }
                }//end name of author
                if (MC)
                    Console.WriteLine("What is the name of the song?  Type the number on the left to answer");
                else
                    Console.WriteLine("What is the name of the song?  Type the name");
                if (MC)
                    correct = generateMC(songNames, songNum, songCount);
                while (true)
                {//name of song
                    response = Console.ReadLine();
                    if (Int32.TryParse(response, out numChosen))
                    {
                        if (numChosen == correct)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Correct.");
                            Console.ResetColor();
                            Namescore++;
                            break;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Incorrect.");
                            Console.ResetColor();
                            Console.Write("The answer is :");
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine(songNames[songNum]);
                            Console.ResetColor();
                            Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                            response = Console.ReadLine();
                            break;


                        }
                    }
                    else
                    {
                        if (MC)
                            Console.WriteLine("Type the number to the left of the name to answer");
                        else if (!MC && response == "")
                            Console.WriteLine("You didn't type an answer");
                        else if (songNames[songNum].ToLower() == response.ToLower() && !MC)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Correct.");
                            Console.ResetColor();
                            Namescore++;
                            break;
                        }
                        else if (!MC)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Incorrect");
                            Console.ResetColor();
                            Console.Write(", however if you made a spelling error or it was close enough type ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("F3");
                            Console.ResetColor();
                            Console.Write("The answer is :");
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine(songNames[songNum]);
                            Console.ResetColor();
                            if (Console.ReadKey(true).Key == ConsoleKey.F3)
                            {
                                Console.Write("Changed score to ");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Correct");
                                Console.ResetColor();
                                Namescore++;
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                                response = Console.ReadLine();
                                break;

                            }
                        }
                    }
                }//end name of song
                if (songAuthors[songNum] == "The Beatles")
                {
                    if (MC)
                        Console.WriteLine("This is a The Beatles song, What is the name of the album?  Type the number on the left to answer");
                    else
                        Console.WriteLine("This is a The Beatles song, What is the name of the album?  Type the name");
                    if (MC)
                        correct = generateMC(songAlbums, songNum, songCount);
                    while (true)
                    {//Album of song
                        response = Console.ReadLine();
                        if (Int32.TryParse(response, out numChosen))
                        {
                            if (numChosen == correct)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Correct.");
                                Console.ResetColor();
                                Albumscore++;
                                break;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Incorrect.");
                                Console.ResetColor();
                                Console.Write("The answer is :");
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine(songAlbums[songNum]);
                                Console.ResetColor();
                                Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                                response = Console.ReadLine();
                                break;


                            }
                        }
                        else
                        {
                            if (MC)
                                Console.WriteLine("Type the number to the left of the name to answer");
                            else if (!MC && response == "")
                                Console.WriteLine("You didn't type an answer");
                            else if (songAlbums[songNum].ToLower() == response.ToLower() && !MC)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Correct.");
                                Console.ResetColor();
                                Albumscore++;
                                break;
                            }
                            else if (!MC)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("Incorrect");
                                Console.ResetColor();
                                Console.Write(", however if you made a spelling error or it was close enough type ");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("F3");
                                Console.ResetColor();
                                Console.Write("The answer is :");
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine(songAlbums[songNum]);
                                Console.ResetColor();
                                if (Console.ReadKey(true).Key == ConsoleKey.F3)
                                {
                                    Console.Write("Changed score to ");
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Correct");
                                    Console.ResetColor();
                                    Albumscore++;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                                    response = Console.ReadLine();
                                    break;

                                }
                            }
                        }
                    }//end while
                }//end if
                questionsDone++;

            }

        }

        private static int getBeatlesAlbumCount(List<string> songAuthors)
        {
            int count = 0;
            for (int i = 0; i < songAuthors.Count;i++)
            {
                if (songAuthors[i] == "The Beatles")
                    count++;
            }
            return count;
        }

        private static int removeDuplicates(List<string> songNames, List<string> songAuthors, List<string> songURLS)
        {
            int songCount = songNames.Count;
            for (int i = 0; i < songCount; i++)
            {
                for (int j = 0; j < songCount; j++)
                {
                    if (i != j && songNames[i] == songNames[j] && songAuthors[i] == songAuthors[j] && songURLS[i] == songURLS[j] && songNames[i] != "remove" && songNames[j] != "remove")
                    {
                        songNames[j] = "remove";
                    }
                }
            }
            for (int i = 0; i < songNames.Count; i++)
            {
                if (songNames[i] == "remove")
                {
                    songNames.RemoveAt(i);
                    songAuthors.RemoveAt(i);
                    songURLS.RemoveAt(i);
                    i = 0;
                    songCount--;
                }
            }
            return songCount;
        }

        private static void showScore(int authorscore, int namescore, int albumscore, int songCount, int albumCount)
        {
            double aperc = ((double)authorscore / (double)songCount) * 100;
            double nperc = ((double)namescore / (double)songCount) * 100;
            double alperc = ((double)albumscore / (double)albumCount) * 100;

            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(aperc), 2) + "% in identifying author names");
            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(nperc), 2) + "% in identifying song names");
            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(alperc), 2) + "% in identifying Album names");
            _spotify.Pause();

        }

        static int generateMC(List<string> songAuthorsOrName, int songNum, int songCount)
        {
            Random rng = new Random();
            int whatSong;
            bool[] tested = new bool[songCount];
            string[] mc = new string[4];
            int correct = rng.Next(0, 4);
            mc[correct] = songAuthorsOrName[songNum];
            tested[songNum] = true;
            if (!checkForSufficientOptions(songAuthorsOrName, songNum, songCount))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i == correct)
                        Console.WriteLine(i + 1 + ". " + mc[i]);
                }
                return correct + 1;
            }
            for (int i = 0; i < 4; i++)
            {
                if (mc[i] == songAuthorsOrName[songNum])
                {
                    i++;
                    if (i >= 4)
                        break;
                }
                while (true)
                {//get a MC choice
                    whatSong = rng.Next(0, songCount);
                    bool duplicate = false;
                    if (!tested[whatSong])
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (songAuthorsOrName[whatSong] == mc[j] || CalculateSimilarity(songAuthorsOrName[whatSong], mc[j]) > .45) //no duplicates allowed.
                                duplicate = true;
                        }
                        if (!duplicate)
                        {
                            tested[whatSong] = true;
                            break;
                        }
                    }
                }
                mc[i] = songAuthorsOrName[whatSong];
            }
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine(i + 1 + ". " + mc[i]);
            }
            return correct + 1;

        }

        private static bool checkForSufficientOptions(List<string> songAuthorsOrName, int songNum, int songCount)
        {
            int avaliableChoices = songCount;
            for (int i = 0; i < songCount; i++)
            {
                if (songAuthorsOrName[i] == songAuthorsOrName[songNum] || CalculateSimilarity(songAuthorsOrName[i], songAuthorsOrName[songNum]) > .45)
                    avaliableChoices--;
            }
            if (avaliableChoices < 3)
                return false;
            else
                return true;
        }

        static int getSongNamesAndAuthors(out List<string> songNames, out List<string> songAuthors, out List<string> songURLs, string playListURL)
        {
            int songCount = 0;
            int numberOfSongsCycled = 0;
            List<string> sNames = new List<string>();
            List<string> sAuthors = new List<string>();
            List<string> sURLs = new List<string>();
            _spotify.PlayURL(playListURL);
            Console.WriteLine("Fetching song info from your Spotify application..");
            _spotify.GetStatus().Shuffle = false;
            Track firstTrack = _spotify.GetStatus().Track;
            while (firstTrack.IsAd())
                firstTrack = _spotify.GetStatus().Track;
            sNames.Add(firstTrack.TrackResource.Name);
            sAuthors.Add(firstTrack.ArtistResource.Name);
            sURLs.Add(firstTrack.TrackResource.Uri + "%230:30");
            songCount++;
            _spotify.Skip();
            Thread.Sleep(200);
            Track currentTrack = _spotify.GetStatus().Track;
            numberOfSongsCycled++;
            while (currentTrack.TrackResource.Name != firstTrack.TrackResource.Name || currentTrack.ArtistResource.Name != firstTrack.ArtistResource.Name)
            {
                Console.Write(numberOfSongsCycled + " ");
                currentTrack = _spotify.GetStatus().Track;
                Thread.Sleep(200);
                while (currentTrack.IsAd())
                    ;
                songCount++;
                numberOfSongsCycled++;
                sNames.Add(currentTrack.TrackResource.Name);
                sAuthors.Add(currentTrack.ArtistResource.Name);
                sURLs.Add(currentTrack.TrackResource.Uri + "%230:30");
                _spotify.Skip();
                //if (numberOfSongsCycled == 100)
                //{
                //    Console.WriteLine("Spotify needs a little break.. Giving it a 10 second break.");
                //    numberOfSongsCycled = 0;
                //    Thread.Sleep(10000);
                //}
            }
            Console.WriteLine("Finished fetching");
            Thread.Sleep(100);
            _spotify.Pause();
            songNames = sNames;
            songAuthors = sAuthors;
            songURLs = sURLs;
            return songCount;
        }

        static int getSongNamesAndAuthorsWithWebAPI(out List<string> songNames, out List<string> songAuthors, out List<string> songURLs, out List<string> songAlbums, string userID, string playlistID)
        {
            int songCount = 0;
            List<string> sNames = new List<string>();
            List<string> sAuthors = new List<string>();
            List<string> sURLs = new List<string>();
            List<string> sAlbums = new List<string>();
            Console.WriteLine("Fetching song info from your Spotify application..");
            FullPlaylist playlist = _spotifyWeb.GetPlaylist(userID, playlistID);
            for (int i = 0; i < playlist.Tracks.Items.Count; i++)
            {
                sNames.Add(playlist.Tracks.Items[i].Track.Name);
                string randomTime = getRandomTimeFromTrack(playlist.Tracks.Items[i].Track);
                sURLs.Add(playlist.Tracks.Items[i].Track.Uri + "%23"+randomTime);
                sAuthors.Add(playlist.Tracks.Items[i].Track.Artists[0].Name);
                sAlbums.Add(playlist.Tracks.Items[i].Track.Album.Name);
                songCount++;
            }
            if (playlist.Tracks.Total > 100)
            {
                for (int i = 100; i < playlist.Tracks.Total; i += 100)
                {
                    Paging<PlaylistTrack> extendedPlaylist = _spotifyWeb.GetPlaylistTracks(userID, playlistID, "", 100, i, "");
                    for (int j = 0; j < extendedPlaylist.Items.Count; j++)
                    {
                        sNames.Add(extendedPlaylist.Items[j].Track.Name);
                        string randomTime = getRandomTimeFromTrack(playlist.Tracks.Items[i].Track);
                        sURLs.Add(extendedPlaylist.Items[j].Track.Uri + "%23"+randomTime);
                        sAuthors.Add(extendedPlaylist.Items[j].Track.Artists[0].Name);
                        sAlbums.Add(extendedPlaylist.Items[j].Track.Album.Name);
                        songCount++;
                    }
                }
            }
            Console.WriteLine("Finished fetching");
            songNames = sNames;
            songAuthors = sAuthors;
            songURLs = sURLs;
            songAlbums = sAlbums;
            return songCount;
        }

        private static string getRandomTimeFromTrack(FullTrack track)
        {
            int trackHalfDurationSeconds = track.DurationMs / 2000;
            Random rng = new Random();
            int starting = rng.Next(0, trackHalfDurationSeconds);
            double hours = (double) starting / 3600.0;
            double minutes = hours * 60;
            double minuteFraction = minutes - Math.Truncate(minutes);
            string sMinutes = ((int)minutes).ToString();
            minuteFraction *= 60;
            string sSeconds = ((int)minuteFraction).ToString();
            if ((int)minuteFraction < 10)
                sSeconds = "0" + sSeconds;
            return sMinutes + ":" + sSeconds;
            

        }

        private static string getUserID(string url)
        {
            string[] userID = url.Split(':');
            try
            {
                return userID[2].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Seems like you did not supply a correct URI.  This is not a URL, but a URI.");
                Console.WriteLine("to get this, press the ..., then Share..., then click URI.");
            }
            return "";
        }

        private static string getPlaylistID(string url)
        {
            string[] userID = url.Split(':');

            return userID[4].ToString();
        }

        //retrieved from https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx
        /// <summary>
        /// Returns the number of steps required to transform the source string
        /// into the target string.
        /// </summary>
        private static int ComputeLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    // Step 3
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 4
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }

        //retrieved from https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx
        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        private static double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }
    }

    public class URIName
    {
        public string Name;
        public string URI;
        public URIName(string name, string uri)
        {
            Name = name;
            URI = uri;
        }
    }
}