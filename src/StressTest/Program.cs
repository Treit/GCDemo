using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

var emojiMap = new Dictionary<HttpStatusCode, string>
{
    [HttpStatusCode.OK] = "😎",
    [HttpStatusCode.InternalServerError] = "😡",
    [HttpStatusCode.RequestTimeout] = "⏱️",
    [HttpStatusCode.NoContent] = "😭",
    [HttpStatusCode.NotFound] = "❓"
};

Console.WriteLine();
Console.WriteLine("Result Key");
Console.WriteLine("-----------------------------");
foreach (var (key, value) in emojiMap)
{
    Console.WriteLine($"{value} => {key}");
}
Console.WriteLine("-----------------------------");
Console.WriteLine();

ThreadPool.SetMinThreads(1000, 1000);
Console.OutputEncoding = Encoding.UTF8;

var cookieContainer = new CookieContainer();

var handler = new HttpClientHandler
{
    MaxConnectionsPerServer = 1000,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    UseProxy = false,
    CookieContainer = cookieContainer
};

var client = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(1000)
};

var tasks = new List<Task>();
var mdop = args.Length > 0 ? int.Parse(args[0]) : 100;
var timings = new double[mdop];
var anyFailures = false;
var delayStart = true;
var successCount = 0;
var totalCount = 0;
var failedActivityIds = new ConcurrentBag<Guid>();

foreach (var arg in args)
{
    if (arg.Equals("--no-delay", StringComparison.OrdinalIgnoreCase))
    {
        delayStart = false;
    }

    if (arg.Equals("--add-cookie", StringComparison.OrdinalIgnoreCase))
    {
        cookieContainer.Add(new Cookie("MUID", "gibberish", "/", ".msn.com"));
        Console.WriteLine("🍪");
    }
}

var url = "http://localhost:5257/fromPrefix?prefix=tr&count=10";

if (args.Length > 1)
{
    url = args[1];
}

var resetEvent = new ManualResetEvent(false);

for (int i = 0; i < mdop; i++)
{
    var num = i;
    var task = Task.Run(() =>
    {
        resetEvent.WaitOne();
        MakeGetCall(num);
    });

    tasks.Add(task);
}

if (delayStart)
{
    Console.WriteLine("-   🔴");
    await Task.Delay(500);

    Console.WriteLine("3   🟡");
    await Task.Delay(500);

    Console.WriteLine("2   🟡");
    await Task.Delay(500);

    Console.WriteLine("1   🟡");
    await Task.Delay(500);

    while (true)
    {
        if (!tasks.All(t => t.Status == TaskStatus.Running))
        {
            await Task.Delay(100);
        }
        else
        {
            break;
        }
    }
}

Console.WriteLine("GO! 🟢");

resetEvent.Set();

await Task.WhenAll(tasks);

var avg = timings.Average();
var min = timings.Min();
var max = timings.Max();

Console.WriteLine();
var token = anyFailures ? "😨" : "😊";
var percentSuccess = successCount / (double)totalCount * 100;
var percentFailure = Math.Round(100.0 - percentSuccess, 2);

if (anyFailures)
{
    Console.WriteLine();
    Console.WriteLine($"🔥 Total: {totalCount} Success: {successCount} Failures: {totalCount - successCount}");
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write($"🔥 {percentFailure}% failure.");
    Console.ResetColor();
    Console.WriteLine();
}

var timingLog = $"{token} [{DateTime.Now}] avg: {avg} ms. min: {min} ms. max: {max} ms. {percentSuccess}% success.";
Console.WriteLine();
Console.WriteLine(timingLog);
using var sw = new StreamWriter("timings.txt", true);
sw.WriteLine(timingLog);

if (failedActivityIds.Any())
{
    Console.WriteLine();
    Console.WriteLine("😔 Failed Activity Ids:");
    foreach (var id in failedActivityIds)
    {
        Console.WriteLine(id);
    }
}

void MakeGetCall(int slot)
{
    try
    {
        var activityId = Guid.NewGuid();
        var localUrl = Regex.Replace(url, @"activityId=[a-fA-F0-9-]{36}", $"activityId={activityId}");
        var msg = new HttpRequestMessage(HttpMethod.Get, localUrl);

        msg.Headers.Add("X-Variants", "mkt:allmk,sid:windows-windowshp-feeds");
        var sw = Stopwatch.StartNew();
        var result = client.Send(msg);
        timings[slot] = sw.ElapsedMilliseconds;
        var token = result.StatusCode switch
        {
            HttpStatusCode.OK => emojiMap[HttpStatusCode.OK],
            HttpStatusCode.InternalServerError => emojiMap[HttpStatusCode.InternalServerError],
            HttpStatusCode.NoContent => emojiMap[HttpStatusCode.NoContent],
            HttpStatusCode.RequestTimeout => emojiMap[HttpStatusCode.RequestTimeout],
            _ => result.StatusCode.ToString()
        };

        Interlocked.Increment(ref totalCount);

        Console.Write(token);
        if (result.StatusCode != HttpStatusCode.OK)
        {
            anyFailures = true;
            failedActivityIds.Add(activityId);
        }
        else
        {
            Interlocked.Increment(ref successCount);
        }

        if (result.StatusCode == HttpStatusCode.InternalServerError)
        {
            //Console.WriteLine(await result.Content.ReadAsStringAsync());
        }
    }
    catch (Exception e)
    {
        anyFailures = true;
        Console.WriteLine(e);
    }
}
