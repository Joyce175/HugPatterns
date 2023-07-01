using System;
using System.Net;
using System.Net.Sockets;
using Bhaptics.Tact;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BHapticsSkinServer
{
    class BHapticsSkinServer
    {
        private static IHapticPlayer _player;
        private static byte[] _motorsMapping = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };//, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19};//, 20, 21, 22, 23, 24, 25, 26, 27, 28 };
        private static byte _durationFrame = 30;
        private static bool _isReceivingMotorMapping = false;


        [Obsolete]
        public static int Main(String[] args)
        {
            _player = new HapticPlayer("BHapticsSkinServerID", "BHapticsSkinServer");

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Record data to a file");
                Console.WriteLine("2. Play back recorded data");
                Console.WriteLine("3. Live interaction");
                Console.Write("Enter your choice: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("Recording data to file...");
                        // do something for Option 1
                        RecordDataToFile();
                        break;
                    case "2":
                        Console.WriteLine("Playing back recorded data...");
                        // do something for Option 2
                        PlayBackRecordedData();
                        break;
                    case "3":
                        Console.WriteLine("Live interaction...");
                        // do something for Option 3
                        StartListening();
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please enter a valid option.");
                        continue;
                }

                Console.WriteLine("Press any key to return to the option menu.");
                Console.ReadKey();
            }

            return 0;
        }
        static void RecordDataToFile()
        {
            string fileName = "data_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            Console.WriteLine("Recording data to file: " + fileName);

            using (StreamWriter writer = new StreamWriter(fileName))
            {
                DateTime endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    byte[] motorBytes = new byte[] { 1, 2, 3, 4 };
                    int delay = 1000;
                    string line = string.Join(",", motorBytes) + "," + delay;
                    writer.WriteLine(line);
                    System.Threading.Thread.Sleep(delay);
                }
            }

            Console.WriteLine("Data recording complete.");
        }

        static void PlayBackRecordedData()
        {
            Console.WriteLine("Select a file to play back:");

            string[] files = Directory.GetFiles(".", "*.txt").OrderByDescending(f => new FileInfo(f).CreationTime).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
            }

            int selection;
            do
            {
                Console.Write("Enter the number of the file to play back: ");
            } while (!int.TryParse(Console.ReadLine(), out selection) || selection < 1 || selection > files.Length);

            string fileName = files[selection - 1];

            Console.WriteLine($"Playing back recorded data from file: {fileName}");

            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');
                    byte[] motorBytes = new byte[values.Length - 1];
                    for (int i = 0; i < motorBytes.Length; i++)
                    {
                        byte.TryParse(values[i], out motorBytes[i]);
                    }
                    int delay;
                    int.TryParse(values[values.Length - 1], out delay);
                    Console.WriteLine("Data: " + string.Join(", ", motorBytes) + ", Delay: " + delay);
                    _player.Submit("_", PositionType.VestBack, motorBytes, delay);
                    System.Threading.Thread.Sleep(delay);
                }

            }

            Console.WriteLine("Data playback complete.");
        }

        public static void StartListening()
        {
            byte[] bytes = new Byte[1024]; // Data buffer for incoming data.  

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 51470);
            Socket listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10); // Listen for incoming connections

                Console.WriteLine("Waiting for a connection on port 51470 ...");

                while (true)
                {
                    Socket handler = listener.Accept(); // Waiting for an incoming connection
                    Console.WriteLine();
                    Console.WriteLine("Client application connected");

                    List<byte> buffer = new List<byte>();
                    try
                    {
                        while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0))
                        {
                            string fileName = "data_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                            Console.WriteLine("Recording data to file: " + fileName);

                            using (StreamWriter writer = new StreamWriter(fileName))
                            {
                                DateTime endTime = DateTime.Now.AddSeconds(20);
                                while (DateTime.Now < endTime)
                                {
                                    int delay = 130;
                                    int bytesRec = handler.Receive(bytes);
                                    for (int i = 0; i < bytesRec; i++)
                                    {
                                        if (bytes[i] == 0xFF)
                                        {
                                            if (!_isReceivingMotorMapping)
                                            {

                                                if (buffer.Count == _motorsMapping.Length)
                                                    BufferUpdate(buffer);
                                                    string line = string.Join(",", buffer) + "," + delay;
                                                    writer.WriteLine(line);

                                                if (buffer.Count == 0)
                                                    _isReceivingMotorMapping = true;
                                            }
                                            else if (buffer.Count == 0)
                                                _isReceivingMotorMapping = true;
                                            else
                                                ParseMotorsMapping(buffer);

                                            buffer = new List<byte>();
                                        }
                                        else
                                            buffer.Add(bytes[i]);
                                    }



                                }
                            }

                            Console.WriteLine("Data recording complete.");






                            
                        }
                        Console.WriteLine("Client application disconnected");
                    }
                    catch (Exception e) { Console.WriteLine(e.ToString()); }
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public static void BufferUpdate(List<byte> buffer)
        {

            List<DotPoint> points = new List<DotPoint>();

            for (int i = 0; i < buffer.Count; i++)
            {
                points.Add(new DotPoint(_motorsMapping[i], (int)(buffer[i] / 2.54f)));
                //Debug.WriteLine("motor:" + _motorsMapping[i] + " intensity:" + (int)(buffer[i] / 2.54f));
            }

            _player.Submit("_", PositionType.VestBack, points, _durationFrame);
        }

        public static void ParseMotorsMapping(List<byte> buffer)
        {
            _durationFrame = buffer[0];
            buffer.RemoveAt(0);
            _motorsMapping = buffer.ToArray();
            Console.Write("Duration of a frame : {0} ms\nNew motors mapping : ", _durationFrame);
            foreach (var b in _motorsMapping)
            {
                Console.Write("{0}, ", b);
            }


            Console.WriteLine();
            _isReceivingMotorMapping = false;
        }
    }
}