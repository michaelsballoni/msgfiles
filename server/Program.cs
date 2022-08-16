var server_app = new msgfiles.ServerApp();

var server = new msgfiles.Server(server_app, server_app.ServerPort);

server.Accept();
