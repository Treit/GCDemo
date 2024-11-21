using KTrie;

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

app.MapGet("/fromPrefix", (HttpContext context) =>
{
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

app.Run();
