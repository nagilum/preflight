using System.Net;
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
            WriteLine(
                ConsoleColor.DarkYellow,
                "[WARNING] ",
                (byte)0x00,
                "Aborted by user!");
            
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

            var req = new HttpRequestMessage(HttpMethod.Options, options.Url);

            req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", options.HttpMethod);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue(Name, Version));

            if (options.Headers is not null)
            {
                req.Headers.Add("Access-Control-Request-Headers", options.Headers);
            }

            if (options.Origin is not null)
            {
                req.Headers.Add("Origin", options.Origin.ToString());
            }
            
            WriteLine(
                ConsoleColor.DarkCyan,
                "[REQUEST] ",
                (byte)0x00,
                "OPTIONS ",
                options.Url);

            foreach (var (key, value) in req.Headers)
            {
                WriteLine(
                    ConsoleColor.White,
                    "[HEADER] ",
                    (byte)0x00,
                    key,
                    ": ",
                    ConsoleColor.White,
                    value.First());
            }

            // Perform request write out response.
            var res = await client.SendAsync(req, cancellationToken);
            
            WriteLine(
                Environment.NewLine,
                res.IsSuccessStatusCode ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed,
                "[RESPONSE] ",
                (byte)0x00,
                $"{(int)res.StatusCode} {res.ReasonPhrase}");

            var headers = GetHeaders(res);

            foreach (var (key, value) in headers)
            {
                WriteLine(
                    ConsoleColor.White,
                    "[HEADER] ",
                    (byte)0x00,
                    key,
                    ": ",
                    ConsoleColor.White,
                    value);
            }

            // Is the status code valid?
            if (res.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent)
            {
                WriteLine(
                    Environment.NewLine,
                    ConsoleColor.DarkGreen,
                    "[PASSED] ",
                    (byte)0x00,
                    "Status Code is either 200 or 204.");
            }
            else
            {
                WriteLine(
                    Environment.NewLine,
                    ConsoleColor.DarkRed,
                    "[FAILED] ",
                    (byte)0x00,
                    "Status Code was neither 200 or 204!");
            }
            
            // Do we have and allowed origin?
            if (res.Headers.Contains("Access-Control-Allow-Origin"))
            {
                WriteLine(
                    ConsoleColor.DarkGreen,
                    "[PASSED] ",
                    (byte)0x00,
                    "Origin matches Access-Control-Allow-Origin.");
            }
            else
            {
                WriteLine(
                    ConsoleColor.Red,
                    "[FAILED] ",
                    (byte)0x00,
                    "Response did not contain Access-Control-Allow-Origin header!");
            }
            
            // Do we have an allowed HTTP method?
            if (res.Headers.Contains("Access-Control-Allow-Methods"))
            {
                WriteLine(
                    ConsoleColor.DarkGreen,
                    "[PASSED] ",
                    (byte)0x00,
                    "Access-Control-Request-Method matches Access-Control-Allow-Methods.");
            }
            else
            {
                WriteLine(
                    ConsoleColor.Red,
                    "[FAILED] ",
                    (byte)0x00,
                    "Response did not contain Access-Control-Allow-Methods header!");
            }
            
            // Are we checking headers?
            if (options.Headers is not null)
            {
                if (res.Headers.Contains("Access-Control-Allow-Headers"))
                {
                    WriteLine(
                        ConsoleColor.DarkGreen,
                        "[PASSED] ",
                        (byte)0x00,
                        "Access-Control-Request-Headers matches Access-Control-Allow-Headers.");
                }
                else
                {
                    WriteLine(
                        ConsoleColor.Red,
                        "[FAILED] ",
                        (byte)0x00,
                        "Response did not contain Access-Control-Allow-Headers header!");
                }
            }
            else
            {
                WriteLine(
                    ConsoleColor.DarkYellow,
                    "[SKIPPED] ",
                    (byte)0x00,
                    "Access-Control-Request-Headers was not used.");
            }
            
            // Do we have max-age?
            if (res.Headers.TryGetValues("Access-Control-Max-Age", out var maxAgeValues))
            {
                if (int.TryParse(maxAgeValues.First(), out var maxAgeSeconds))
                {
                    if (maxAgeSeconds is > -1 and < 86400)
                    {
                        WriteLine(
                            ConsoleColor.DarkGreen,
                            "[PASSED] ",
                            (byte)0x00,
                            "Access-Control-Max-Age is valid.");
                    }
                    else
                    {
                        WriteLine(
                            ConsoleColor.DarkYellow,
                            "[PASSED] ",
                            (byte)0x00,
                            "Access-Control-Max-Age is outside \"normal\" range, 0 to 86400!");
                    }
                }
                else
                {
                    WriteLine(
                        ConsoleColor.DarkRed,
                        "[FAILED] ",
                        (byte)0x00,
                        "Access-Control-Max-Age is invalid!");
                }
            }
            else
            {
                WriteLine(
                    ConsoleColor.DarkYellow,
                    "[SKIPPED] ",
                    (byte)0x00,
                    "Access-Control-Max-Age was not used.");
            }
        }
        catch (Exception ex)
        {
            while (true)
            {
                WriteLine(
                    ConsoleColor.DarkRed,
                    "[ERROR] ",
                    (byte)0x00,
                    ex.Message);

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
            $"  {Name.ToLower()} <url> <options>",
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

                    if (!Uri.TryCreate(args[i + 1], UriKind.Absolute, out var originUri))
                    {
                        Console.WriteLine($"Error: '{args[i + 1]}' is not a valid origin URL.");
                        return false;
                    }

                    options.Origin = originUri;
                    skip = true;
                    break;

                default:
                    if (options.Url is not null)
                    {
                        Console.WriteLine("Error: You can only specify one URL.");
                        return false;
                    }

                    if (!Uri.TryCreate(argv, UriKind.Absolute, out var requestUri))
                    {
                        Console.WriteLine($"Error: '{argv}' is not a valid URL.");
                        return false;
                    }

                    options.Url = requestUri;
                    break;
            }
        }

        if (options.Url is null)
        {
            Console.WriteLine("Error: You must specify a URL to request!");
            return false;
        }

        if (options.Origin is null)
        {
            Console.WriteLine("Error: You must specify an origin. Use the --origin or -o option to specify it.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Write objects to console.
    /// </summary>
    /// <param name="objects">Objects.</param>
    private static void WriteLine(params object?[] objects)
    {
        foreach (var obj in objects)
        {
            switch (obj)
            {
                case ConsoleColor cc:
                    Console.ForegroundColor = cc;
                    break;
                
                case byte and 0x00:
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