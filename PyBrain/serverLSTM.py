import time
import socket
import BaseHTTPServer
import json
import random
import sys
import os.path
from   six.moves   import cPickle
from   urlparse    import urlparse, parse_qs
from   serviceLSTM import NN

HOST_NAME = 'localhost' # !!!REMEMBER TO CHANGE THIS!!!
PORT_NUMBER = None
lstm = None

    
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
            modelFileName = 'dumpModels/{}_{}.save'.format(data['symbol'], data['chunkIndex'])
			
            if (os.path.isfile(modelFileName)):
                fileToOpen = open(modelFileName, 'rb')
                lstm = NN( nEpochs = int(sys.argv[2]) )
                lstm.__setstate__(cPickle.load(fileToOpen))
                lstm.cTor(False)
                fileToOpen.close()
            else:
                lstm = NN( nEpochs = int(sys.argv[2]) )
                lstm.cTor(True)
                lstm.train(inData = data['input'], outData = data['target'])
                # save the LSTM Model
                fileToSave = open(modelFileName, 'wb')
                cPickle.dump(lstm.__getstate__(), fileToSave, protocol=cPickle.HIGHEST_PROTOCOL)
                fileToSave.close()

            lstm.test(data['symbol'])				
            s.wfile.write(lstm.test(data['symbol']))
        else:
            s.wfile.write(lstm.predict(inData = data['input']))
        return

if __name__ == '__main__':
    print '\n\n\nServerLSTM - Hello!'
    sys.setrecursionlimit(10000)
    server_class = BaseHTTPServer.HTTPServer
    PORT_NUMBER = int(sys.argv[1])
    httpd = server_class((HOST_NAME, PORT_NUMBER), MyHandler)
    print time.asctime(), "Server Starts - %s:%s" % (HOST_NAME, int(sys.argv[1]))
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    print time.asctime(), "Server Stops - %s:%s" % (HOST_NAME, PORT_NUMBER)