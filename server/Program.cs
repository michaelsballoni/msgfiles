using System.Diagnostics;
using msgfiles;

string docs_dir = ServerApp.AppDocsDirPath;

string settings_file_path = Path.Combine(docs_dir, "settings.ini");
if (!File.Exists(settings_file_path))
{
    Console.WriteLine("settings.ini does not exist");
    string settings_template_file_path = Path.Combine(docs_dir, "settings.template.ini");
    if (!File.Exists(settings_template_file_path))
    {
        Console.WriteLine("settings.template.ini does not exist; must exit");
        return 1;
    }

    Console.WriteLine("Copying settings.template.ini to settings.ini...");
    File.Copy(settings_template_file_path, settings_file_path);

    Console.WriteLine("Hit [Enter] to open settings.ini to fill it in");
    Console.ReadLine();
    var editor = OpenFile(settings_file_path);
    if (editor == null)
    {
        Console.WriteLine("Opening settings.ini file failed; must exit");
        return 1;
    }
    editor.WaitForExit();
}

Console.WriteLine("Starting up...");

ServerApp? server_app = null;
while (true)
{
    try
    {
        server_app = new ServerApp();
        break;
    }
    catch (Exception exp)
    {
        Console.WriteLine();
        Console.WriteLine($"Startup ERROR: {Utils.SumExp(exp)}");

        if (server_app != null)
        {
            try
            {
                server_app.Dispose();
            }
            catch { }
        }

        Console.WriteLine("Hit [Enter] to open settings.ini to improve things");
        Console.ReadLine();
        var editor = OpenFile(settings_file_path);
        if (editor == null)
        {
            Console.WriteLine("Opening settings.ini file failed; must exit");
            return 1;
        }
        editor.WaitForExit();
    }
}

try
{
    Server server = new Server(server_app, server_app.ServerPort);

    var accepter_thread = new Thread(AcceptThread);
    accepter_thread.Start(server);

    Console.WriteLine("Up and running!");

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
                        return 0;
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
    return 1;
}
catch (Exception exp)
{
    Console.WriteLine();
    Console.WriteLine($"Unhandled EXCEPTION: {exp}");
    return 1;
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

Process? OpenFile(string filePath)
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
    return Process.Start(si);
}
