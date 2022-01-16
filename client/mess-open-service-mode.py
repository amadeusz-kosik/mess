#!/usr/bin/python3

import configparser
import getopt
import sys
import xmlrpc.client


def print_usage():
    print("Starts worker VM for service mode (preparing new snapshot).")
    print("Parameters:")
    print("-m --vm-name MW1                            REQUIRED    name of worker VM")
    print("-a --snapshot-name snapshot name            REQUIRED    name of snapshot to use (if not default one)")
    print()


def main(argv):
    vm_name = ""
    snapshot_name = ""

    config = configparser.ConfigParser()
    config.read(["~/mess-config.ini", "mess-config.ini"])
    proxy_url = config.get("Client", "proxy_url")

    if not proxy_url:
        print("Configuration file not found or proxy_url property missing")
        exit(2)

    try:
        opts, args = getopt.getopt(argv, "m:a:", ["vm-name=", "snapshot-name=", "help"])
    except getopt.GetoptError:
        print_usage()
        sys.exit(2)

    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print_usage()
            sys.exit(0)
        elif opt in ("-m", "--vm-name"):
            vm_name = arg
        elif opt in ("-a", "--snapshot-name"):
            snapshot_name = arg

    if not (vm_name and snapshot_name):
        print("Insufficient parameters (%s %s)\n" % (vm_name, snapshot_name))
        print_usage()
        sys.exit(1)

    mess = xmlrpc.client.ServerProxy(proxy_url)
    mess.open_service_mode(vm_name, snapshot_name)


main(sys.argv[1:])
