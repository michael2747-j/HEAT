In order to run this code, load up your visual studio code as administrator.  Ensure that pcap.h is installed and included in your directories for visual studio.
go to this website:  https://npcap.com/#download
and download the installer, and the SDK put the SDK in a known directory, IE your desktop.  Then in your Visual Studio (The purple community one), navigate to your solution explorer.
Once you are there in your solution explorer, right click the first file under your solution file (See Screenshot 1) and click properties.
Then, copy paste the directory of your basic SDK folder into the C++ include directories (See Screenshot 2).
Then, under linker general, add the include directory of your SDK librarry x64 files to your additional library dependencies (See Screenshot 3)
Finally, under the linker input tab, copy this line:  wpcap.lib;Packet.lib;ws2_32.lib;$(CoreLibraryDependencies);%(AdditionalDependencies) into the Additional Dependencies tab (See Screenshot 4)

Then, to test the code, download and install Iperf.  Once that's done, unzip it to a folder, and open the sub folder (for me its iperf3.16_64) up in two separate command prompt terminals.

In those terminals, designate one as the sender, and one as the receiver.  
In your receiver terminal, type this:    iperf3.exe -s -p (port number)
In your sender terminal, type this:      iperf3.exe -c (IP address) -p (port number) -u -b 1M

The port number should be either 500, 4500, or 51820.  Those are the ports it reads on.  
The IP address is the IP address of the receiver, and can be found with ipconfig under: Wireless LAN adapter Wi-Fi.
Ensure you start the receiver before the sender.  A successful run should look as follows.

-----------------------------------------------------------
Server listening on 51820 (test #1)
-----------------------------------------------------------

Accepted connection from 192.168.1.57, port 57397   (The IP will be different if you are sending from another machine)

Connecting to host 192.168.1.57, port 51820

Once this is done, and the server is running, run the given code in your compiler as admin.  The output should be something like this:
Capturing on all available interfaces...
Press Ctrl+C to stop.
Started capturing on interface: Microsoft Wi-Fi Direct Virtual Adapter
Started capturing on interface: WAN Miniport (IPv6)
Started capturing on interface: WAN Miniport (IP)
Started capturing on interface: VirtualBox Host-Only Ethernet Adapter
Started capturing on interface: WAN Miniport (Network Monitor)
Started capturing on interface: Bluetooth Device (Personal Area Network)
Started capturing on interface: Realtek RTL8821CE 802.11ac PCIe Adapter
Started capturing on interface: Microsoft Wi-Fi Direct Virtual Adapter #2
Started capturing on interface: Adapter for loopback traffic capture
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 4 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:51820 -> 192.168.1.57:50536 | Payload length: 4 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
[IPv4] 192.168.1.57:50536 -> 192.168.1.57:51820 | Payload length: 65495 bytes | WireGuard UDP packet
