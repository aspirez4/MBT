import time
import socket
import BaseHTTPServer
import json
import random
import sys
from   urlparse    import urlparse, parse_qs
from   serviceLSTM import NN

HOST_NAME = 'localhost' # !!!REMEMBER TO CHANGE THIS!!!
lstm = None;

    
class MyHandler(BaseHTTPServer.BaseHTTPRequestHandler):
    def do_HEAD(s):
        s.send_response(200)
        s.send_header("Content-type", "text/html")
        s.end_headers()

    def do_POST(s):
        global lstm
        s.send_response(200)
        s.end_headers()
        data = json.loads(s.rfile.read(int(s.headers['Content-Length'])))
        if (s.path.startswith("/train")):
            lstm = NN( nEpochs = int(sys.argv[2]) )
            lstm.train(inData = data['input'], outData = data['target'])
            s.wfile.write(lstm.test())
        else:
            s.wfile.write(lstm.predict(inData = data['input']))

        return

if __name__ == '__main__':
    print '\n\n\nServerLSTM - Hello!'
    server_class = BaseHTTPServer.HTTPServer
    httpd = server_class((HOST_NAME, int(sys.argv[1])), MyHandler)
    print time.asctime(), "Server Starts - %s:%s" % (HOST_NAME, int(sys.argv[1]))
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    print time.asctime(), "Server Stops - %s:%s" % (HOST_NAME, PORT_NUMBER)