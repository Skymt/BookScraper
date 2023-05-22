using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace BookScraper
{
    internal static class Scraper
    {
        public static string Destination = @"C:\Temp";

        static readonly ConcurrentQueue<string> _textQueue = new();
        static readonly ConcurrentQueue<string> _fileQueue = new();
        static readonly ConcurrentDictionary<string, string?> _remoteToLocalMap = new();
        static readonly HttpClient _client = new() { BaseAddress = new("http://books.toscrape.com/") };

        public static async Task<int> Init()
        {
            _remoteToLocalMap["/index.html"] = Path.Combine(Destination, "index.html");
            _textQueue.Enqueue("/index.html");
            await Next(); return _textQueue.Count + _fileQueue.Count;
        }

        public static void Run(int threadCount = 1)
        {
            // Report progress to console window title (This is technically +1 in threads, but since it sleeps a lot I don't count it...)
            using Timer _ = new(_ => Console.Title = $"Total: {_remoteToLocalMap.Count}, Remaining: {_textQueue.Count + _fileQueue.Count}", null, 50, 100);
            // Sanity check of the thread count
            if (threadCount < 1) throw new ArgumentOutOfRangeException(nameof(threadCount), "At least one thread is required to run.");
            // Setup the workers
            var threads = Enumerable.Range(1, threadCount).Select(async id =>
            {
                while (true)
                {
                    while (await Next(id)) ;
                    await Task.Delay(100);
                    if (!await Next(id))
                    {
                        Log($"Thread {id} exited.", ConsoleColor.Green);
                        break;
                    }
                }
            });
            // Wait for all threads to complete.
            Task.WaitAll(threads.ToArray());
            Console.ResetColor();
        }
        static async Task<bool> Next(int threadId = -1)
        {
            if (_textQueue.TryDequeue(out var text))
            {
                var content = await _client.GetStringAsync(text);
                var localFile = _remoteToLocalMap[text]!;

                int newLinkCount = Scrape2(text, content);
                // While percent-encoded query strings are "allowed" by the file system, I don't like it!
                content = content.Replace("%3Fv=3.2.1", string.Empty); // Local file names are also cleaned with _stripQueryPattern regex

                if (!File.Exists(localFile))
                    await File.WriteAllTextAsync(localFile, content);
                if (threadId > -1) Log($"Thread {threadId} read {text} and found {newLinkCount} new links.", ConsoleColor.Yellow);
                return true;
            }
            else if (_fileQueue.TryDequeue(out var file))
            {
                var localFile = _remoteToLocalMap[file]!;
                if (!File.Exists(localFile))
                {
                    var content = await _client.GetByteArrayAsync(file);
                    await File.WriteAllBytesAsync(localFile, content);
                }
                if (threadId > -1) Log($"Thread {threadId} downloaded {file}.", ConsoleColor.DarkYellow);
                return true;
            }
            return false;
        }

        static readonly Regex _linkPattern = new(@"^((..\/)+|(\/))?([a-z0-9_-]+\/)*([a-zA-Z0-9-.]+)\.([a-zA-Z0-9-]+)(%.*)?$", RegexOptions.Compiled);
        static readonly Regex _stripQueryPattern = new(@"[^%]+", RegexOptions.Compiled);
        static readonly char[] _quotationChars = new[] { '"', '\'' };
        static int Scrape(string remoteLink, string linkContent)
        {
            var basePath = remoteLink.Split('/')[..^1];
            if (basePath.Length == 0) basePath = new[] { string.Empty };

            var quotedStrings = new List<string>();
            var buffer = new StringBuilder(); char? quoteChar = null;
            foreach (var c in linkContent.AsSpan())
            {
                // Start reading the string by remembering the char that opened it
                if (quoteChar == null && _quotationChars.Contains(c))
                    quoteChar = c;
                // Read while we know what char started the string
                else if (quoteChar != null && quoteChar != c)
                    buffer.Append(c);
                // Until we get the same char again
                else if (quoteChar == c)
                {
                    quotedStrings.Add(buffer.ToString());
                    buffer.Clear(); quoteChar = null;
                }
            }

            var counter = 0;
            foreach (var file in quotedStrings.Where(stringIsALinkToFile).Select(theAbsoluteFilePath))
            {
                if (_remoteToLocalMap.TryAdd(file, null))
                {
                    FileInfo localFile = new(Path.Combine(Destination, _stripQueryPattern.Match(file[1..]).Value));
                    localFile.Directory?.Create();
                    _remoteToLocalMap[file] = localFile.FullName;

                    switch (localFile.Extension)
                    {  // note: percent encoded query is stripped by the _stripQueryPattern regex
                        case ".html": case ".css": case ".js":
                            _textQueue.Enqueue(file); break;
                        case ".jpg": case ".ico": case ".eot":
                        case ".woff": case ".ttf": case ".svg":
                            _fileQueue.Enqueue(file); break;
                    }

                    counter++;
                }
            }
            return counter;
            bool stringIsALinkToFile(string s) => _linkPattern.IsMatch(s);
            string theAbsoluteFilePath(string path)
            {
                // path may already be absolute
                if (path.StartsWith('/')) return path;

                var relativeParts = path.Split('/');
                var parents = relativeParts.TakeWhile(p => p == "..").Count();

                // Range operator is really convenient! :D
                return string.Join('/', basePath[..^parents].Concat(relativeParts[parents..]));
            }
        }
        static int Scrape2(string remoteLink, string linkContent)
        {
            var basePath = remoteLink.Split('/')[..^1];
            if (basePath.Length == 0) basePath = new[] { string.Empty };

            var content = linkContent.AsSpan();
            var ranges = Test(content); var counter = 0;
            foreach(var range in ranges) 
            {
                var quotedString = content[range].ToString();
                if(_linkPattern.IsMatch(quotedString))
                {
                    var file = absoluteFilePath(quotedString);
                    if (_remoteToLocalMap.TryAdd(file, null))
                    {
                        FileInfo localFile = new(Path.Combine(Destination, _stripQueryPattern.Match(file[1..]).Value));
                        localFile.Directory?.Create();
                        _remoteToLocalMap[file] = localFile.FullName;

                        switch (localFile.Extension)
                        {  // note: percent encoded query is stripped by the _stripQueryPattern regex
                            case ".html": case ".css": case ".js":
                                _textQueue.Enqueue(file); break;
                            case ".jpg": case ".ico": case ".eot":
                            case ".woff": case ".ttf": case ".svg":
                                _fileQueue.Enqueue(file); break;
                        }

                        counter++;
                    }
                }
            }
            return counter;

            string absoluteFilePath(string path)
            {
                // path may already be absolute
                if (path.StartsWith('/')) return path;

                var relativeParts = path.Split('/');
                var parents = relativeParts.TakeWhile(p => p == "..").Count();

                // Range operator is really convenient! :D
                return string.Join('/', basePath[..^parents].Concat(relativeParts[parents..]));
            }
        }
        static Range[] Test(ReadOnlySpan<char> content)
        {
            var ranges = new List<Range>();
            char? quoteChar = null; var (index, start) = (0, 0);
            foreach (var c in content)
            {
                if (quoteChar == null && _quotationChars.Contains(c))
                {
                    quoteChar = c;
                    start = index + 1;
                }
                else if(quoteChar == c)
                {
                    ranges.Add(start..index);
                    quoteChar = null;
                }
                index++;
            }
            return ranges.ToArray();
        }

        static readonly object _consoleLock = new();
        static void Log(string message, ConsoleColor color)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
        }
    }
}