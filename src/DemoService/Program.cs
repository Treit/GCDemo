using KTrie;
using Microsoft.AspNetCore.Http.HttpResults;
using System.IO.MemoryMappedFiles;

var callCount = 0;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var words = File.ReadAllLines(@"data\words.txt");
var trie = new Trie();
foreach (var word in words)
{
    trie.Add(word);
}

app.MapGet("/ping", (HttpContext context) =>
{
    Interlocked.Increment(ref callCount);
    byte[] bytes = [];

    if (context.Request.Query.ContainsKey("size"))
    {
        bytes = new byte[int.Parse(context.Request.Query["size"]!)];
    }

    if (callCount % 10 == 0 && context.Request.Query.ContainsKey("gc"))
    {
        GC.Collect();
    }

    return "pong - " + bytes.Length;
});

app.MapGet("/fromPrefix", (HttpContext context) =>
{
    Interlocked.Increment(ref callCount);

    var prefix = context.Request.Query["prefix"].FirstOrDefault();
    var countStr = context.Request.Query["count"].FirstOrDefault();

    if (prefix == null)
    {
        return Results.BadRequest("prefix is required");
    }

    if (countStr == null || !int.TryParse(countStr, out var count))
    {
        return Results.BadRequest("count is required and must be an integer");
    }

    var matches = trie.StartsWith(prefix).Take(count);

    return Results.Ok(matches);
});

app.MapGet("/fromPrefixToList", (HttpContext context) =>
{
    Interlocked.Increment(ref callCount);

    var prefix = context.Request.Query["prefix"].FirstOrDefault();
    var countStr = context.Request.Query["count"].FirstOrDefault();

    if (prefix == null)
    {
        return Results.BadRequest("prefix is required");
    }

    if (countStr == null || !int.TryParse(countStr, out var count))
    {
        return Results.BadRequest("count is required and must be an integer");
    }

    var matches = trie.StartsWith(prefix).ToList().Take(count);

    return Results.Ok(matches);
});

DoMonitorResources("MonitorResources", CancellationToken.None);

app.Run();

static void DoMonitorResources(string operation, CancellationToken cancelToken)
{
    GC.RegisterForFullGCNotification(10, 10);
    Console.Title = operation;

    _ = Task.Factory.StartNew(
        () =>
        MonitorResources(cancelToken),
        cancelToken,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);
}

static void MonitorResources(CancellationToken ct)
{
    var sleepTime = 500;
    var commfile = "demo_comm";
    using var mmf = MemoryMappedFile.CreateOrOpen(commfile, 8);
    using var view = mmf.CreateViewAccessor(0, 8);

    view.Write(4, true);

    while (!ct.IsCancellationRequested)
    {
        try
        {
            Thread.Sleep(sleepTime);
            GC.Collect();
        }
        catch (Exception e)
        {
            Console.WriteLine($"OH NO {e}");
        }
    }

    view.Write(0, -1);
}