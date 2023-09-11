using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Online_Bingo_Game_Server
{
    class Program
    {
        static Thread approver;
        static TcpListener server;
        static TcpClient currentClient;
        static Random rng = new Random();
        static int wordIndex = 0;
        static int winOpt;
        static string input;
        static string currentWord = ""; //Current randomized word
        // int podiumMembers = 0; //Counter for winners. If there are more than 3 bingos, the game will stop
        static List<Player> winners = new List<Player>();
        private static List<string> wordsStock = new List<string>();
        private static List<Player> players = new List<Player>();
        private static bool listen = true;
        private static int port;
        static void Main(string[] args)
        {
            Console.Title = "Online Bingo Game Server";
            //Input section for the words stock
            getWords();
            getWinMethod();
            startServer();
        }
        private static void startServer() 
        {
            server = new TcpListener(port);
            server.Start(); //Starts the server
            Console.WriteLine("Server has started!");

            approver = new Thread(waitForConnections);
            approver.Start();

            while (players.Count < 3)
            {
                //Wait for 3 connections minimum
                Thread.Sleep(1000);
            }
            //Console.WriteLine("To start the game, enter 'startgame'.");
            //while (!Console.ReadLine().Equals("startgame"))
            //{
            //    Console.WriteLine("");
            //    Console.WriteLine("To start the game, enter 'startgame'.");
            //}
            for (int i = 5; i > 0; i--)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Game starts in " + i);
            }
            server.Stop();
            try
                {
                    approver.Abort();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                }
            //Starts the game
            handleGame();

        }
        private static void waitForConnections() 
        {
            string name = string.Empty;
            while (listen)
            {
                    currentClient = server.AcceptTcpClient(); //Accept the current client
                    byte[] stream = new byte[100025];
                    NetworkStream clientS = currentClient.GetStream();
                    clientS.Read(stream, 0, currentClient.ReceiveBufferSize);
                    name = Encoding.ASCII.GetString(stream);
                    name = name.Substring(0, name.IndexOf("$"));
                    Console.WriteLine(name + " Has Connected.");
                    Player currentPlayer = new Player(currentClient, name);
                    currentPlayer.sendData(generateBoard());
                    Thread.Sleep(50);
                    currentPlayer.sendData("WINOPT:" + winOpt.ToString()); //Sends the winning method to the player
                    Thread.Sleep(50); //Avoids mixture of messages
                    
                    broadcast("MESSAGE:" + currentPlayer.Name + " Has joined the game!", true);
                    players.Add(currentPlayer);
                
            }
        }
        private static void getWords()
        {
            int extra = 15; //Extra words for the stock
            Console.WriteLine("Welcome to the Online Bingo Game Server.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("All rights reserved to Tal Frumkin");
            Console.WriteLine("For contact - tal.frumkin@gmail.com");

            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Choose game number and enter it");
            string temp = Console.ReadLine();
            int t = 0;
            while (!(int.TryParse(temp, out t) && (int.Parse(temp) < 1000 && int.Parse(temp) > 0)))
            {
                Console.WriteLine();
                Console.WriteLine("Enter valid number between 0-1000");
                temp = Console.ReadLine();
            }
            port = int.Parse(temp);
            Console.WriteLine();
            Console.WriteLine("Enter at least 15 words for the stock. Maximum 35 words.");
            for(int i = 1; i <= 15; i++)
            {
                wordsStock.Add(Console.ReadLine().TrimStart().TrimEnd()); //Adding to word to the stock
            }
            Console.WriteLine();
            Console.WriteLine("You're able to stop entering extra words. If you wish to, enter 'stopcode'.");
            Console.WriteLine("Else, continue (maximum 35 words)");
            input = Console.ReadLine();
            while (!input.Equals("stopcode") && extra < 36)
            {
                wordsStock.Add(input.TrimStart().TrimEnd());
                extra++;
                Console.WriteLine();
                Console.WriteLine("You're able to stop entering extra words. If you wish to, enter 'stopcode'.");
                Console.WriteLine("Else, continue (maximum 35 words");
                input = Console.ReadLine();
            }

            //Randomize thw word stock
            shuffle(wordsStock);
        }
        private static void getWinMethod()
        {
            Console.WriteLine();
            Console.WriteLine("There are two optional ways of winning");
            Console.WriteLine("1. 3 in a row");
            Console.WriteLine("2. Full card");
            Console.WriteLine();
            Console.WriteLine("Choose one of the options above");
            input = Console.ReadLine();
            while (!input.Equals("1") && !input.Equals("2"))
            {
                Console.WriteLine("Enter a valid option");
                input = Console.ReadLine();
            }
            winOpt = int.Parse(input);
            Console.WriteLine("");
            Console.WriteLine("Wait for at least 3 players");
        }
        private static string generateBoard()
        {
            List<string> temp = new List<string>();
            string data = "WORDLIST:";

            for (int i = 0; i < wordsStock.Count; i++) //Copy the word stock to the temp list
                temp.Add(wordsStock[i]);

            //Shuffles the temp list
            shuffle(temp);

            for (int i = temp.Count - 1, constCount = temp.Count; i >= 9; i--) //Removes all the element after index 8 (9 words for a single bingo card0
                temp.RemoveAt(i);

            for (int i = 0; i < temp.Count; i++) //Attaching all the words to one string
                data += temp[i] + "|";
            data += "$";

            return data;
        }
        private static void shuffle(List<string> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        private static void handleGame() 
        {
            
            broadcast("MESSAGE:Game has started.", true);
            Thread.Sleep(1000);
            while (winners.Count < 3)
            {
                if (winners.Count + players.Count < 3)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The Game stops due to lack of players");
                    Thread.Sleep(3000);
                    Environment.Exit(0);
                }
                currentWord = wordsStock[wordIndex];
                Console.WriteLine("The next word is - " + currentWord);
                wordIndex++;
                broadcast("WORD:" + currentWord, true);
                Thread.Sleep(6000); //Six seconds to check
            }

            //Ends the game
            endGame();
        }

        private static void endGame()
        {
            StringBuilder score = new StringBuilder();
            score.AppendLine("The standings are:");

            for (int i = 0; i < winners.Count; i++)
                score.AppendLine(sum(i, 1) + ". " + winners[i].Name);
            score.AppendLine("The game will be closed in 10 seconds");
            Console.WriteLine(score);
            broadcast("MESSAGE:" + score.ToString(), false);

            Thread.Sleep(10000);
            Environment.Exit(0);
        }
        static int sum(int a, int b)
        { return a + b; }
        /// <summary>
        /// Broadcasts a message to all the players
        /// </summary>
        private static void broadcast(string data, bool forP)
        {
            foreach (Player player in players)
                if (player != null)
                    player.sendData(data);
            if(!forP)
                foreach (Player player in winners)
                if(player != null)
                    player.sendData(data);
        }
        public static void stateBingo(Player player) 
        {
            string message = "MESSAGE:" + player.Name + " has declared Bingo!";
            players.Remove(player);
            winners.Add(player);
            Console.WriteLine(player.Name + " has declared Bingo!");
            broadcast(message, false);
        }  
        public static void remove(Player p)
        {
            if (players.Contains(p)) //Detects if the player is in the non winners list
                players.Remove(p);
            else
                winners.Remove(p);
        }

        internal static void report(Player p)
        {
            Console.WriteLine(p.Name + " has disconnected");
            broadcast("MESSAGE:" + p.Name + " has left",true);
        }
    }
}
