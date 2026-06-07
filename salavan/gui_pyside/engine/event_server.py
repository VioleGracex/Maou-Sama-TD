import os
import json
import socket
import threading
import uuid
import time
from datetime import datetime
from http.server import BaseHTTPRequestHandler, HTTPServer
from PySide6.QtCore import QThread, Signal


# ─────────────────────────────────────────────────────────────
#  HTML template for the web dashboard (served at :9191)
# ─────────────────────────────────────────────────────────────
_HTML_PAGE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Salavan UDP Log</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    background: #0f0f13;
    color: #e2e2e8;
    font-family: 'Consolas', 'Courier New', monospace;
    font-size: 13px;
  }
  header {
    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
    border-bottom: 1px solid #2d2d44;
    padding: 16px 24px;
    display: flex;
    align-items: center;
    gap: 16px;
    position: sticky;
    top: 0;
    z-index: 10;
  }
  header h1 { font-size: 16px; color: #a78bfa; letter-spacing: 1px; }
  .badge {
    padding: 3px 10px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: bold;
  }
  .badge-green { background: #052e16; color: #34d399; border: 1px solid #065f46; }
  .badge-blue  { background: #0c1a3a; color: #60a5fa; border: 1px solid #1d4ed8; }
  .badge-gray  { background: #1c1c27; color: #94a3b8; border: 1px solid #334155; }
  .spacer { flex: 1; }
  #status-dot {
    width: 8px; height: 8px;
    border-radius: 50%;
    background: #34d399;
    box-shadow: 0 0 6px #34d399;
    display: inline-block;
    margin-right: 4px;
  }
  #status-dot.stale { background: #f59e0b; box-shadow: 0 0 6px #f59e0b; }

  .meta-bar {
    display: flex;
    gap: 12px;
    padding: 10px 24px;
    background: #13131c;
    border-bottom: 1px solid #1e1e2e;
    flex-wrap: wrap;
  }
  .meta-item { color: #6b7280; font-size: 11px; }
  .meta-item span { color: #c4b5fd; }

  table {
    width: 100%;
    border-collapse: collapse;
  }
  thead th {
    background: #1a1a2e;
    color: #64748b;
    font-size: 10px;
    font-weight: bold;
    letter-spacing: 1px;
    text-transform: uppercase;
    padding: 8px 16px;
    text-align: left;
    position: sticky;
    top: 57px;
    border-bottom: 1px solid #2d2d44;
  }
  tbody tr {
    border-bottom: 1px solid #1a1a24;
    transition: background 0.1s;
  }
  tbody tr:hover { background: #1a1a2e; }
  tbody tr.new-row { animation: flash 0.8s ease-out; }
  @keyframes flash {
    0%   { background: #1e2a1a; }
    100% { background: transparent; }
  }
  td { padding: 7px 16px; vertical-align: top; }
  .col-seq  { color: #475569; width: 52px; }
  .col-time { color: #475569; width: 110px; white-space: nowrap; }
  .col-name { font-weight: bold; }
  .col-data { color: #94a3b8; }

  /* Event type coloring */
  .ev-tile      .col-name { color: #34d399; }
  .ev-ui        .col-name { color: #60a5fa; }
  .ev-scene     .col-name { color: #f59e0b; }
  .ev-error     .col-name { color: #f87171; }
  .ev-default   .col-name { color: #c4b5fd; }

  .empty-state {
    text-align: center;
    color: #374151;
    padding: 80px;
    font-size: 14px;
  }
  .empty-state b { color: #4b5563; }

  #scroll-anchor { height: 1px; }
</style>
</head>
<body>

<header>
  <span id="status-dot"></span>
  <h1>⚡ SALAVAN UDP EVENT LOG</h1>
  <span class="badge badge-blue" id="port-badge">UDP :9090</span>
  <span class="badge badge-gray" id="session-badge">Session: ...</span>
  <div class="spacer"></div>
  <span class="badge badge-green" id="count-badge">0 events</span>
</header>

<div class="meta-bar">
  <div class="meta-item">Server started: <span id="started-at">—</span></div>
  <div class="meta-item">Last event: <span id="last-event-time">—</span></div>
  <div class="meta-item">Refresh: <span id="refresh-rate">1s</span></div>
</div>

<table id="event-table">
  <thead>
    <tr>
      <th class="col-seq">#</th>
      <th class="col-time">Time</th>
      <th class="col-name">Event Name</th>
      <th class="col-data">Data</th>
    </tr>
  </thead>
  <tbody id="tbody">
    <tr><td colspan="4" class="empty-state"><b>Waiting for events…</b><br>Launch the game to see UDP packets arrive here.</td></tr>
  </tbody>
</table>
<div id="scroll-anchor"></div>

<script>
let lastCount = 0;
let startedAt = null;

function evClass(name) {
  const n = name.toLowerCase();
  if (n.includes('tile') || n.includes('placement')) return 'ev-tile';
  if (n.includes('panel') || n.includes('button') || n.includes('ui') || n.includes('ascension')) return 'ev-ui';
  if (n.includes('scene') || n.includes('level') || n.includes('load')) return 'ev-scene';
  if (n.includes('error') || n.includes('fail')) return 'ev-error';
  return 'ev-default';
}

function fmt(ts) {
  const d = new Date(ts * 1000);
  const h = String(d.getHours()).padStart(2,'0');
  const m = String(d.getMinutes()).padStart(2,'0');
  const s = String(d.getSeconds()).padStart(2,'0');
  const ms = String(d.getMilliseconds()).padStart(3,'0');
  return `${h}:${m}:${s}.${ms}`;
}

async function poll() {
  try {
    const r = await fetch('/api/events');
    const data = await r.json();
    const events = data.events || [];
    const info   = data.info   || {};

    document.getElementById('session-badge').textContent = 'Session: ' + (info.session_id || '').slice(0,8);
    document.getElementById('port-badge').textContent = 'UDP :' + (info.udp_port || '9090');
    document.getElementById('count-badge').textContent = events.length + ' event' + (events.length !== 1 ? 's' : '');

    if (!startedAt && info.started_at) {
      startedAt = info.started_at;
      document.getElementById('started-at').textContent = fmt(startedAt);
    }
    if (events.length > 0) {
      const last = events[events.length - 1];
      document.getElementById('last-event-time').textContent = fmt(last.ts);
      document.getElementById('status-dot').className = '';
    }

    if (events.length !== lastCount) {
      const tbody = document.getElementById('tbody');
      tbody.innerHTML = '';
      events.forEach((ev, i) => {
        const tr = document.createElement('tr');
        tr.className = evClass(ev.event_name) + (i >= lastCount ? ' new-row' : '');
        tr.innerHTML = `<td class="col-seq">${ev.seq}</td>
                        <td class="col-time">${fmt(ev.ts)}</td>
                        <td class="col-name">${ev.event_name}</td>
                        <td class="col-data">${ev.event_data || ''}</td>`;
        tbody.appendChild(tr);
      });
      lastCount = events.length;
      document.getElementById('scroll-anchor').scrollIntoView({ behavior: 'smooth' });
    }
  } catch(e) {
    document.getElementById('status-dot').className = 'stale';
  }
  setTimeout(poll, 1000);
}

poll();
</script>
</body>
</html>
"""


class _WebHandler(BaseHTTPRequestHandler):
    """Minimal HTTP handler — serves the dashboard and the JSON API."""

    def log_message(self, fmt, *args):
        # Suppress default access log spam
        pass

    def do_GET(self):
        server: SalavanEventServer = self.server._salavan_ref
        if self.path in ("/", "/index.html"):
            body = _HTML_PAGE.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path == "/api/events":
            with server._log_lock:
                events_snapshot = list(server._event_log)
            payload = {
                "info": {
                    "session_id": server.session_id,
                    "udp_port":   server.port,
                    "started_at": server._started_at,
                },
                "events": events_snapshot,
            }
            body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            self.wfile.write(body)

        else:
            self.send_response(404)
            self.end_headers()


class SalavanEventServer(QThread):
    """
    UDP server that:
      - Receives [Salavan] tagged packets on port 9090 (fallback 9091)
      - Emits event_received(event_name, data) Qt signal
      - Persists every event to <base_dir>/logs/udp_events.jsonl
      - Serves a live web dashboard at http://localhost:9191
    """

    # Emits (event_name, data_str)
    event_received = Signal(str, str)

    def __init__(self, port: int = 9090, web_port: int = 9191, parent=None):
        super().__init__(parent)
        self.port = port
        self.web_port = web_port
        self.running = True
        self.sock = None

        # Session identity — regenerated on each Salavan launch
        self.session_id: str = str(uuid.uuid4())
        self._started_at: float = time.time()

        # In-memory event log (list of dicts) — kept for the full Salavan session
        self._event_log: list = []
        self._log_lock = threading.Lock()
        self._seq = 0

        # Persistent log file path (resolved lazily after QThread starts so
        # get_base_dir() can run in the correct working directory)
        self._log_path: str = ""

        # Web server handle — so we can shut it down cleanly
        self._web_server: HTTPServer | None = None

    # ── public helpers ────────────────────────────────────────

    @property
    def event_log(self) -> list:
        """Thread-safe snapshot of the full event log."""
        with self._log_lock:
            return list(self._event_log)

    # ── lifecycle ─────────────────────────────────────────────

    def stop(self):
        self.running = False
        # Wake up the blocking recvfrom
        if self.sock:
            try:
                dummy = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                dummy.sendto(b"WAKEUP", ("127.0.0.1", self.port))
                dummy.close()
            except Exception:
                pass
            try:
                self.sock.close()
            except Exception:
                pass
        # Stop HTTP server
        if self._web_server:
            try:
                self._web_server.shutdown()
            except Exception:
                pass
        self.wait()

    def run(self):
        self._resolve_log_path()
        self._start_web_server()
        self._run_udp_loop()

    # ── internal ──────────────────────────────────────────────

    def _resolve_log_path(self):
        try:
            from core.paths import get_base_dir
            logs_dir = os.path.join(get_base_dir(), "logs")
            os.makedirs(logs_dir, exist_ok=True)
            self._log_path = os.path.join(logs_dir, "udp_events.jsonl")
        except Exception as e:
            print(f"[SalavanEventServer] Could not resolve log path: {e}")

    def _start_web_server(self):
        try:
            server = HTTPServer(("127.0.0.1", self.web_port), _WebHandler)
            server._salavan_ref = self  # back-reference for the handler
            self._web_server = server
            t = threading.Thread(target=server.serve_forever, daemon=True)
            t.start()
            print(f"[SalavanEventServer] Web dashboard → http://localhost:{self.web_port}")
        except Exception as e:
            print(f"[SalavanEventServer] Could not start web server on port {self.web_port}: {e}")

    def _run_udp_loop(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        bound = False
        for p in [self.port, 9091]:
            try:
                self.sock.bind(("127.0.0.1", p))
                self.port = p
                bound = True
                print(f"[SalavanEventServer] Listening on UDP 127.0.0.1:{p}")
                break
            except Exception as e:
                print(f"[SalavanEventServer] Failed to bind port {p}: {e}")

        if not bound:
            print("[SalavanEventServer] Failed to bind to any UDP port.")
            return

        while self.running:
            try:
                data, addr = self.sock.recvfrom(4096)
                if not self.running:
                    break

                message = data.decode("utf-8", errors="ignore").strip()

                if message == "WAKEUP":
                    continue

                if not message.startswith("[Salavan]"):
                    continue

                content = message[len("[Salavan]"):].strip()
                if ":" in content:
                    parts = content.split(":", 1)
                    event_name = parts[0].strip()
                    event_data = parts[1].strip()
                else:
                    event_name = content
                    event_data = ""

                self._record_event(event_name, event_data, addr)
                self.event_received.emit(event_name, event_data)

            except Exception:
                if not self.running:
                    break

    def _record_event(self, event_name: str, event_data: str, addr):
        ts = time.time()
        with self._log_lock:
            self._seq += 1
            seq = self._seq
            entry = {
                "seq":        seq,
                "ts":         ts,
                "time":       datetime.fromtimestamp(ts).strftime("%H:%M:%S.%f")[:-3],
                "session_id": self.session_id,
                "event_name": event_name,
                "event_data": event_data,
                "from":       f"{addr[0]}:{addr[1]}",
            }
            self._event_log.append(entry)

        # Append to persistent JSONL file
        if self._log_path:
            try:
                with open(self._log_path, "a", encoding="utf-8") as f:
                    f.write(json.dumps(entry, ensure_ascii=False) + "\n")
            except Exception as e:
                print(f"[SalavanEventServer] Failed to write log: {e}")
