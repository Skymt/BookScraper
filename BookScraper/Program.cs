using System.Diagnostics;

Console.WriteLine("Initializing...");
BookScraper.Scraper.Destination = @"D:\Temp";
int initialBatch = await BookScraper.Scraper.Init();

Console.ForegroundColor= ConsoleColor.Green;
Console.WriteLine($"Index.html scraped, found {initialBatch} initial files to process. Press any key to start!");
Console.CursorVisible = false; Console.ReadKey(true);

BookScraper.Scraper.Process(threads: 16);
Console.WriteLine("Processing done! Open scraped site? (Y/N)");

if(Console.ReadKey(true).Key == ConsoleKey.Y)
    Process.Start("explorer.exe", Path.Combine(BookScraper.Scraper.Destination, "index.html"));
