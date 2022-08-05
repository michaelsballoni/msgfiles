// FORNOW - Have server app hand out port and name from settings.ini
int port = 9914;
string hostname = "msgfiles";

var server_app = new msgfiles.ServerApp();
var server = new msgfiles.Server(server_app, port, hostname);

server.Accept();
