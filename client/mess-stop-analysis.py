#!/usr/bin/python3

import configparser
import getopt
import sys
import urllib.request
import xmlrpc.client


def print_usage():
    print("Stops current sample analysis on the VH worker VM and downloads it's results.")
    print("Parameters:")
    print("-f --force                                  OPTIONAL    skip results downloading and force VM to stop")
    print("-m --vm-name MW1                            REQUIRED    name of worker VM")


def main(argv):
    force_stop = False
    vm_name = ""

    config = configparser.ConfigParser()
    config.read(["~/mess-config.ini", "mess-config.ini"])
    proxy_url = config.get("Client", "proxy_url")
    results_url = config.get("Client", "results_url")

    if not proxy_url:
        print("Configuration file not found or proxy_url property missing")
        exit(2)

    try:
        opts, args = getopt.getopt(argv, "hfm:", ["help", "force", "vm-name="])
    except getopt.GetoptError:
        print_usage()
        sys.exit(1)

    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print_usage()
            sys.exit(0)
        elif opt in ("-m", "--vm-name"):
            vm_name = arg
        elif opt in ("-f", "--force"):
            force_stop = True

    if not vm_name:
        print_usage()
        sys.exit(1)

    vh = xmlrpc.client.ServerProxy(proxy_url)
    result_file_name = vh.stop_analysis(vm_name, force_stop)
    if not force_stop:
        result_file_url = "%s/%s" % (results_url, result_file_name)
        urllib.request.urlretrieve(result_file_url, result_file_name)


main(sys.argv[1:])
