import configparser
import glob
import os
import sys
import threading
import time
import xmlrpc.server

from mess.controller import apisupervisor, apivm


class RPCError(Exception):
    def __init__(self, value):
        self.value = value

    def __str__(self):
        return repr(self.value)


class MESSRPCServiceAPI:

    def __init__(self, config):
        self.config = config
        self.results_dir = self.config.get("Controller", "results_dir")

        self.supervisors = {}
        supervisor_names = config.get("Controller", "supervisors").split(",")

        for supervisor_name in supervisor_names:
            ip = self.config.get(supervisor_name, "ip")
            port = int(self.config.get(supervisor_name, "port"))
            self.supervisors[supervisor_name] = apisupervisor.Supervisor(ip, port)

        self.workers = {}
        worker_names = config.get("Controller", "workers").split(",")

        for worker_name in worker_names:
            ip = self.config.get(worker_name, "ip")
            port = int(self.config.get(worker_name, "port"))
            self.workers[worker_name] = {
                'vm': apivm.VM(worker_name, ip, port, self.results_dir),
                'supervisor': self.supervisors[self.config.get(worker_name, "supervisor")],
                'lock': threading.Lock()
            }

        self.default_snapshot_name = config.get("Controller", "default_snapshot_name")

    def _get_vm(self, vm_name):
        if vm_name in self.workers:
            return self.workers[vm_name]
        else:
            raise RPCError("VM %s does not exist" % vm_name)

    def get_vm_state(self, vm_name):
        try:
            return self._get_vm(vm_name)['vm'].get_state()
        except:
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def list_all_vms(self):
        try:
            return ", ".join(self.workers.keys())
        except:
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def open_service_mode(self, vm_name, source_snapshot_name):
        try:
            worker = self._get_vm(vm_name)
            worker['lock'].acquire()
            worker['vm'].open_service_mode()
            worker['supervisor'].start_vm(vm_name, source_snapshot_name)
            worker['lock'].release()

            return "OK"
        except:
            self._get_vm(vm_name)['lock'].release()
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def close_service_mode(self, vm_name, target_snapshot_name):
        try:
            worker = self._get_vm(vm_name)
            worker['lock'].acquire()
            worker['supervisor'].create_snapshot(vm_name, target_snapshot_name)
            worker['supervisor'].stop_vm(vm_name)
            worker['vm'].close_service_mode()
            worker['lock'].release()

            return "OK"
        except:
            self._get_vm(vm_name)['lock'].release()
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def purge_result_files(self, vm_name, keep_number):
        try:
            self._get_vm(vm_name)

            result_files = glob.glob("%s/%s*.zip" % (self.results_dir, vm_name))
            result_files.sort()

            print("Checking old result files from %s" % vm_name)

            if len(result_files) > int(keep_number):
                print("Found %d result files from %s when %s is supposed to keep, removing"
                      % (len(result_files), vm_name, keep_number))

                for result_file in result_files[:-int(keep_number)]:
                    print("Removing result file %s" % result_file)
                    os.unlink(result_file)
            else:
                print("No result files from %s to remove" % vm_name)

            return "OK"
        except:
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def restart_vm(self, vm_name):
        try:
            worker = self._get_vm(vm_name)
            worker['lock'].acquire()
            worker['vm'].restart_vm()
            worker['lock'].release()
            return "OK"
        except:
            self._get_vm(vm_name)['lock'].release()
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def start_analysis(self, vm_name, malware_file_name, malware_file_call, malware_file_data,
                       toolkit_file_data, snapshot_name):
        try:
            if not snapshot_name:
                snapshot_name = self.default_snapshot_name

            print("Trying to start analysis on %s, for malware %s (%s) using snapshot %s"
                  % (vm_name, malware_file_name, list(malware_file_call), snapshot_name))

            worker = self._get_vm(vm_name)
            worker['lock'].acquire()
            print("Preparing VM: %s" % vm_name)
            worker['supervisor'].start_vm(vm_name, snapshot_name)

            time.sleep(5)
            print("Starting Worker on %s" % vm_name)
            worker['vm'].start_analysis(malware_file_name, malware_file_call, malware_file_data, toolkit_file_data)
            worker['lock'].release()

            print("Analysis started on %s" % vm_name)
            return "OK"
        except:
            self._get_vm(vm_name)['lock'].release()
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise

    def stop_analysis(self, vm_name, force):
        try:
            print("Trying to stop analysis on %s" % vm_name)

            worker = self._get_vm(vm_name)
            worker['lock'].acquire()

            self.purge_result_files(vm_name, self.config.get("Controller", "results_cache_size"))

            print("Stopping Worker on %s" % vm_name)
            result = worker['vm'].stop_analysis(force)

            print("Shutting down VM %s" % vm_name)
            worker['supervisor'].stop_vm(vm_name)

            worker['lock'].release()
            return result
        except:
            self._get_vm(vm_name)['lock'].release()
            e = sys.exc_info()[0]
            print("Exception occurred: %s" % str(e))
            raise 


class MESSRPCService:
    def __init__(self):
        config = configparser.ConfigParser()
        config.read(["~/mess-config.ini", "mess-config.ini"])

        endpoint = (config.get("Controller", "listen_address"), int(config.get("Controller", "listen_port")))
        print("Loaded configuration file. Listening on %s:%d" % endpoint)

        self.rpc = xmlrpc.server.SimpleXMLRPCServer(endpoint)
        self.rpc.register_introspection_functions()
        self.rpc.register_multicall_functions()
        self.rpc.register_instance(MESSRPCServiceAPI(config))

    def run(self):
        try:
            self.rpc.serve_forever()
        except KeyboardInterrupt:
            print("Stopped RPC service")


if __name__ == "__main__":
    service = MESSRPCService()
    print("Starting RPC service")
    service.run()

