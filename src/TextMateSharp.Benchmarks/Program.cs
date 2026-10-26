using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace TextMateSharp.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--dump")
            {
                // The SHA-256 of every sample's tokens, optionally writing the tokens to a file.
                using var writer = args.Length > 1 ? new StreamWriter(args[1]) : null;
                Console.WriteLine(Samples.Dump(writer));
                return;
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
