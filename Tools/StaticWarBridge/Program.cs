const string TelemetryRoot =
    @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector";

static string[] Tail(string path, int count)
{
    if (!File.Exists(path))
        return Array.Empty<string>();

    return File.ReadLines(path)
        .TakeLast(count)
        .ToArray();
}

static DirectoryInfo? LatestSession()
{
    string root = Path.Combine(
        TelemetryRoot,
        "ai",
        "behavior-delta");

    if (!Directory.Exists(root))
        return null;

    return new DirectoryInfo(root)
        .GetDirectories()
        .OrderByDescending(x => x.LastWriteTimeUtc)
        .FirstOrDefault();
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
    "http://127.0.0.1:8422");

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl =
        "no-store, no-cache, must-revalidate, max-age=0";

    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers.Expires = "0";

    await next();
});

app.MapGet("/health", () =>
{
    return Results.Json(new
    {
        ok = true,
        bridge = "ClanAI Static War Bridge",
        time = DateTimeOffset.Now
    });
});

app.MapGet("/wartext", () =>
{
    DirectoryInfo? session = LatestSession();

    if (session == null)
        return Results.Text(
            "NO ACTIVE TELEMETRY SESSION",
            "text/plain; charset=utf-8");

    string behaviorPath =
        Path.Combine(
            session.FullName,
            "behavior_changes.csv");

    string candidatesPath =
        Path.Combine(
            session.FullName,
            "decision_candidates.csv");

    string deltasPath =
        Path.Combine(
            session.FullName,
            "radius_deltas.csv");

    string statusPath =
        Path.Combine(
            session.FullName,
            "status.txt");

    var output = new List<string>();

    output.Add(
        "generated=" +
        DateTimeOffset.Now.ToString("O"));

    output.Add(
        "session=" + session.Name);

    output.Add("");
    output.Add("=== STATUS ===");
    output.AddRange(Tail(statusPath, 60));

    output.Add("");
    output.Add("=== RECENT BEHAVIOR CHANGES ===");
    output.AddRange(Tail(behaviorPath, 100));

    output.Add("");
    output.Add("=== RECENT DECISION CANDIDATES ===");
    output.AddRange(Tail(candidatesPath, 200));

    output.Add("");
    output.Add("=== RECENT LOCAL WORLD DELTAS ===");
    output.AddRange(Tail(deltasPath, 80));

    return Results.Text(
        string.Join(
            Environment.NewLine,
            output),
        "text/plain; charset=utf-8");
});

app.MapGet("/diagtext", () =>
{
    string path = Path.Combine(
        TelemetryRoot,
        "logs",
        "inspector.log");

    return Results.Text(
        string.Join(
            Environment.NewLine,
            Tail(path, 250)),
        "text/plain; charset=utf-8");
});


app.MapGet("/slots", () =>
{
    var sb = new System.Text.StringBuilder();

    sb.AppendLine("<html><body>");

    for (int i = 1; i <= 200; i++)
    {
        string n = i.ToString("000");

        sb.AppendLine(
            "<a href=\"/wartext?slot=" + n + "\">WAR SLOT " + n + "</a>" +
            " | " +
            "<a href=\"/diagtext?slot=" + n + "\">DIAG SLOT " + n + "</a><br>");
    }

    sb.AppendLine("</body></html>");

    return Results.Content(
        sb.ToString(),
        "text/html; charset=utf-8");
});

app.Run();


