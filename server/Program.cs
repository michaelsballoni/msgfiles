using System.Diagnostics;
using msgfiles;

try
{
    Console.WriteLine("Starting up...");
    var server_app = new ServerApp();

    var server = new Server(server_app, server_app.ServerPort);

    var accepter_thread = new Thread(AcceptThread);
    accepter_thread.Start(server);

    Console.WriteLine("Up and running!");

    string docs_dir = server_app.AppDocsDirPath;
    while (true)
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("Commands: home, allow, block, settings, logs, files, quit");

            Console.WriteLine();
            Console.Write("> ");

            string cmd = Console.ReadLine() ?? "";
            if (cmd == null || string.IsNullOrWhiteSpace(cmd))
                continue;

            switch (cmd.Trim().ToLower())
            {
                case "home":
                    ShowFolder(docs_dir);
                    break;

                case "allow":
                    OpenFile(Path.Combine(docs_dir, "allow.txt"));
                    break;

                case "block":
                    OpenFile(Path.Combine(docs_dir, "block.txt"));
                    break;

                case "settings":
                    OpenFile(Path.Combine(docs_dir, "settings.ini"));
                    break;

                case "logs":
                    ShowFolder(Path.Combine(docs_dir, "logs"));
                    break;

                case "files":
                    ShowFolder(server_app.FileStoreDirPath);
                    break;

                case "quit":
                    {
                        Console.WriteLine("Stopping server...");
                        server.Dispose();

                        Console.WriteLine("Waiting on network close...");
                        accepter_thread.Join();

                        Console.WriteLine("Closing up shop...");
                        server_app.Dispose();

                        Console.WriteLine("All done.");
                        return;
                    }

                default:
                    Console.WriteLine($"ERROR: Unknown command: {cmd}");
                    break;
            }
        }
        catch (Exception exp)
        {
            Console.Write($"ERROR: {Utils.SumExp(exp)}");
        }
    }
}
catch (InputException exp)
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: {exp.Message}");
}
catch (Exception exp)
{
    Console.WriteLine();
    Console.WriteLine($"Unhandled EXCEPTION: {exp}");
}

void AcceptThread(object? serverObj)
{
    if (serverObj == null)
        throw new NullReferenceException("serverObj");

    var server = (Server)serverObj;
    try
    {
        server.Accept();
    }
    catch (Exception exp)
    {
        Console.WriteLine();
        Console.WriteLine($"Accepter ERROR: {exp}");
        Environment.Exit(1);
    }
}

void ShowFolder(string dirPath)
{
    Console.WriteLine($"Showing folder: {dirPath}");
    ProcessStartInfo si = new ProcessStartInfo("explorer.exe", dirPath);
    Process.Start(si);
}

void OpenFile(string filePath)
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"File does not exist: {filePath}");
        Console.WriteLine("Creating file...");
        File.WriteAllText(filePath, "");
    }

    Console.WriteLine($"Opening file: {filePath}");
    ProcessStartInfo si = new ProcessStartInfo();
    si.FileName = filePath;
    si.UseShellExecute = true;
    si.Verb = "open";
    Process.Start(si);
}
