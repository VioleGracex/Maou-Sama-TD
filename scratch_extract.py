import os

log_path = os.path.expandvars(r"%LOCALAPPDATA%\Unity\Editor\Editor.log")
results_path = r"d:\OuikiDev\Maou-Sama-TD\scratch_extract_results.txt"

if not os.path.exists(log_path):
    with open(results_path, 'w', encoding='utf-8') as f:
        f.write("Log file does not exist!")
    exit(1)

with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

mcp_lines = []
compile_errors = []
# Scan last 5000 lines
scan_count = min(5000, len(lines))
start_idx = len(lines) - scan_count

for i in range(scan_count):
    idx = start_idx + i
    line = lines[idx]
    lower_line = line.lower()
    if "mcp" in lower_line or "relay" in lower_line or "websocket" in lower_line:
        mcp_lines.append((idx + 1, line.strip()))
    if "error cs" in lower_line or "compilation failed" in lower_line:
        compile_errors.append((idx + 1, line.strip()))

with open(results_path, 'w', encoding='utf-8') as f:
    f.write(f"Total log lines: {len(lines)}\n")
    f.write(f"Scanned last {scan_count} lines (from line {start_idx + 1})\n\n")
    
    f.write("--- MCP/Relay/WebSocket Lines (last 50) ---\n")
    for ln, l in mcp_lines[-50:]:
        f.write(f"Line {ln}: {l}\n")
        
    f.write("\n--- Compile Errors (last 50) ---\n")
    for ln, l in compile_errors[-50:]:
        f.write(f"Line {ln}: {l}\n")

