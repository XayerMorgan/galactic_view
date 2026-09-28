import os
import sys
import threading
import socket
from http.server import HTTPServer, SimpleHTTPRequestHandler
import webview

def get_base_dir():
    if getattr(sys, 'frozen', False):
        return os.path.dirname(os.path.abspath(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))

BASE_DIR = get_base_dir()

class QuietHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=BASE_DIR, **kwargs)
        
    def log_message(self, format, *args):
        pass

def get_free_port():
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind(('127.0.0.1', 0))
    port = s.getsockname()[1]
    s.close()
    return port

def start_server(port):
    server = HTTPServer(('127.0.0.1', port), QuietHandler)
    server.serve_forever()

def main():
    port = get_free_port()
    t = threading.Thread(target=start_server, args=(port,), daemon=True)
    t.start()
    
    url = f'http://127.0.0.1:{port}/index.html'
    
    window = webview.create_window(
        title='Cosmic Zoom Engine | 3D Universal Scale & Relativistic Compass',
        url=url,
        width=1540,
        height=960,
        min_size=(1100, 720),
        background_color='#02040a',
        text_select=False
    )
    
    webview.start(private_mode=False)

if __name__ == '__main__':
    main()
