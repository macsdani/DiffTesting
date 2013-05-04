using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            //DiffRequest req = new DiffRequest(new string[] { "a", "a", "a", "b","b" }, new string[] { "a","b","a", "b"});
            DiffRequest req = new DiffRequest(System.IO.File.ReadAllLines(@"D:\in1.txt"),
                                              System.IO.File.ReadAllLines(@"D:\in2.txt"));
            DiffOperation op = new DiffOperation();
            DiffResult res = op.getDiff(req);
            //Console.WriteLine(res.toString());
            String[] in1=System.IO.File.ReadAllLines(@"D:\in1.txt");
            String[] in2=System.IO.File.ReadAllLines(@"D:\in2.txt");
            Thread oThread = new Thread(new ThreadStart(Program.MemoryRead));
            oThread.Start();
            for (int i = 1; i < 10; i++) {
				int multip = 5 * i;
				//mt.setLines(lines * multip);
				String[] testF1B = new String[in1.Length * multip];
				String[] testF2B = new String[in2.Length * multip];
				for (int x = 0; x < testF1B.Length; x++) {
					testF1B[x] = in1[x % in1.Length];
				}
				for (int x = 0; x < testF2B.Length; x++) {
					testF2B[x] = in2[x % in2.Length];
				}
                req = new DiffRequest(testF1B,testF2B);
                op = new DiffOperation();
				DateTime before = DateTime.Now;
				res = op.getDiff(req);
				DateTime after = DateTime.Now;
                TimeSpan elapsedTime = after - before;
				Console.WriteLine((50 * multip) + "|"
						+ (elapsedTime.TotalMilliseconds));
			}
            oThread.Abort();

            Console.ReadKey();
        }

        public static void MemoryRead()
        {
            while (true)
            {
                Console.WriteLine("Total Memory: " + GC.GetTotalMemory(false)/1024/1024+" MB");
                Thread.Sleep(5);
            }
        }
    }
}
