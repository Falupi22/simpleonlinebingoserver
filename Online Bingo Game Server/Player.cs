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
    class Player
    {
        Thread connection;
        TcpClient client;
        string name;
        public Player(TcpClient client, string name)
        {
            this.client = client;
            this.name = name;
            connection = new Thread(receiveData);
            connection.Start();
        }
        private void receiveData() //recieves data from the pl
        {
            try
            {
                string data = "";
                while (true)
                {
                    byte[] stream = new byte[100025];
                    NetworkStream clientS = client.GetStream();
                    clientS.Read(stream, 0, client.ReceiveBufferSize);
                    data = Encoding.ASCII.GetString(stream);
                    data = data.Substring(0, data.IndexOf("$"));
                    if (data.Equals("BINGO"))
                    {
                        Program.stateBingo(this);
                    }
                }
            }
            catch 
            {
                client.Close();
                
                Program.remove(this);
                Program.report(this);
                connection.Abort();
            }
        }
        public void sendData(string data) //sends data to the player
        {
            try
            {
                NetworkStream clientS = client.GetStream();
                byte[] stream;
                stream = Encoding.ASCII.GetBytes(data + "$");
                clientS.Write(stream, 0, stream.Length);
                clientS.Flush();
            }
            catch
            {
                client.Close();
                Program.remove(this);
                Program.report(this);
                connection.Abort();
            }

        }
        public string Name 
        { 
            get {return name;} 
            set {name = value;} 
        }
    }
}
