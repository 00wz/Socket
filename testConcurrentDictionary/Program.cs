using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server
{
    class Program
    {

        static ConcurrentDictionary<int,int> Clients = new ConcurrentDictionary<int, int>();

        static void Main(string[] args)
        {
            for (int i = 0; i <= 1000; i++)
            {
                Clients[i] = i;
            }

            Task task1 = new Task(() =>
            {
                for (int i = 1000; i >= 0; i--)
                {
                    Clients.Remove(i, out _);
                    Thread.Sleep(20);
                }
            });
            task1.Start();

            Task task2 = new Task(() =>
            {
                foreach(var i in Clients)
                {
                    Console.WriteLine(i);
                    Thread.Sleep(20);
                }
            });
            task2.Start();

            Console.ReadKey();
        }
    }
}
