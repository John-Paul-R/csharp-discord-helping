
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Octokit;
using FileMode = System.IO.FileMode;

namespace csharp_discord_helping;

public static class Installer
{
    private static async Task DownloaderAsync(string url, string path)
    {
        HttpClient httpClient = new HttpClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("user-agent", "foo");
        var results = await httpClient.SendAsync(requestMessage);
        FileStream fileStream = new FileStream(path, FileMode.Create);
        await results.Content.CopyToAsync(fileStream);
        fileStream.Close();
        fileStream.Dispose();
        requestMessage.Dispose();
        results.Dispose();
    }

    private static async Task<HttpResponseMessage> DownloaderAsync(string url)
    {
        HttpClient httpClient = new HttpClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("user-agent", "foo");
        return await httpClient.SendAsync(requestMessage);
    }

    public static async Task Main(string[] args)
    {
        string temp = "";
        string branch = "";
        if (args.Length == 0)
        {
            branch = "Stable";
        }
        else
        {
            branch = args[0].ToString();
        }

        // verify branch is empty or is valid or not
        List<string> branches = ["Stable", "Canary", "PTB"];
        if (!branches.Contains(branch))
        {
            throw new Exception("The branch is not valid. valid branches are : [ Stable , Canary ,  PTB ]");
        }

        if (branch == "Stable") { branch = ""; }

        //retrieve system information & get ready variables
        string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

        string Platform = "";
        if (OperatingSystem.IsLinux())
        {
            Platform = "linux";
        }
        else if (OperatingSystem.IsMacOS())
        {
            Platform = "darwin";
        }
        else if (OperatingSystem.IsWindows())
        {
            Platform = "win";
        }
        else { throw new Exception("OS is Not Supported"); }
        //gets temp folder loaction & verify that discord exists, if not throw an exception
        // add does other platform specific stuff
        switch (Platform)
        {
            case "win":
                {
                    temp = Path.GetTempPath();
                    if (!(Directory.Exists(Environment.GetEnvironmentVariable("localappdata") + @"\Discord" + branch)))
                    {
                        throw new Exception("Discord " + branch + " is not installed");
                    }
                    break;

                }
            case "darwin":
                {
                    temp = "~/Library/Caches";
                    if (!(Directory.Exists("/Library/Application Support/Discord" + branch)))
                    {
                        throw new Exception("Discord " + branch + " is not installed");
                    }
                    break;
                }
            case "linux":
                {
                    temp = "/tmp";
                    try
                    {
                        Process discord = new Process();
                        discord.StartInfo.UseShellExecute = true;
                        discord.StartInfo.FileName = "discord" + branch;
                        discord.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        discord.Start();
                        discord.Kill();
                        discord.WaitForExit();
                        discord.Dispose();
                    }
                    catch
                    {
                        throw new Exception("Discord " + branch + " is not installed");
                    }
                    break;
                }
        };
        Console.WriteLine("Your OS is: " + Platform);
        Console.WriteLine("Your Architecture type is: " + arch);
        Console.WriteLine("Your Temp folder is: " + temp);
        //download source code and dependencies
        string workspaceName = "BetterDiscord";
        String repositoryName = "BetterDiscord";

        var client = new GitHubClient(new ProductHeaderValue(repositoryName));

        // Retrieve a List of Releases in the Repository, and get latest zipball
        var releases = await client.Repository.Release.GetLatest(workspaceName, repositoryName);
        await DownloaderAsync(releases.ZipballUrl, Path.Combine(temp + "betterdiscord.zip"));
        Console.WriteLine("Downloaded BD");

        ZipFile.ExtractToDirectory(Path.Combine(temp + "betterdiscord.zip"), temp, true);
        Console.WriteLine(temp + @"BetterDiscord-BetterDiscord*");
        Console.WriteLine(string.Join("\n", Directory.GetDirectories(temp, @"BetterDiscord-BetterDiscord*")));
        string bddir = Directory.GetDirectories(temp, @"BetterDiscord-BetterDiscord*")[0];
        Console.WriteLine("Extracted BD");


        //detect node
        var process = Process.Start(new ProcessStartInfo()
        {
            FileName = "node",
            Arguments = "--version",
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            //set up node
            Console.WriteLine("Node.js is not installed!");
            const string nodebaseurl = @"https://nodejs.org/dist/latest/";
            var results = await DownloaderAsync(nodebaseurl);
            List<string> hrefTags = new List<string>();
            var parser = new HtmlParser();
            var document = parser.ParseDocument(await results.Content.ReadAsStringAsync());
            foreach (IElement element in document.QuerySelectorAll("a"))
            {
                hrefTags.Add(element.GetAttribute("href"));
            }
            string AchiveExt = "";
            if (Platform == "win") { AchiveExt = ".zip"; }
            else { AchiveExt = ".tar.xz"; }
            var myRegex = new Regex("^.*-" + Platform + "-" + arch + AchiveExt + "$");
            string Nodever = hrefTags.Where(x => myRegex.IsMatch(x)).ToList()[0];
            Console.WriteLine("The NodeJS version that will be downloaded is: " + Nodever);
            results.Dispose();
            string execExt = "";

            await DownloaderAsync(nodebaseurl + Nodever, Path.Combine(temp + "node" + AchiveExt));
            if (Platform == "win")
            {
                ZipFile.ExtractToDirectory(Path.Combine(temp + "node.zip"), temp, true);
                execExt = ".exe";
            }
            else
            {
                TarFile.ExtractToDirectory(Path.Combine(temp + "node.tar.xz"), temp, true);
            }

            Console.WriteLine("Extracted NodeJS");
            Console.WriteLine("Installing pnpm");
            Directory.SetCurrentDirectory(bddir);
            var node = Nodever.Replace(AchiveExt, "");
            string Nodedir = Path.Combine(temp + node);
            process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = Nodedir + "/npm" + execExt;
            process.StartInfo.WorkingDirectory = bddir;
            process.StartInfo.Arguments = "i pnpm -g";
            process.StartInfo.WorkingDirectory = Nodedir;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            Console.WriteLine("Installing dependencies");
            await process.WaitForExitAsync();
            process.StartInfo.FileName = "pnpm";
            process.StartInfo.Arguments = "install";
            process.Start();
            Console.WriteLine("Building");
            await process.WaitForExitAsync();
            process.StartInfo.Arguments = "build";
            process.Start();
            Console.WriteLine("Installing");
            await process.WaitForExitAsync();
            process.StartInfo.Arguments = "inject" + branch;
            process.Start();
            await process.WaitForExitAsync();
            Console.WriteLine("Done");


        }
        else
        {
            Console.WriteLine("Installing pnpm");
            process = new Process();
            process.StartInfo.FileName = "npm";
            process.StartInfo.WorkingDirectory = bddir;
            process.StartInfo.Arguments = "i pnpm -g";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.UseShellExecute = true;
            process.Start();
            Console.WriteLine("Installing dependencies");
            await process.WaitForExitAsync();
            process.StartInfo.FileName = "pnpm";
            process.StartInfo.Arguments = "install";
            process.Start();
            Console.WriteLine("Building");
            await process.WaitForExitAsync();
            process.StartInfo.Arguments = "build";
            process.Start();
            Console.WriteLine("Installing");
            await process.WaitForExitAsync();
            process.StartInfo.Arguments = "inject" + branch;
            process.Start();
            await process.WaitForExitAsync();
            Console.WriteLine("Done");
        }
    }
}
