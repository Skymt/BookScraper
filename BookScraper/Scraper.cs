using System.Collections.Concurrent;

namespace BookScraper
{
    internal static class Scraper
    {
        public static string Destination = @"C:\Temp";
        static readonly ConcurrentQueue<string> _workload = new();
        static readonly ConcurrentDictionary<string, bool> _process = new();
        static readonly HttpClient _client = new() { BaseAddress = new("http://books.toscrape.com/") };
        static readonly string[] _fileExtensions = new[] { ".html", ".css", ".js" };
        static readonly string[] _imageExtension = new[] { ".jpg", ".ico" };

        /// <summary>
        /// Scrape index.html and return the amount of "interesting" links.
        /// </summary>
        /// <remarks>
        /// Calling this is optional. <see cref="Process(int)"/> will work regardless.
        /// </remarks>
        /// <returns>The initial amount of files that should be processed</returns>
        public static async Task<int> Init()
        {
            _workload.Enqueue("index.html");
            await ProcessWorkItem(-1);
            return _workload.Count;
        }
        
        /// <summary>
        /// Start threaded processing of the workload.
        /// </summary>
        /// <remarks>
        /// If <see cref="Init"/> has not been called, root index.html will be evaluated as first item!
        /// </remarks>
        /// <param name="threads">The amount of threads to do work.</param>
        public static void Process(int threads = 1)
        {
            if (!_workload.Any())
            {
                _workload.Enqueue("index.html");
                ProcessWorkItem().Wait();
            }

            int currentCompleted = 1;
            using var _ = new Timer(_ =>
            {
                var newCurrentCompleted = _process.Where(kvp => kvp.Value).Count();
                if (newCurrentCompleted != currentCompleted)
                {
                    currentCompleted = newCurrentCompleted;
                    Log($"Current workload: {_workload.Count:0000}, Completed: {currentCompleted}", ConsoleColor.Red);
                }
            }, null, 50, 1000);

            Task.WaitAll(Enumerable.Range(1, threads).Select(worker).ToArray());
            Console.ForegroundColor = ConsoleColor.White;
            return;

            static async Task worker(int threadId)
            {
                int retries = 3;
                do
                {
                    if (await ProcessWorkItem(threadId)) 
                        retries = 3;
                    else
                    {
                        Log($"No work found for thread {threadId}... retrying ({retries})", ConsoleColor.DarkGreen);
                        await Task.Delay(500);
                        retries--;
                    }
                } while(retries > 0);
                Log($"Thread {threadId} exited due to lack of work.", ConsoleColor.Green);
            }
        }

        static async Task<bool> ProcessWorkItem(int threadId = 0)
        {
            if (_workload.TryDequeue(out var filePath))
            {
                FileInfo localFile = new(Path.Combine(Destination, filePath));
                localFile.Directory?.Create();

                if (_fileExtensions.Contains(localFile.Extension)) await DownloadFile(filePath, localFile.FullName);
                else if (_imageExtension.Contains(localFile.Extension)) await DownloadImage(filePath, localFile.FullName);
                Log($"Thread {threadId} processed {filePath}", ConsoleColor.White);
                return _process[filePath] = true;
            }
            return false;
        }
        static async Task DownloadFile(string filePath, string localFilePath)
        {
            var content = await _client.GetStringAsync(filePath);
            await File.WriteAllTextAsync(localFilePath, content);
            Scrape(filePath, content);
        }
        static async Task DownloadImage(string filePath, string localFilePath)
        {
            var content = await _client.GetByteArrayAsync(filePath);
            await File.WriteAllBytesAsync(localFilePath, content);
        }
        static void Scrape(string filePath, string fileContent)
        {
            int newJobsCounter = 0;
            foreach (var quotedString in scanForQuotedStrings())
            {
                if (isValidFile(quotedString))
                {
                    var mergedPath = mergePath(quotedString);
                    if (!_process.ContainsKey(mergedPath) && _process.TryAdd(mergedPath, false))
                    {
                        _workload.Enqueue(mergedPath);
                        newJobsCounter++;
                    }
                }
            }
            if (newJobsCounter > 0) Log($"Added {newJobsCounter} file(s) to process!", ConsoleColor.Yellow);
            return;

            IEnumerable<string> scanForQuotedStrings()
            {
                var buffer = string.Empty; var reading = false;
                foreach (var c in fileContent)
                {
                    if (c == '"')
                    {
                        if (reading) 
                        { 
                            yield return buffer; 
                            buffer = string.Empty; 
                        }
                        reading = !reading;
                    }
                    else if (reading) buffer += c;
                }
            }
            static bool isValidFile(string s)
            {
                if (s.Contains("//")) return false;
                if (s.Contains(' ')) return false;
                if (!s.Contains('.')) return false;

                var extension = s[s.LastIndexOf('.')..];
                if (_fileExtensions.Contains(extension)) return true;
                if (_imageExtension.Contains(extension)) return true;
                return false;
            }
            string mergePath(string s)
            {
                if (s.StartsWith('/')) return s;
                if (!filePath.Contains('/')) return s;

                var basePath = filePath.Split('/');
                var relativePath = s.Split('/');
                var parents = relativePath.TakeWhile(p => p == "..").Count();

                var newPath = basePath[..^(parents + 1)].Concat(relativePath[parents..]);
                return string.Join('/', newPath);
            }
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