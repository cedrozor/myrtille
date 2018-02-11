# Myrtille
Myrtille provides a simple and fast access to remote desktops and applications through a web browser, without any plugin, extension or configuration.

Technically, Myrtille is an HTTP(S) to RDP gateway.

## How does it work?
It works by forwarding the user inputs (keyboard, mouse, touchscreen) from a web browser to an HTTP(S) gateway, then up to an RDP client which maintains a session with an RDP server.

The display resulting, or not, of such actions is streamed back to the browser, from the rdp client and through the gateway.

The implementation is quite straightforward in order to maintain the best speed and stability as possible. Some optimizations, such as inputs buffering and display quality tweaking, help to mitigate with latency and bandwidth issues.

More information in the DOCUMENTATION.md file.

## Features
- HTTP(S) to RDP gateway
- Start remote application from URL
- File transfer (local and roaming accounts)
- HTML4 and HTML5 support
- Responsive design
- Remote clipboard support
- PNG, JPEG and WEBP compression
- Realtime connection information
- On-screen/console/logfile debug information
- fully parameterizable

## Requirements
- Browser: any HTML4 or HTML5 browser (starting from IE6!). No extension or administrative rights required.
- Gateway (myrtille): IIS 7 or greater (preferably IIS 8+ with websocket protocol enabled) and .NET 4.5+
- RDP server: any RDP enabled computer (preferably Windows Server but can also be Windows XP, 7, 8, 10)

## Resources
Myrtille does support multiple users sessions/tabs.

There is no limitation about the maximal number of concurrent users beside what the rdp server(s) can handle (number of CALs, CPU, RAM?).

Regarding the gateway, a simple dual core CPU with 4GB RAM can handle up to 50 simultaneous sessions (about 50MB RAM by rdp client process).

Each session uses about 20KB/sec bandwidth with the browser.

## Build
Microsoft Visual Studio 2015. See DOCUMENTATION.md.

## Installation
All releases here: https://github.com/cedrozor/myrtille/releases

See DOCUMENTATION.md for more details.

## Usage
Once Myrtille is installed on your server, you can use it at http://myserver/myrtille. Set the rdp server address, user domain (if any), name and password then click "Connect!" to log in. "Disconnect" to log out.

You can also connect a remote desktop and **start a program automatically from an url** (see DOCUMENTATION.md). From version 1.5.0, Myrtille does support encrypted credentials (aka "password 51" into .rdp files) so the urls can be distributed to third parties without compromising on security.

The installer creates a self-signed certificate for https://myserver/myrtille. Like for all self-signed certificates, you will have to add a security exception into your browser (just ignore the warning message and proceed to the website).
Of course, you can avoid that by installing a certificate provided by a trusted Certification Authority (see DOCUMENTATION.md).

If you want connection information, you can enable stat (displayed on screen or browser console). If you want debug information, you can enable debug (logs are saved under the Myrtille "log" folder). *Hidden from version 1.5.0 (can be enabled into default.css)*.

You can also choose the rendering mode, HTML4 or HTML5 (HTML4 may be useful, for example, if websockets are blocked by a proxy or firewall). *Hidden (autodetected) from version 1.5.0 (can be enabled into default.css)*.

On touchscreen devices, you can pop the device keyboard with the "Keyboard" button. Then enter some text and click "Send". This can be used, for example, to paste the local clipboard content and send it to the server (then it be copied from there, within the remote session).
Alternatively, you can run **osk.exe** (the Windows on screen keyboard, located into %SystemRoot%\System32) within the remote session. It can even be run automatically on session start (https://www.cybernetman.com/kb/index.cfm/fuseaction/home.viewArticles/articleId/197).

The remote clipboard content can also be retrieved locally with the "Clipboard" button (text format only).

You can upload/download file(s) to/from the user documents folder with the "Files" button. Note that it requires the rdp server to be localhost (same machine as the http server) or a domain to be specified.

## Enterprise Functions
Enterprise functions work by first authenticating a user against an active directory (or similar). This allows the administrators to add hosts which the users are allowed to connect to. This is controlled by allowing certain active directory groups to access hosts only.

To enable enterprise mode uncomment the Myrtille.Services app.config file where indicated

EnterpriseAdapter - This configuration parameter can be changed to use another enterprise service adapter which can be created to work with other directories, this adapter must implement interface Myrtille.Common.Interfaces.IEnterpriseAdapter
EnterpriseAdminGroup - This is the domain group allowed to add/edit hosts, and also create session link URLS to be privided to 3rd parties.
EnterpriseDomain - The configuration parameter of the directory server to provide the authentication, this should be your FQDN or the IP address of a domain controller.

## One Time Passcode authentication
To add an additional layer of security Myrtille now supports one-time passcode authentication, enabling this option will 

DISCLAMER: The contributor of the Myrtille.OASISOTPAdapter is the owner of a company providing a cloud based one-time passcode authentication service.
The contributor ensured that the interfaces were written to allow for other authentication providers to be used in keeping with the open source contribution and to purposely ensure no tie in to the OASIS platform.

To use the OASIS platform, visit https://oasis.oliveinnovations.com and register and follow the steps

1) create a new User Group named for example Myrtille
2) Create a new Application named for example MyrtilleRDP, once created tick to allow user group Myrtille
3) Once application created, click button Application Key, this will give you an API Key, Application ID, Application Key, copy these values to the Myrtille.Service app.config in the appSettings, then uncomment app setting MFAAuthAdapter 
4) Create a user, the username must be the same as the user who you wish to verify, tick the box to register via email and follow instructions sent via email.
5) Once created ensure the user belongs to the user group created

