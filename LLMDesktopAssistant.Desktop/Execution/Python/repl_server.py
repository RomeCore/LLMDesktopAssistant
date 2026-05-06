import sys

sys.stdout.reconfigure(encoding="utf-8")
sys.stderr.reconfigure(encoding="utf-8")

import json
import io

_original_stdout = sys.stdout

def send_response(type, data):
	response = json.dumps({"type": type, "data": data})
	_original_stdout.write(response + "\n")
	_original_stdout.flush()

while True:
	try:
		line = sys.stdin.readline()
		if not line:
			break
		
		command = json.loads(line)
		action = command.get("action")
		
		if action == "execute":
			captured = io.StringIO()
			sys.stdout = captured
			
			try:
				exec(command["code"], globals())
				output = captured.getvalue()
				send_response("result", {
					"output": output,
					"status": "ok"
				})
			except Exception as e:
				send_response("result", {
					"output": str(e),
					"status": "error"
				})
			finally:
				sys.stdout = _original_stdout
				
		elif action == "get_var":
			name = command["name"]
			try:
				val = eval(name, globals())
				send_response("var", {"name": name, "value": repr(val)})
			except:
				send_response("var", {"name": name, "error": f"Variable '{name}' not found"})
				
		elif action == "set_var":
			name = command["name"]
			code = command["code"]
			exec(f"globals()['{name}'] = {code}", globals())
			send_response("var", {"name": name, "status": "set"})
			
		elif action == "eval":
			try:
				result = eval(command["code"], globals())
				send_response("result", {
					"output": repr(result),
					"status": "ok"
				})
			except Exception as e:
				send_response("result", {
					"output": str(e),
					"status": "error"
				})
				
		elif command["action"] == "reset":
			globals().clear()
			exec("import sys, json, math, re, os, datetime")
			send_response("result", {"output": "Context cleared", "status": "ok"})
			
		elif command["action"] == "ping":
			send_response("pong", {"version": sys.version})
			
	except Exception as e:
		send_response("error", {"message": str(e)})