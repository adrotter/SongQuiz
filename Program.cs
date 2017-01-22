using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMPLib;

namespace SongQuizlet
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Song Quizlet");
            while (true) {
                Console.WriteLine("Enter a number corresponding to the week of music you want to listen to, or type Q to quit:");
                string answer = Console.ReadLine();
                int week = 0;
                if (answer == "Q" || answer == "q")
                    return;
                if (!Int32.TryParse(answer, out week))
                    Console.WriteLine("Please enter a number only.");
                else if (week < 0 || week > 10)
                    Console.WriteLine("Enter a valid week.  Only 1-10.");
                else
                {
                    doMCQuiz(week);
                }
            }
        }
        static void doMCQuiz(int week)
        {
            string currentDir = Environment.CurrentDirectory;
            string weekDirectory = currentDir + "\\Week" + week.ToString();
            Random rng = new Random();
            try
            {
                Environment.CurrentDirectory = weekDirectory; //change directory to the corresponding week's directory.
            }
            catch(System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine("Did not find a Week" + week.ToString() + " folder in the current folder.  Make sure there is one there.");
                return;
            }
            int songCount = Directory.GetFiles(weekDirectory).Length;
            int questionsDone = 0;
            int Authorscore = 0;
            int Namescore = 0;
            WindowsMediaPlayer Player = new WindowsMediaPlayer();
            Player.settings.volume = 10;
            List<string> songNames;
            List<string> songAuthors;
            List<string> fileNames;
            getSongNamesAndAuthors(out songNames, out songAuthors, out fileNames, weekDirectory);
            bool[] tested = new bool[songCount];
            while (true)
            {//quiz time
                int songNum;
                while (true)
                {//get a random song that hasn't been played before
                    if (questionsDone == songCount)
                    {
                        showScore(Authorscore,Namescore,songCount);
                        Environment.CurrentDirectory = currentDir;
                        return;
                    }
                    songNum = rng.Next(0, songCount);
                    if (!tested[songNum]) {
                        tested[songNum] = true;
                        break;
                    }
                }
                //songNum has the index of the next song to be quizzed on.
                Console.WriteLine("Authors score: " + Authorscore + "/" + songCount);
                Console.WriteLine("Song name score: " + Namescore + "/" + songCount);
                Console.WriteLine("Question " + (questionsDone + 1) +"/" + songCount+"\nType anything and then enter to continue..");
                Console.ReadLine();
                Console.WriteLine("What is the name of the author of the song?  Type the full name or just the number on the left");
                int correct = generateMC(songAuthors, songNum, songCount);
                Player.settings.playCount = 100;
                Player.URL = fileNames[songNum];
                string response = Console.ReadLine();
                int numChosen;
                if (Int32.TryParse(response, out numChosen)) {
                    if (numChosen == correct)
                    {
                        Console.WriteLine("Correct.");
                        Authorscore++;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect.");
                        Console.WriteLine("The answer is :" + songAuthors[songNum]);
                    }

                }
                else
                {
                    if (songAuthors[songNum] == response)
                    {
                        Console.WriteLine("Correct.");
                        Authorscore++;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect, however if you made a spelling error type i");
                        Console.WriteLine("The answer is :" + songAuthors[songNum]);
                        if (Console.ReadKey(true).Key == ConsoleKey.I)
                            Authorscore++;
                    }
                }
                Console.WriteLine("What is the name of the song?  Type the full name or just the number on the left");
                correct = generateMC(songNames, songNum, songCount);
                response = Console.ReadLine();
                if (Int32.TryParse(response, out numChosen))
                {
                    if (numChosen == correct)
                    {
                        Console.WriteLine("Correct.");
                        Namescore++;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect.");
                        Console.WriteLine("The answer is :" + songNames[songNum]);
                    }
                }
                else
                {
                    if (songAuthors[songNum] == response)
                    {
                        Console.WriteLine("Correct.");
                        Namescore++;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect, however if you made a spelling error type i");
                        Console.WriteLine("The answer is :" + songNames[songNum]);
                        if (Console.ReadKey(true).Key == ConsoleKey.I)
                            Namescore++;
                    }
                }
                Player.controls.stop();
                questionsDone++;

            }
            
        }

        private static void showScore(int authorscore, int namescore, int songCount)
        {
            double aperc = ((double)authorscore / (double)songCount) * 100;
            double nperc = ((double)namescore / (double)songCount) * 100;

            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(aperc), 2) + "% in identifying author names");
            Console.WriteLine("You got " + Math.Round(Convert.ToDecimal(nperc), 2) + "% in identifying song names");

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
            for (int i = 0; i < 4; i++)
            {
                if (mc[i] == songAuthorsOrName[songNum])
                {
                    i++;
                    if (i >= 4)
                        break;
                }
                while(true)
                {//get a MC choice
                    whatSong = rng.Next(0, songCount);
                    bool duplicate = false;
                    if (!tested[whatSong])
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            if (songAuthorsOrName[whatSong] == mc[j]) //no duplicates allowed.
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
                Console.WriteLine(i+". "+mc[i]);
            }
            return correct;

        }

        static void getSongNamesAndAuthors(out List<string> songNames, out List<string> songAuthors, out List<string> fileNames, string dir)
        {
            string currentDir = Environment.CurrentDirectory;
            List<string> sNames = new List<string>();
            List<string> sAuthors = new List<string>();
            List<string> fNames = new List<string>();
            string[] files = Directory.GetFiles(dir);
            int songCount = Directory.GetFiles(dir).Length;
            int leftparPos = 0;
            string temp;
            for (int i = 0; i < songCount;i++)
            {
                leftparPos = files[i].IndexOf('{');
                temp = files[i].Substring(currentDir.Length + 1);
                fNames.Add(temp);
                temp = files[i].Substring(currentDir.Length + 1, leftparPos - currentDir.Length - 1);
                temp = temp.Replace('_', ' ');
                sAuthors.Add(temp);
                temp = files[i].Substring(leftparPos + 1);
                temp = temp.Replace('_', ' ');
                temp = temp.Substring(0, temp.Length - 4);
                sNames.Add(temp);
            }
            songNames = sNames;
            songAuthors = sAuthors;
            fileNames = fNames;



        }
    }
}
