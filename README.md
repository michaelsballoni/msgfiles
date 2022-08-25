# msgfiles
A file delivery system, somewhere between email and FTP

The source code is here and open for contributions

Visit [msgfiles.io](https://msgfiles.io) to get the client and server and read more about the project

## Code Structure
There are two applications, client and server.  These projects have very little code in them, just top-level orchestration.

### securenet
This library includes all 3rd party dependencies, including ZIP, AES, and JSON.  It also includes the core TLS code, including self-signed certificate generation and SslStream wrapper functions.  Many of the core building blocks, like the SMTP wrapper class EmailClient, and the session management class SessionStore, are also here.

### msglib
This library implements the message processing in client-side MsgClient and server-side MsgRequestHandler.cs classes.  The core MessageStore class is also here.

### client
A basic proof-of-concept WinForms application that responds to MsgClient events to displays progress and prompt the user for tokens and confirmation.  I imagine sexier applications to take the place of this program; hopefully they will be at least as simple and easy to use as this humble beginning.

### server
Command-line application for running the show on the server side.  The server relies on an INI file and allow and block list files.  The command line prompt gives easy access to these files, and the server picks up changes and puts them into effect immediately.
