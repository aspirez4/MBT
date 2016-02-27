import time
import socket
import BaseHTTPServer
import json
import random
import sys
from   urlparse import urlparse, parse_qs
from   pyBrain  import NN

HOST_NAME = 'localhost' # !!!REMEMBER TO CHANGE THIS!!!
elmanInstance = None;

    
class MyHandler(BaseHTTPServer.BaseHTTPRequestHandler):
    def do_HEAD(s):
        s.send_response(200)
        s.send_header("Content-type", "text/html")
        s.end_headers()
    def do_GET(s):
        s.send_response(200)
        s.send_header("Content-type", "text/html")
        s.end_headers()
        query_components = parse_qs(urlparse(s.path).query)
        if (s.path.startswith("/train")):
            print query_components['data']
            elmanInstance.Train(dataSet = query_components['data'])
        else:
            s.wfile.write("defualt")

    def do_POST(s):
        s.send_response(200)
        s.end_headers()
        data = json.loads(s.rfile.read(int(s.headers['Content-Length'])))
        if (s.path.startswith("/train")):
            s.wfile.write(elmanInstance.Train(dataSet = data['dataSet']))
        else:
            s.wfile.write(elmanInstance.Predict(input = data['input']))
        return

if __name__ == '__main__':
    elmanInstance = NN()
    server_class = BaseHTTPServer.HTTPServer
    httpd = server_class((HOST_NAME, int(sys.argv[1])), MyHandler)
    print time.asctime(), "Server Starts - %s:%s" % (HOST_NAME, int(sys.argv[1]))
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    print time.asctime(), "Server Stops - %s:%s" % (HOST_NAME, PORT_NUMBER)