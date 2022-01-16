import time
import urllib.request


class Supervisor:

    def __init__(self, ip, port):
        base_url = "http://%s:%s/mess-supervisor/" % (ip, port)

        self.start_vm_url = base_url + "%s/start"
        self.status_vm_url = base_url + "%s/status"
        self.stop_vm_url = base_url + "%s/stop"
        self.apply_snapshot_url = base_url + "%s/snapshot/%s/apply"
        self.create_snapshot_url = base_url + "%s/snapshot/%s/create"

    def start_vm(self, vm_name, snapshot_name):
        operation_result = urllib.request.urlopen(self.apply_snapshot_url % (vm_name, snapshot_name)).read().strip()
        status_url = self.status_vm_url % vm_name

        if operation_result != b'OK':
            raise SupervisorError("Failed to load snapshot for %s" % vm_name)

        for is_saved in (urllib.request.urlopen(status_url).read(32).decode("utf-8")):
            if is_saved == "Saved":
                break

            time.sleep(1)

        operation_result = urllib.request.urlopen(self.start_vm_url % vm_name).read().strip()

        if operation_result != b'OK':
            raise SupervisorError("Failed to start VM %s" % vm_name)

        for is_running in (urllib.request.urlopen(status_url).read(32).decode("utf-8")):
            if is_running == "Running":
                break

            time.sleep(1)

    def stop_vm(self, vm_name):
        operation_result = urllib.request.urlopen(self.stop_vm_url % vm_name).read().strip()
        status_url = self.status_vm_url % vm_name

        if operation_result != b'OK':
            raise SupervisorError("Failed to stop VM %s (%s)" % vm_name)

        while urllib.request.urlopen(self.status_vm_url % vm_name).read().strip() != b'Off':
            time.sleep(1)

        for is_saved in (urllib.request.urlopen(status_url).read(32).decode("utf-8")):
            if is_saved == "Saved":
                break

            time.sleep(1)

    def create_snapshot(self, vm_name, snapshot_name):
        operation_result = urllib.request.urlopen(self.create_snapshot_url % (vm_name, snapshot_name)).read().strip()

        if operation_result != b'OK':
            raise SupervisorError("Failed to create snapshot %s for %s: " % (snapshot_name, vm_name))


class SupervisorError(Exception):
    def __init__(self, value):
        self.value = value

    def __str__(self):
        return repr(self.value)