More information can be found at http://www.oliveinnovations.com

If you are using a active directory for authentication Olive Innovations also provides a gateway and directory sync service to create users automatically from your domain services.

NOTE: Olive Innovations created the OTP service, interfaces and adapter as they use Myrtille and wanted to share their implmentation. They also provided in Myrtille.Common.Intefaces an interface IMultiFactorAuthenticationAdapter, other adapters can be written and implement this interface to allow custom authentciation providers and even one that is not dependant on the OASIS service.

## Third-party
Myrtille uses the following licensed software:
- RDP client: FreeRDP (https://github.com/FreeRDP/FreeRDP), licensed under Apache 2.0 license. Myrtille uses a fork of FreeRDP (https://github.com/cedrozor/FreeRDP), to enforce a loose coupling architecture and always use the latest version of FreeRDP (the fork is periodically synchronized with the FreeRDP master branch).
- OpenSSL toolkit 1.0.2n (https://github.com/openssl/openssl), licensed under BSD-style Open Source licenses. Precompiled versions of OpenSSL can be obtained here: https://wiki.openssl.org/index.php/Binaries.
- WebP encoding: libWebP 0.5.1 (https://developers.google.com/speed/webp/), licensed under BSD-style Open Source license. Copyright (c) 2010, Google Inc. All rights reserved.
- HTML5 websockets: Microsoft.WebSockets 0.2.3.1 (https://www.nuget.org/packages/Microsoft.WebSockets/0.2.3.1), licensed under MIT license. Copyright (c) Microsoft 2012.
- Logging: Log4net 2.0.8 (https://logging.apache.org/log4net/), licensed under Apache 2.0 license.
## Optional Third-party
- OTP Adapter: OASIS.Integration 1.5.0.0 (https://github.com/oliveinnovations/oasis) (https://www.nuget.org/packages/OASIS.Integration/), licensed under Apache 2.0 License.



See DISCLAIMERS.md file.

The Myrtille code in FreeRDP is surrounded by region tags "#pragma region Myrtille" and "#pragma endregion".

libWebP are official Google's WebP precompiled binaries, and are left unmodified.

## License
Myrtille is licensed under Apache 2.0 license.
See LICENSE file.

## Author
Cedric Coste (cedrozor@gmail.com).
- LinkedIn:	https://fr.linkedin.com/in/cedric-coste-a1b9194b
- Twitter:	https://twitter.com/cedrozor
- Google+:	https://plus.google.com/111659262235573941837
- Facebook:	https://www.facebook.com/profile.php?id=100011710352840

## Contributors
- Catalin Trifanescu (AppliKr developer: application server. Steemind cofounder)
- Fabien Janvier (AppliKr developer: website css, clipping algorithm, websocket server)
- UltraSam (AppliKr developer: rdp client, http gateway)

## Links
- Website:	http://cedrozor.github.io/myrtille
- Sources:	https://github.com/cedrozor/myrtille
- Tracker:	https://github.com/cedrozor/myrtille/issues
- Wiki:		https://github.com/cedrozor/myrtille/wiki
- Support:	https://groups.google.com/forum/#!forum/myrtille_rdp
- Demo:		https://www.youtube.com/watch?v=l4CL8h0KfQ8
