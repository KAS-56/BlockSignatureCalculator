using System;
using System.Collections.Generic;
using System.IO;
using BlockSignatureCalculator.Readers;
using BlockSignatureCalculator.Workers;
using BlockSignatureCalculator.Writers;

namespace BlockSignatureCalculator
{
    internal static class Program
    {
        private const int CorrectArgumentsCount = 2;
        private static readonly int MaxThreads = Environment.ProcessorCount;

        private static int Main(string[] args)
        {
            if (!ParseArguments(args, out var options))
            {
                ShowHowToUse();
                return 1;
            }

            bool isNotSucceeded = Run(options);

            return isNotSucceeded ? 1 : 0;
        }

        private static bool Run(Options options)
        {
            IWorker worker = new Worker();
            IReader reader = new SourceReader(options.BlockSize, options.SourceFile, MaxThreads);
            IWriter writer = new ResultWriter(MaxThreads);
            Synchronizer synchronizer = new Synchronizer(MaxThreads, reader, writer, worker);
            return synchronizer.Run(options);
        }

        private static void ShowHowToUse()
        {
            string file = typeof(Program).Assembly.Location;
            string executableName = Path.GetFileNameWithoutExtension(file);
            Console.WriteLine($"Calculate signature of file divided by blocks with specified size.{Environment.NewLine}");
            Console.WriteLine($"Usage: {executableName} source_file block_size{Environment.NewLine}");
            Console.WriteLine("   source_file\t - name or path to source file");
            Console.WriteLine("   block_size\t - size of each block in bytes");
        }

        private static bool ParseArguments(IReadOnlyList<string> args, out Options options)
        {
            options = new Options();
            if (args.Count != CorrectArgumentsCount)
            {
                return false;
            }

            if (!int.TryParse(args[1], out var blockSize))
            {
                return false;
            }

            options.SourceFile = args[0];
            options.BlockSize = blockSize;

            return true;
        }
    }
}
