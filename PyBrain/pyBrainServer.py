import time
import socket
import BaseHTTPServer
import json
import random
from   urlparse import urlparse, parse_qs
from   pyBrain  import NN

HOST_NAME = 'localhost' # !!!REMEMBER TO CHANGE THIS!!!
PORT_NUMBER = 4567 # Maybe set this to 9000.
elmanInstance = None;

    
class MyHandler(BaseHTTPServer.BaseHTTPRequestHandler):
    def do_HEAD(s):
        s.send_response(200)
        s.send_header("Content-type", "text/html")
        s.end_headers()
    def do_GET(s):
        """Respond to a GET request."""
        s.send_response(200)
        s.send_header("Content-type", "text/html")
        s.end_headers()
        query_components = parse_qs(urlparse(s.path).query)
        if (s.path.startswith("/train")):
            print query_components['data']
            elmanInstance.Train(dataSet = query_components['data'])
        else:
            print "else!!!!!"
            s.wfile.write("defualt")

    def do_POST(s):
        s._set_headers()
        s.send_response(200)
        s.end_headers()
        print "in post method"
        s.data_string = s.rfile.read(int(s.headers['Content-Length']))
        data = simplejson.loads(s.data_string)
        #length = int(s.headers['Content-Length'])
        #post_data = urlparse.parse_qs(s.rfile.read(length).decode('utf-8'))
        #data = json.loads(post_data['json'][0])
        if (s.path.startswith("/train")):
            print data
            elmanInstance.Train(dataSet = data)
        else:
            print "else!!!!!"
            s.wfile.write("defualt")
        return

if __name__ == '__main__':
    elmanInstance = NN()
    server_class = BaseHTTPServer.HTTPServer
    httpd = server_class((HOST_NAME, PORT_NUMBER), MyHandler)
    print time.asctime(), "Server Starts - %s:%s" % (HOST_NAME, PORT_NUMBER)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    print time.asctime(), "Server Stops - %s:%s" % (HOST_NAME, PORT_NUMBER)