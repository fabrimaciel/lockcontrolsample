using System;
using System.Linq;

namespace LockControlSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var accessControl = new AccessControl();

            var method = new System.Threading.ParameterizedThreadStart(state =>
            {
                var items = (object[])state;
                var id = (int)items[0];
                var reference = items[1];
                var time = (int)items[2];

                using (accessControl.CreateLocker(reference, System.Threading.Timeout.InfiniteTimeSpan))
                {
                    Console.WriteLine($"Thread {id} starting...");
                    System.Threading.Thread.Sleep(time);
                    Console.WriteLine($"Thread {id} stopped...");
                }
            });

            var thread1 = new System.Threading.Thread(method);
            var thread2 = new System.Threading.Thread(method);
            var thread3 = new System.Threading.Thread(method);
            var thread4 = new System.Threading.Thread(method);

            var reference1 = new object();
            var reference2 = new object();

            thread1.Start(new object[] { 1, reference1, 5000 });
            thread2.Start(new object[] { 2, reference2, 1000 });
            thread3.Start(new object[] { 3, reference1, 3000 });
            thread4.Start(new object[] { 4, reference1, 1500 });

            var threads = new[] { thread1, thread2, thread3, thread4 };

            while (threads.Any(f => f.IsAlive))
            {
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
