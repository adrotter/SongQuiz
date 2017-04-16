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
            String userID;
            String playlistID;
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

            status = _spotify.GetStatus(); //status contains infos
            while (true)
            {
                while (true)
                {
                    Console.WriteLine("Enter the spotify playlist URI to begin (U R I, to get this, press the ..., then Share..., then click URI.");
                    Console.WriteLine("To quit this program, type Q");
                    answer = Console.ReadLine();
                    if (answer == "Q" || answer == "q")
                        return;
                    userID = getUserID(answer);
                    if (userID != "")
                        break;
                    
                }
                playlistID = getPlaylistID(answer);
                while (true)
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

        private static async void doAuthorization()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory("http://localhost", 8888, "a35bc60673e543638646d303f79fb2c8", Scope.UserReadPrivate,TimeSpan.FromSeconds(20));

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
            Random rng = new Random();
            WindowsMediaPlayer Player = new WindowsMediaPlayer();
            Player.settings.volume = 10;
            List<string> songNames;
            List<string> songAuthors;
            List<string> songURLS;
            int songCount= getSongNamesAndAuthorsWithWebAPI(out songNames, out songAuthors, out songURLS, userID, playListID);
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
                        showScore(Authorscore, Namescore, songCount);
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
                {
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
                            Console.WriteLine("The answer is :" + songAuthors[songNum]);
                            Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                            response = Console.ReadLine();
                            break;
                        }

                    }
                    else
                    {
                        if (MC)
                            Console.WriteLine("Type the number to the left of the name to answer");
                        if (songAuthors[songNum] == response && !MC)
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
                            Console.WriteLine(", however if you made a spelling error or it was close enough type F3");
                            Console.WriteLine("The answer is :" + songAuthors[songNum]);
                            if (Console.ReadKey(true).Key == ConsoleKey.F3)
                            {
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
                }
                if (MC)
                    Console.WriteLine("What is the name of the song?  Type the number on the left to answer");
                else
                    Console.WriteLine("What is the name of the song?  Type the name");
                if (MC)
                    correct = generateMC(songNames, songNum, songCount);
                while (true)
                {
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
                            Console.WriteLine("The answer is :" + songNames[songNum]);
                            Console.WriteLine("Type the answer the way it is spelled here exactly to continue:");
                            response = Console.ReadLine();
                            break;
                            
                            
                        }
                    }
                    else
                    {
                        if (MC)
                            Console.WriteLine("Type the number to the left of the name to answer");
                        if (songNames[songNum] == response && !MC)
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
                            Console.WriteLine(", however if you made a spelling error or it was close enough type F3");
                            Console.WriteLine("The answer is :" + songNames[songNum]);
                            if (Console.ReadKey(true).Key == ConsoleKey.F3)
                            {
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
                }
                Player.controls.stop();
                questionsDone++;

            }

        }

        private static int removeDuplicates(List<string> songNames, List<string> songAuthors, List<string> songURLS)
        {
            int songCount = songNames.Count;
            for (int i = 0; i < songCount; i++)
            {
                for (int j = 0; j< songCount; j++ )
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

        private static void showScore(int authorscore, int namescore, int songCount)
        {
            double aperc = ((double)authorscore / (double)songCount) * 100;
            double nperc = ((double)namescore / (double)songCount) * 100;

            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(aperc), 2) + "% in identifying author names");
            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(nperc), 2) + "% in identifying song names");
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
            if (!checkForSufficientOptions(songAuthorsOrName, songNum,songCount))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i == correct)
                        Console.WriteLine(i+1 + ". " + mc[i]);
                }
                return correct+1;
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
                            if (songAuthorsOrName[whatSong] == mc[j] || CalculateSimilarity(songAuthorsOrName[whatSong], mc[j]) > .3) //no duplicates allowed.
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
                Console.WriteLine(i+1 + ". " + mc[i]);
            }
            return correct+1;

        }

        private static bool checkForSufficientOptions(List<string> songAuthorsOrName, int songNum, int songCount)
        {
            int avaliableChoices = songCount;
            for (int i = 0; i < songCount; i++)
            {
                if (songAuthorsOrName[i] == songAuthorsOrName[songNum])
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
            sURLs.Add(firstTrack.TrackResource.Uri+"%230:30");
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

        static int getSongNamesAndAuthorsWithWebAPI(out List<string> songNames, out List<string> songAuthors, out List<string> songURLs, string userID, string playlistID)
        {
            int songCount = 0;
            List<string> sNames = new List<string>();
            List<string> sAuthors = new List<string>();
            List<string> sURLs = new List<string>();
            Console.WriteLine("Fetching song info from your Spotify application..");
            FullPlaylist playlist = _spotifyWeb.GetPlaylist(userID, playlistID);
            for (int i = 0; i < playlist.Tracks.Items.Count; i++)
            {
                sNames.Add(playlist.Tracks.Items[i].Track.Name);
                sURLs.Add(playlist.Tracks.Items[i].Track.Uri + "%230:30");
                sAuthors.Add(playlist.Tracks.Items[i].Track.Artists[0].Name);
                songCount++;
            }
            if (playlist.Tracks.Total > 100)
            {
                for (int i = 100; i < playlist.Tracks.Total;i += 100)
                {
                    Paging<PlaylistTrack> extendedPlaylist = _spotifyWeb.GetPlaylistTracks(userID, playlistID, "", 100, i, "");
                    for (int j = 0; j < extendedPlaylist.Items.Count; j++)
                    {
                        sNames.Add(extendedPlaylist.Items[j].Track.Name);
                        sURLs.Add(extendedPlaylist.Items[j].Track.Uri + "%230:30");
                        sAuthors.Add(extendedPlaylist.Items[j].Track.Artists[0].Name);
                        songCount++;
                    }
                }
            }
            Console.WriteLine("Finished fetching");
            songNames = sNames;
            songAuthors = sAuthors;
            songURLs = sURLs;
            return songCount;
        }
        private static string getUserID(string url)
        {
            string[] userID = url.Split(':');
            try
            {
                return userID[2].ToString();
            }
            catch(Exception e)
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
}