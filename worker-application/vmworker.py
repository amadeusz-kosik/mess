import os.path
import subprocess
import time
import urllib.request
import xmlrpc.client
import xmlrpc.server


class WorkerRPCAPI:
    def __init__(self):
        self.current_malware = None

        mess_path = "C:/MESS/Worker/"
        self.malware_path = "%s/Malware" % mess_path
        self.results_path = "%s/Results" % mess_path
        self.toolkit_path = "%s/Toolkit" % mess_path

        svc_url = "http://localhost:5012/mess-worker"
        self.start_svc_url = "%s/analysis/start" % svc_url
        self.stop_svc_url = "%s/analysis/stop" % svc_url
        self.status_svc_url = "%s/analysis/isEnabled" % svc_url
        self.disable_svc_url = "%s/analysis/disable" % svc_url

        print("Checking if there is a working analysis.")
        if urllib.request.urlopen(self.status_svc_url).read(32).decode("utf-8") == "True":
            if os.path.isfile("%s/post-restart.ps1" % self.toolkit_path):
                subprocess.call(["powershell", "%s/post-restart.ps1" % self.toolkit_path])

    def start_analysis(self, malware_file_name, malware_file_call, malware_file_data, toolkit_file_data):
        malware_file_path = self.malware_path

        with open("%s/%s" % (self.malware_path, malware_file_name), "wb") as file_handle:
            file_handle.write(malware_file_data.data)

        with open("%s/toolkit.zip" % self.toolkit_path, "wb") as file_handle:
            file_handle.write(toolkit_file_data.data)

        parsed_call = [ch.replace("!mpath", malware_file_path) for ch in malware_file_call]

        print("Running: %s (raw: %s)" % (list(parsed_call), list(malware_file_call)))

        subprocess.call(["7z", "x", "-y", "-o%s/" % self.toolkit_path, "%s/toolkit.zip" % self.toolkit_path])

        if os.path.isfile("%s/before.ps1" % self.toolkit_path):
            subprocess.call(["powershell", "%s/before.ps1" % self.toolkit_path])

        urllib.request.urlopen(self.start_svc_url).read(32).decode("utf-8")

        time.sleep(5)
        self.current_malware = subprocess.Popen(parsed_call,
                                                stdin=None, stdout=None, stderr=None, close_fds=True, shell=False)
        return True

    def stop_analysis(self):

        if self.current_malware and (self.current_malware.poll() is None):
            print("Killing in the name of")
            self.current_malware.kill()
            self.current_malware = None

        urllib.request.urlopen(self.stop_svc_url)
        urllib.request.urlopen(self.disable_svc_url)

        if os.path.isfile("%s/after.ps1" % self.toolkit_path):
                subprocess.call(["powershell", "%s/after.ps1" % self.toolkit_path])

        time.sleep(5)
        subprocess.call(["7z", "a", "-y", "-tzip", "-mx3",
                         "%s/result.zip" % self.results_path, "%s/*" % self.results_path])
        time.sleep(5)

        return True

    def restart_vm(self):
        urllib.request.urlopen(self.stop_svc_url)

        if os.path.isfile("%s/pre-restart.ps1" % self.toolkit_path):
                subprocess.call(["powershell", "%s/pre-restart.ps1" % self.toolkit_path])

        subprocess.Popen(["shutdown", "/r", "/t", "5"])
        return True


class WorkerRPCService:
    def __init__(self, endpoint):
        self.rpc = xmlrpc.server.SimpleXMLRPCServer(endpoint)
        self.rpc.register_introspection_functions()
        self.rpc.register_multicall_functions()
        self.rpc.register_instance(WorkerRPCAPI())

    def run(self):
        try:
            self.rpc.serve_forever()
        except KeyboardInterrupt:
            print("Stopped RPC service")

if __name__ == "__main__":
    service = WorkerRPCService(("", 5501))
    print("Starting RPC Service...")
    service.run()

