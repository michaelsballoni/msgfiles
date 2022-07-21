// FORNOW - Hardcoded port and hostname
int port = 9914;
string hostname = "localhost";

var server_app = new msgfiles.ServerApp();
var server = new msgfiles.Server(server_app, port, hostname);

server.Accept();
