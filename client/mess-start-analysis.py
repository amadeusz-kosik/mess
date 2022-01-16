#!/usr/bin/python3

import configparser
import getopt
import sys
import xmlrpc.client


def print_usage():
    print("Starts sample analysis on the VH worker VM.")
    print("Parameters:")
    print("-s --sample-file-path path/to/sample        REQUIRED    sample file to upload and analyse")
    print("-n --sample-target-name file.exe            REQUIRED    name for sample file on VM worker machine")
    print("-t --toolkit-file-path path/to/toolkit.zip  REQUIRED    additional toolkit file")
    print("-m --vm-name MW1                            REQUIRED    name of worker VM")
    print("-a --snapshot-name snapshot name            OPTIONAL    name of snapshot to use (if not default one)")
    print("-c --command $path/$exec%param1%param2      OPTIONAL    command used to start the sample file")
    print()
    print("For --command option !mname is substituted with sample file name and !mpath is substituted with"
          "sample upload directory.")


def main(argv):
    vm_name = ""
    sample_target_name = ""
    sample_file_path = ""
    toolkit_file_path = ""
    snapshot_name = ""
    command = "!mpath/!mname"

    config = configparser.ConfigParser()
    config.read(["~/mess-config.ini", "mess-config.ini"])
    proxy_url = config.get("Client", "proxy_url")

    if not proxy_url:
        print("Configuration file not found or proxy_url property missing")
        exit(2)

    try:
        opts, args = getopt.getopt(argv, "m:n:s:t:d:ha:c:", ["vm-name=", "sample-target-name=", "sample-file-path=",
                                                             "toolkit-file-path=", "snapshot-name=", "command=",
                                                             "help"])
    except getopt.GetoptError:
        print_usage()
        sys.exit(2)

    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print_usage()
            sys.exit(0)
        elif opt in ("-m", "--vm-name"):
            vm_name = arg
        elif opt in ("-n", "--sample-target-name"):
            sample_target_name = arg
        elif opt in ("-s", "--sample-file-path"):
            sample_file_path = arg
        elif opt in ("-t", "--toolkit-file-path"):
            toolkit_file_path = arg
        elif opt in ("-a", "--snapshot-name"):
            snapshot_name = arg
        elif opt in ("-c", "--command"):
            command = arg

    if not (vm_name and sample_target_name and sample_file_path and toolkit_file_path):
        print("Insufficient parameters (%s %s %s)\n" % (vm_name, sample_target_name, sample_file_path))
        print_usage()
        sys.exit(1)

    with open(sample_file_path, "rb") as f:
        sample_file = xmlrpc.client.Binary(f.read())

    with open(toolkit_file_path, "rb") as f:
        toolkit_file = xmlrpc.client.Binary(f.read())

    command = command.replace("!mname", sample_target_name)

    mess = xmlrpc.client.ServerProxy(proxy_url)
    mess.start_analysis(vm_name, sample_target_name, command.split("%"), sample_file, toolkit_file, snapshot_name)


main(sys.argv[1:])
