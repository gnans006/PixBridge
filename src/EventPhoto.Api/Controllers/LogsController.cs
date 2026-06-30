using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EventPhoto.Api.Controllers;

/// <summary>Provides recent application log entries from Serilog file output for the admin UI.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class LogsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IWebHostEnvironment env, ILogger<LogsController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>Returns the most recent log entries from today's Serilog log file (max 200 lines).</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetRecent([FromQuery] int limit = 200)
    {
        try
        {
            var logsFolder = Path.Combine(_env.ContentRootPath, "logs");
            if (!Directory.Exists(logsFolder))
                return Ok(new { success = true, data = Array.Empty<object>() });

            // Find today's log file (Serilog rolling daily: pixbridge-YYYYMMDD.log)
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var logFile = Directory.GetFiles(logsFolder, "*.log")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (logFile is null)
                return Ok(new { success = true, data = Array.Empty<object>() });

            var entries = new List<object>();

            // Read last N lines without loading whole file
            var lines = ReadLastLines(logFile, limit * 2);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Try parse as Serilog compact JSON (if using JsonFormatter)
                if (line.TrimStart().StartsWith('{'))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        entries.Add(new
                        {
                            timestamp = root.TryGetProperty("@t", out var t) ? t.GetString() : DateTime.UtcNow.ToString("O"),
                            level = root.TryGetProperty("@l", out var l) ? l.GetString() : "Information",
                            message = root.TryGetProperty("@m", out var m) ? m.GetString() : line,
                            exception = root.TryGetProperty("@x", out var x) ? x.GetString() : null,
                        });
                        continue;
                    }
                    catch { /* fall through to plain text parse */ }
                }

                // Parse plain-text Serilog output: [2024-01-01 12:00:00 INF] Message
                var parsed = ParsePlainTextLogLine(line);
                if (parsed is not null)
                    entries.Add(parsed);
            }

            var result = entries.TakeLast(limit).ToList();
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read log file");
            return Ok(new { success = true, data = Array.Empty<object>() });
        }
    }

    private static readonly Regex PlainLogPattern = new(
        @"^\[(?<ts>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+(?<lvl>[A-Z]{2,3})\]\s+(?<msg>.+)$",
        RegexOptions.Compiled);

    private static object? ParsePlainTextLogLine(string line)
    {
        var match = PlainLogPattern.Match(line);
        if (!match.Success)
            return new { timestamp = DateTime.UtcNow.ToString("O"), level = "Information", message = line.Trim(), exception = (string?)null };

        var levelCode = match.Groups["lvl"].Value;
        var level = levelCode switch
        {
            "VRB" => "Verbose",
            "DBG" => "Debug",
            "INF" => "Information",
            "WRN" => "Warning",
            "ERR" => "Error",
            "FTL" => "Fatal",
            _ => "Information"
        };

        return new
        {
            timestamp = DateTime.Parse(match.Groups["ts"].Value).ToString("O"),
            level,
            message = match.Groups["msg"].Value.Trim(),
            exception = (string?)null
        };
    }

    private static List<string> ReadLastLines(string filePath, int lineCount)
    {
        var lines = new List<string>();
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var allLines = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line is not null) allLines.Add(line);
            }
            return allLines.TakeLast(lineCount).ToList();
        }
        catch
        {
            return lines;
        }
    }
}
