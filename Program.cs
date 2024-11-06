using System.Diagnostics;
using System.Net.Http.Headers;

namespace Preflight;

internal static class Program
{
    /// <summary>
    /// Program name.
    /// </summary>
    private const string Name = "Preflight";

    /// <summary>
    /// Program version.
    /// </summary>
    private const string Version = "0.1-alpha";
    
    /// <summary>
    /// Init all the things...
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static async Task Main(string[] args)
    {
        if (args.Length is 0 ||
            args.Any(n => n is "-h" or "--help"))
        {
            ShowProgramUsage();
            return;
        }

        if (!TryParseCmdArgs(args, out var options))
        {
            return;
        }

        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;

        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("Aborted by user!");
            e.Cancel = true;
            tokenSource.Cancel();
        };

        await PerformPreflightRequest(options, token);
    }

    /// <summary>
    /// Get all headers from a HTTP response message.
    /// </summary>
    /// <param name="res">HTTP response message.</param>
    /// <returns>Headers.</returns>
    private static Dictionary<string, string> GetHeaders(HttpResponseMessage res)
    {
        var headers = new Dictionary<string, string>();

        foreach (var (key, value) in res.Headers)
        {
            headers.TryAdd(key, value.First());
        }
        
        foreach (var (key, value) in res.TrailingHeaders)
        {
            headers.TryAdd(key, value.First());
        }
        
        foreach (var (key, value) in res.Content.Headers)
        {
            headers.TryAdd(key, value.First());
        }

        return headers
            .OrderBy(n => n.Key)
            .ToDictionary(n => n.Key,
                n => n.Value);
    }

    /// <summary>
    /// Perform the OPTIONS preflight request and analyze response.
    /// </summary>
    /// <param name="options">Parsed options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task PerformPreflightRequest(IOptions options, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Name, Version));

            var req = new HttpRequestMessage(HttpMethod.Options, options.Url)
            {
                Headers = { { "Access-Control-Request-Method", options.HttpMethod } }
            };
            
            WriteLine(
                ConsoleColor.DarkCyan, 
                "OPTIONS ", 
                0x00, 
                options.Url!);
            
            WriteLine(
                ConsoleColor.DarkCyan, 
                "> ", 
                0x00, 
                "Accept: ", 
                ConsoleColor.White, "*/*");
            
            WriteLine(
                ConsoleColor.DarkCyan, 
                "> ", 
                0x00, 
                "User-Agent: ", 
                ConsoleColor.White, 
                $"{Name}/{Version}");
            
            WriteLine(
                ConsoleColor.DarkCyan, 
                "> ", 
                0x00, 
                "Access-Control-Request-Method: ", 
                ConsoleColor.White, 
                options.HttpMethod);

            if (options.Headers is not null)
            {
                WriteLine(
                    ConsoleColor.DarkCyan, 
                    "> ", 
                    0x00, 
                    "Access-Control-Request-Headers: ", 
                    ConsoleColor.White, 
                    options.Headers);
                
                req.Headers.Add("Access-Control-Request-Headers", options.Headers);
            }

            if (options.Origin is not null)
            {
                WriteLine(
                    ConsoleColor.DarkCyan, 
                    "> ", 
                    0x00, 
                    "Origin: ", 
                    ConsoleColor.White, 
                    options.Origin);
                
                req.Headers.Add("Origin", options.Origin);
            }

            var stopwatch = Stopwatch.StartNew();
            var res = await client.SendAsync(req, cancellationToken);
            
            stopwatch.Stop();
            
            WriteLine(
                res.IsSuccessStatusCode ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, 
                $"{(int)res.StatusCode} {res.ReasonPhrase}");
            
            WriteLine(
                res.IsSuccessStatusCode ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, 
                $"{stopwatch.ElapsedMilliseconds} ms");

            var headers = GetHeaders(res);

            foreach (var (key, value) in headers)
            {
                WriteLine(
                    ConsoleColor.DarkCyan,
                    "< ",
                    0x00,
                    key,
                    ": ",
                    key.StartsWith("Access-Control", StringComparison.OrdinalIgnoreCase)
                        ? ConsoleColor.DarkGreen
                        : ConsoleColor.White,
                    value);
            }
        }
        catch (Exception ex)
        {
            while (true)
            {
                Console.WriteLine($"Error: {ex.Message}");

                if (ex.InnerException is null)
                {
                    break;
                }

                ex = ex.InnerException;
            }
        }
    }

    /// <summary>
    /// Show program usage and options.
    /// </summary>
    private static void ShowProgramUsage()
    {
        var lines = new[]
        {
            $"{Name} v{Version}",
            "Performs a CORS preflight request and verifies the response.",
            "",
            "Usage:",
            "  corstester <url> [<options>]",
            "",
            "Options:",
            "  --method <string>    HTTP method to check for. Defaults to GET.",
            "  --headers <string>   Comma separated list of headers to check for. Ex: content-type,x-pingother",
            "  --origin <host>      Set the origin for the request.",
            "",
            "Source and documentation available at https://github.com/nagilum/preflight"
        };

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    /// <summary>
    /// Attempt to parse the command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="options">Parsed options.</param>
    /// <returns>Success.</returns>
    private static bool TryParseCmdArgs(string[] args, out IOptions options)
    {
        options = new Options();

        var skip = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (skip)
            {
                skip = false;
                continue;
            }

            var argv = args[i];

            switch (argv)
            {
                case "-m":
                case "--method":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by the name of a HTTP method.");
                        return false;
                    }

                    options.HttpMethod = args[i + 1].ToUpper();
                    skip = true;
                    break;

                case "-e":
                case "--headers":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a list of headers.");
                        return false;
                    }

                    options.Headers = args[i + 1];
                    skip = true;
                    break;

                case "-o":
                case "--origin":
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine($"Error: Option {argv} must be followed by a valid host.");
                        return false;
                    }

                    options.Origin = args[i + 1];
                    skip = true;
                    break;

                default:
                    if (options.Url is not null)
                    {
                        Console.WriteLine("Error: You can only specify one URL.");
                        return false;
                    }

                    if (!Uri.TryCreate(argv, UriKind.Absolute, out var uri))
                    {
                        Console.WriteLine($"Error: '{argv}' is not a valid URL.");
                        return false;
                    }

                    options.Url = uri;
                    break;
            }
        }

        if (options.Url is not null)
        {
            return true;
        }

        Console.WriteLine("Error: You must specify a URL to request.");
        return false;
    }

    /// <summary>
    /// Write objects to console.
    /// </summary>
    /// <param name="objects">Objects.</param>
    private static void WriteLine(params object[] objects)
    {
        foreach (var obj in objects)
        {
            switch (obj)
            {
                case ConsoleColor cc:
                    Console.ForegroundColor = cc;
                    break;
                
                case 0x00:
                    Console.ResetColor();
                    break;
                
                default:
                    Console.Write(obj);
                    break;
            }
        }
        
        Console.ResetColor();
        Console.WriteLine();
    }
}