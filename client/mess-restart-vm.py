#!/usr/bin/python3

import configparser
import getopt
import sys
import xmlrpc.client


def print_usage():
    print("Restart specified VM.")
    print("Parameters:")
    print("-m --vm-name MW1                            REQUIRED    name of worker VM")


def main(argv):
    vm_name = ""

    config = configparser.ConfigParser()
    config.read(["~/mess-config.ini", "mess-config.ini"])
    proxy_url = config.get("Client", "proxy_url")

    if not proxy_url:
        print("Configuration file not found or proxy_url property missing")
        exit(2)

    try:
        opts, args = getopt.getopt(argv, "m:h", ["vm-name=", "help"])
    except getopt.GetoptError:
        print_usage()
        sys.exit(1)

    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print_usage()
            sys.exit(0)
        elif opt in ("-m", "--vm-name"):
            vm_name = arg

    if not vm_name:
        print_usage()
        sys.exit(1)

    mess = xmlrpc.client.ServerProxy(proxy_url)
    print(mess.restart_vm(vm_name))


main(sys.argv[1:])
