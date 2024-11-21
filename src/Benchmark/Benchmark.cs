namespace Test
{
    using BenchmarkDotNet.Attributes;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    [MemoryDiagnoser]
    public class Benchmark
    {
        HttpClient _client;

        [Params(10, 100, 1000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _client = new HttpClient();
        }

        [Benchmark(Baseline = true)]
        public async Task<int> FromPrefix()
        {
            var totalProcessed = 0;

            for (int i = 0; i < Count; i++)
            {
                var result = await _client.GetAsync("http://localhost:5257/fromPrefix?prefix=a&count=1000");
                totalProcessed++;
            }

            return totalProcessed;
        }

        [Benchmark]
        public async Task<int> FromPrefixToList()
        {
            var totalProcessed = 0;

            for (int i = 0; i < Count; i++)
            {
                var result = await _client.GetAsync("http://localhost:5257/fromPrefixToList?prefix=a&count=1000");
                totalProcessed++;
            }

            return totalProcessed;
        }        
    }
}
