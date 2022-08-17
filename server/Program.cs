try
{
    var server_app = new msgfiles.ServerApp();

    var server = new msgfiles.Server(server_app, server_app.ServerPort);

    server.Accept();
}
catch (msgfiles.InputException exp)
{
    Console.WriteLine();
    Console.WriteLine($"ERROR: {exp.Message}");
}
catch (Exception exp)
{
    Console.WriteLine();
    Console.WriteLine($"Unhandled EXCEPTION: {exp}");
}
