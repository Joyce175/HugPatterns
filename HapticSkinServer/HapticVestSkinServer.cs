using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;

namespace HapticVestSkinServer
{
    class HapticVestSkinServer
    {
        private static byte[] _motorsMapping = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        private static byte _durationFrame = 30;
        private static bool _isReceivingMotorMapping = false;

        static SerialPort _serialPort;


        [Obsolete]
        public static int Main(String[] args)
        {
            _serialPort = new SerialPort();

            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter COM port value : ");
            _serialPort.PortName = Console.ReadLine();
            _serialPort.BaudRate = 115200;
            _serialPort.Open();

            Thread readThread = new Thread(SerialRead);
            readThread.Start();

            StartListening();

            return 0;
        }


        public static void StartListening()
        {
            byte[] bytes = new Byte[1024]; // Data buffer for incoming data.  

            // SkinAnalyser is configured to communicate through port 51470
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 51470);
            Socket listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10); // Listen for incoming connections

                Console.WriteLine("Waiting for a connection on port 51470 ...");

                while (true) // Allows the client application to connect and disconnect as desired
                {
                    Socket handler = listener.Accept(); // Waiting for an incoming connection
                    Console.WriteLine();
                    Console.WriteLine("Client application connected");

                    List<byte> buffer = new List<byte>();
                    try
                    {
                        while (!(handler.Poll(1, SelectMode.SelectRead) && handler.Available == 0))
                        {
                            int bytesRec = handler.Receive(bytes);
                            for (int i = 0; i < bytesRec; i++)
                            {
                                if (bytes[i] == 0xFF) // End of frame byte
                                {
                                    if (!_isReceivingMotorMapping)
                                    {
                                        if (buffer.Count == _motorsMapping.Length)
                                            BufferUpdate(buffer);

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
                        Console.WriteLine("Client application disconnected");
                    }
                    catch (Exception e) { Console.WriteLine(e.ToString()); }
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }


        public static void BufferUpdate(List<byte> buffer)
        {
            for (int i = 0; i < 12; i++)
            {
                _serialPort.Write(buffer.ToArray(), 0, 8);
            }

            byte[] b = { 0xFF };
            _serialPort.Write(b, 0, 1);
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

        public static void SerialRead()
        {
            while (true)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Console.WriteLine("[Vest] : " + message);
                }
                catch (TimeoutException) { }
            }
        }
    }
}
