#!/usr/bin/env bash

# iptables binary
IT=iptables
ITS=iptables-save

# Internal networks (deny from workers machine)
#  Format: x.x.x.x/netmask
declare -a PRIVATE_NETWORKS=("192.0.0.0/8" "194.0.0.0/8")

#########################################################################
## Setup NAT and firewall
$IT -P INPUT ACCEPT
$IT -P FORWARD ACCEPT
$IT -P OUTPUT ACCEPT

$IT -A INPUT -i lo -j ACCEPT
$IT -A INPUT -m state --state RELATED,ESTABLISHED -j ACCEPT
$IT -A INPUT -m conntrack --ctstate RELATED,ESTABLISHED -j ACCEPT

$IT -A INPUT -s 10.0.1.0/24 -j DROP
$IT -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT
$IT -A INPUT -p tcp -m tcp --dport 80 -j ACCEPT
$IT -A INPUT -p tcp -m tcp --dport 2811 -j ACCEPT
$IT -A INPUT -j DROP

$IT -t nat -A POSTROUTING -o eth0 -j MASQUERADE

for NETWORK in "${PRIVATE_NETWORKS[@]}"
do
    $IT -A FORWARD -d $NETWORK -m state --state NEW -j DROP
done

$IT -A FORWARD -p tcp -m multiport --dports 25,135,139,445 -j DROP

$IT -A FORWARD -m state --state RELATED,ESTABLISHED -j ACCEPT
$IT -A FORWARD -s 10.0.1.0/24 -j ACCEPT
$IT -A OUTPUT -m state --state RELATED,ESTABLISHED -j ACCEPT

service netfilter-persistent save

# Enable NAT in kernel
echo "net.ipv4.ip_forward = 1" >> /etc/sysctl.conf
echo 1 > /proc/sys/net/ipv4/ip_forward