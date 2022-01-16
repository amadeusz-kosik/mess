import datetime
import urllib.request
import xmlrpc.client


class VMState:
    ANALYSING = "ANALYSING"
    READY = "READY"
    SERVICE = "SERVICE"


class VMError(Exception):
    def __init__(self, value):
        self.value = value

    def __str__(self):
        return repr(self.value)


class VM:
    def __init__(self, name, ip_address, port, results_path):
        self.name = name
        self.server = xmlrpc.client.ServerProxy("http://%s:%d" % (ip_address, port))
        self.state = VMState.READY
        self.ip_address = ip_address
        self.results_path = results_path

    def get_state(self):
        return self.state

    def open_service_mode(self):
        if self.state != VMState.READY:
            raise VMError("VM not in READY state")
        else:
            self.state = VMState.SERVICE

    def close_service_mode(self):
        if self.state != VMState.SERVICE:
            raise VMError("VM not in SERVICE state")
        else:
            self.state = VMState.READY

    def restart_vm(self):
        if self.state != VMState.ANALYSING:
            raise VMError("VM not in ANALYSING state")
        else:
            self.server.restart_vm()
            return True

    def start_analysis(self, malware_file_name, malware_file_call, malware_file_data, toolkit_file_data):
        if self.state != VMState.READY:
            raise VMError("VM not in READY state")
        else:
            print("Starting analysis on %s (%s)" % (self.name, self.ip_address))
            self.state = VMState.ANALYSING
            self.server.start_analysis(malware_file_name, malware_file_call, malware_file_data, toolkit_file_data)

    def stop_analysis(self, force):
        if self.state != VMState.ANALYSING and force is False:
            raise VMError("VM not in ANALYSING state")
        else:
            self.state = VMState.READY

            if not force:
                self.server.stop_analysis()
                file_name = "%s-%s-result.zip" % (self.name, datetime.datetime.now().strftime("%Y%m%d-%H%M"))
                file_url = "http://%s/result.zip" % self.ip_address
                urllib.request.urlretrieve(file_url, self.results_path + file_name)
                return file_name
            else:
                return "OK"
