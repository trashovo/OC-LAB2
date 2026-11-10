using System;
using System.Diagnostics;
using System.Threading;

namespace MatrixMultiplication
{
    class Program
    {
        static int progressCounter = 0;
        static readonly object locker = new object();

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Использование: dotnet run -c Release -- <n> <m> <k> <T>");
                return;
            }

            int n = int.Parse(args[0]);
            int m = int.Parse(args[1]);
            int k = int.Parse(args[2]);
            int T = int.Parse(args[3]);

            if (T < 1) return;

            Console.WriteLine($"Генерация матриц A({n}x{m}) и B({m}x{k})...");
            
            int[,] A = new int[n, m];
            int[,] B = new int[m, k];
            long[,] C = new long[n, k]; 

            Random rand = new Random();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    A[i, j] = rand.Next(0, 10);

            for (int i = 0; i < m; i++)
                for (int j = 0; j < k; j++)
                    B[i, j] = rand.Next(0, 10);

            Console.WriteLine($"Запуск умножения на {T} потоках...");
            Thread[] threads = new Thread[T];
            
            int rowsPerThread = n / T;
            int remainder = n % T;
            int currentRow = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int p = 0; p < T; p++)
            {
                int startRow = currentRow;
                int endRow = startRow + rowsPerThread;
                if (p < remainder) endRow++;
                currentRow = endRow;

                threads[p] = new Thread(() => Worker(A, B, C, startRow, endRow, m, k));
                threads[p].Start();
            }

            int lastPercent = -1;
            while (true)
            {
                int currentProgress;
                lock (locker)
                {
                    currentProgress = progressCounter;
                }

                int percent = (int)((currentProgress / (double)n) * 100);

                if (percent > lastPercent)
                {
                    Console.WriteLine($"Прогресс: {percent}%");
                    lastPercent = percent;
                }

                if (currentProgress >= n) break;
                Thread.Sleep(200);
            }

            for (int p = 0; p < T; p++) threads[p].Join();
            sw.Stop();

            long checksum = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < k; j++)
                    checksum += C[i, j];

            Console.WriteLine("------------------------------");
            Console.WriteLine($"Время умножения: {sw.Elapsed.TotalSeconds:F4} секунд");
            Console.WriteLine($"Контрольная сумма: {checksum}");
        }

        static void Worker(int[,] A, int[,] B, long[,] C, int startRow, int endRow, int m, int k)
        {
            for (int i = startRow; i < endRow; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    for (int t = 0; t < m; t++)
                    {
                        C[i, j] += A[i, t] * B[t, j];
                    }
                }
                lock (locker)
                {
                    progressCounter++;
                }
            }
        }
    }
}
