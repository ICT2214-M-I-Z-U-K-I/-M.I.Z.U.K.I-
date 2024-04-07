# -M.I.Z.U.K.I-
A Multifaceted Integrated Zero-trust Unified Key Infrastructure

At its core, [M.I.Z.U.K.I] is a secure file sharing solution that enables a fault tolerant collaborative encryption and decryption sequence between multiple parties while enforcing Authentication, Authorization and Accounting (AAA) between individual parties over a network. It has been designed to be scalable from the ground up; it can be deployed in environments ranging from small-scale home networks to large-scale enterprise networks spanning across the internet, catering to both enthusiasts and businesses alike.

# Usage Instructions
System Requirements:
Windows 11, x86-64 Architecture.
.NET Desktop Runtime 9.0.0-preview.2 or later.
Internet Connectivity.

User Manual;
Launch MIZUKI.exe, and allow Certificates to be Installed to Windows Certificate Manager
MIZUKI.exe will automatically create the Signed Certificate Profile.


Add Peers
1. Begin by Adding Peers, with the Add Friend button. This requires their GUID, ensure that GUID is accurate.


Encryption.
1. Control + Click, or Drag Select the number of Peers you need to encrypt your file with, then;
2. Add a file by either clicking on Browse button, or Drag and Drop files.
3. Click on Encrypt button to begin encryption
4. Select the Threshold (Minimum number of Peers required for Decryption). The minimum is two.
5. The Encrypted .mzk file is automatically created at the same directory of your original file.


Decryption.
1. Control + Click, or Drag Select the number of Peers you need to encrypt your file with, then;
2. Add a file by either clicking on Browse button, or Drag and Drop files.
3. Click on Decrypt button to begin decryption.
4. MIZUKI will automatically find all peers to decrypt the file.
5. The decrypted file is is automatically created at the same directory of your encrypted file.


Decryption Prompt.
1. When a decryption prompt appears, it notifies on the decryption request that was made by a Peer.
2. Approve or Deny the request.