In order to run this code, load up your visual studio code as administrator.  Ensure that pcap.h is installed and included in your directories for visual studio.
go to this website:  https://npcap.com/#download
and download the installer, and the SDK put the SDK in a known directory, IE your desktop.  Then in your Visual Studio (The purple community one), navigate to your solution explorer.
Once you are there in your solution explorer, right click the first file under your solution file (See Screenshot 1) and click properties.
Then, copy paste the directory of your basic SDK folder into the C/C++ include directories (See Screenshot 2).
Then, under linker general, add the include directory of your SDK librarry x64 files to your additional library dependencies (See Screenshot 3)

Now that we installed pcap, we have to install Libsodium download it from the link below:
https://download.libsodium.org/libsodium/releases/libsodium-1.0.20-stable-msvc.zip
Once thats installed, we need to link a few more things, so unzip it to a folder on your desktop.
Navigate to your project properties as shown in screenshot 1.  
Copy paste the directory of include subfolder into the c++ include directories like with screenshot 2.  Ensure a semi colon (;) is used to seperate them and all future includes
Then, under linker general, copy the directory of your libsodium itcg folder (See screenshot 3).  (\libsodium\x64\Release\v143\ltcg).
Then, under the linker input tab, copy this line: wpcap.lib;Packet.lib;libsodium.lib;ws2_32.lib;$(CoreLibraryDependencies);%(AdditionalDependencies) into the Additional Dependencies tab (See Screenshot 4)
Then under the c++ Preprocessor Definitions tab, copy this line (See Screenshot 5): SODIUM_STATIC=1;_DEBUG;_CONSOLE;%(PreprocessorDefinitions)
Finally, to wrap up nagivate to the language tab under C/C++ and ensure that your program is running C++ version 17 if not, copy this line into it: ISO C++17 Standard (/std:c++17)  (See Screenshot 6)

