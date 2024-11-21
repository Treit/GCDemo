namespace Test
{
    using BenchmarkDotNet.Running;
    using System;
    using System.Threading.Tasks;

    internal class Program
    {
        static async Task Main(string[] args)
        {
#if RELEASE
            BenchmarkRunner.Run<Benchmark>();
#else
            Benchmark b = new Benchmark();
            b.Count = 100;
            b.GlobalSetup();
            var first = await b.FromPrefix();
            var second = await b.FromPrefixToList();
            Console.WriteLine(first);
            Console.WriteLine(second);
#endif

        }
    }
}
