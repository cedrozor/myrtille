<%--
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

	    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
--%>

<%@ Page Language="C#" Inherits="Myrtille.Web.Default" Codebehind="Default.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="Myrtille.Web" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>

        <!-- force IE out of compatibility mode -->
        <meta http-equiv="X-UA-Compatible" content="IE=edge, chrome=1"/>

        <!-- mobile devices -->
        <meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0"/>
        
        <title>Myrtille</title>
        
        <link rel="icon" type="image/png" href="img/myrtille.png"/>
        <link rel="stylesheet" type="text/css" href="css/Default.css"/>
        <link rel="stylesheet" type="text/css" href="css/Modal.css"/>
        <link rel="stylesheet" type="text/css" href="css/Enterprise.css"/>
        <script language="javascript" type="text/javascript" src="js/myrtille.js"></script>
        <script language="javascript" type="text/javascript" src="js/config.js"></script>
        <script language="javascript" type="text/javascript" src="js/dialog.js"></script>
        <script language="javascript" type="text/javascript" src="js/display.js"></script>
        <script language="javascript" type="text/javascript" src="js/display/canvas.js"></script>
        <script language="javascript" type="text/javascript" src="js/display/divs.js"></script>
        <script language="javascript" type="text/javascript" src="js/network.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/buffer.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/longpolling.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/websocket.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/xmlhttp.js"></script>
        <script language="javascript" type="text/javascript" src="js/user.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/keyboard.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/mouse.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/touchscreen.js"></script>
        <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.2.1/jquery.min.js"></script>
        <script language="javascript" type="text/javascript" src="js/app/enterprise.js"></script>

	</head>
	
    <body>
        <input type="hidden" id="enterpriseSessionID" value="" />
        <input type="hidden" id="enterpriseSessionKey" value="" />
        <!-- display resolution -->
        <input type="hidden" runat="server" id="width"/>
        <input type="hidden" runat="server" id="height"/>
        
        <!-- LOGIN -->
        <div id="loginScreen">

            <!-- customizable logo -->
            <div runat="server" id="logo"></div>

            <div id="domainServerDiv" runat="server">
                <!-- server -->
                <div class="inputDiv">
                    <label id="serverLabel" for="server">Server (:port)</label>
                    <input type="text" id="server" title="server address or hostname (:port, if other than the standard 3389)"/>
                </div>

                <!-- domain -->
                <div class="inputDiv">
                    <label id="domainLabel" for="domain">Domain (optional)</label>
                    <input type="text" id="domain" title="user domain (if applicable)"/>
                </div>
            </div>
            <!-- user -->
            <div class="inputDiv">
                <label id="userLabel" for="user">User</label>
                <input type="text" id="username" title="user name"/>
            </div>

            <!-- password -->
            <div class="inputDiv">
                <label id="passwordLabel" for="password">Password</label>
                <input type="password" id="password" title="user password"/>
            </div>

            <!-- mfa password -->
            <div id="mfaDiv" class="inputDiv" runat="server" visible="false">
                <a runat="server" id="mfaProvider" href="#" target="_blank" tabindex="-1"></a>
                <input type="password" id="mfaPassword" title="mfa password"/>
            </div>

            <!-- program to run -->
            <div class="inputDiv" id="programDiv">
                <label id="programLabel" for="program">Program to run (optional)</label>
                <input type="text" id="program" title="executable path, name and parameters (double quotes must be escaped) (optional)"/>
            </div>

            <label id="loginError"></label>
            <!-- connect -->
            <%--<input type="submit" runat="server" id="connect" value="Connect!" onclick="showToolbar();" onserverclick="ConnectButtonClick" title="open session"/>--%>
            <%--<input type="button" id="login" value="Connect!" onclick="showToolbar();" title="open session"/>--%>
            <input type="button" id="login" runat="server" value="Connect!" title="open session"/>
            <!-- myrtille version -->
            <div id="version">
                <a href="http://cedrozor.github.io/myrtille/">
                    <img src="img/myrtille.png" alt="myrtille" width="15px" height="15px"/>
                </a>
                <span>
                    <%=typeof(Default).Assembly.GetName().Version%>
                </span>
            </div>                
        </div>
        <!-- TOOLBAR -->
        <div runat="server" id="toolbar" style="display:none">

            <!-- server info -->
            <input type="text" id="serverInfo" name="serverInfo" title="connected server" disabled="disabled"/>

            <!-- stat bar -->
            <input type="button" id="stat" value="Show Stat" onclick="toggleStatMode();" title="display network and rendering info" disabled="disabled"/>

            <!-- debug log -->
            <input type="button" id="debug" value="Show Debug" onclick="toggleDebugMode();" title="display debug info" disabled="disabled"/>

            <!-- browser mode -->
            <input type="button" id="browser" value="HTML4" onclick="toggleCompatibilityMode();" title="rendering mode" disabled="disabled"/>

            <!-- scale display -->
            <input type="button" id="scale" value="Scale" onclick="toggleScaleDisplay();" title="dynamically scale the remote session display to the browser size (responsive design)"/>

            <!-- send right-click (on the last touched or left-clicked position) to the remote session. may be useful on touchpads or iOS devices -->
            <input type="button" runat="server" id="mrc" value="Right-Click" onclick="sendRightClick();" title="send Right-Click (on the last touched or left-clicked position) to the remote session" />

            <!-- virtual keyboard. on devices without a physical keyboard, forces the device virtual keyboard to pop up -->
            <input type="button" id="keyboard" value="Keyboard" onclick="openPopup('virtualKeyboardPopup', 'VirtualKeyboard.aspx');" title="send text to the remote session (tip: can be used to send the local clipboard content (text only))"/>

            <!-- remote clipboard. display the remote clipboard content and allow to copy it locally (text only) -->
            <input type="button" id="clipboard" value="Clipboard" onclick="requestRemoteClipboard();" title="retrieve the remote clipboard content (text only)" disabled="disabled"/>

            <!-- upload/download file(s). only enabled if the connected server is localhost or if a domain is specified (so file(s) can be accessed within the rdp session) -->
            <input type="button" id="files" value="Files" onclick="openPopup('fileStoragePopup', 'FileStorage.aspx');" title="upload/download files to/from the user documents folder" disabled="disabled"/>

            <!-- send ctrl+alt+del to the remote session. may be useful to change the user password, for example -->
            <input type="button" id="cad" value="Ctrl+Alt+Del" onclick="sendCtrlAltDel();" title="send Ctrl+Alt+Del to the remote session"/>

            <!-- disconnect -->
            <input type="button" name="disconnect" id="disconnect" value="Disconnect" title="close session" class="logout"/>
        </div>

        <div id="serverContainer" style="left:150%;position:absolute">
            <div id="enterpriseDiv" style="display: none">
                <input type="button" value="New Host" title="New Host" id="newHost" onclick="showHostModal(false);" />        
                <input type="button" value="Logout" title="Logout" id="logout" class="logout"/>     

                <div id="serverListDiv" runat="server"></div>
            </div>        
        </div>
        

        <!-- remote session display -->
        <div id="displayDiv"></div>

        <!-- remote session helpers -->
        <div id="statDiv"></div>
		<div id="debugDiv"></div>
        <div id="msgDiv"></div>
        <div id="kbhDiv"></div>
        <div id="bgfDiv"></div>

        <div class="popup" id="popupHost">
            <div class="popup-inner">
                <input type="hidden" id="editHostID" name="editHostID" />
                <span><strong>Host Configuration</strong></span>
                <br />
                <div >
                    <h5><label id="hostnameLabel" for="editHostname">Host name:</label></h5>
                    <input type="text" id="editHostname" name="editHostname" title="host name" />
                    <input type="button" id="createSession" value="Create Session" onclick="showCreateSessionModal(false);"/>
                </div>
                <div>
                    <h5><label id="hostAddressLabel" for="editHostaddress">Host address (optional, uses hostname if not specified):</label></h5>
                    <input type="text" runat="server" id="editHostaddress" name="editHostaddress" title="host address" />
                </div>
                <div>
                    <h5><label id="groupAccessLabel" for="editgroupaccess">Domain Groups Allowed (comma seperated)</label></h5>
                    <input type="text" runat="server" id="editgroupaccess" name="editgroupaccess" title="group access" style="width:100%"/>
                </div>
                <div>
                    <h5><label id="securityProtocol" for="editSecurityProtocol">RDP Security Protocol</label></h5>
                    <select name="editSecurityProtocol" id="editSecurityProtocol">
                        <option value="0">auto</option>
                        <option value="1">rdp</option>
                        <option value="2">tls</option>
                        <option value="3">nla</option>
                        <option value="4">nla-ext</option>
                    </select>
                </div>
                <br />
                <br />
                <div style="float: right">
                    <input type="button" id="deleteHost" value="Delete" style="margin-right:10px"/>
                    <input type="button" id="saveHost" value="Save" />
                    <input type="button" id="closePopupButton" value="Close" onclick="closeModal('popupHost');"/>
                </div>
            </div>
        </div>

        <div class="popup" id="popupSession">
            <div class="popup-inner">
                <span><strong>Create Host Session for <label id="sessionHostnameLabel"></label></strong></span>
                <br />
                <div >
                    <h5><label id="sessionUsernameLabel" for="sessionUsername">Username:</label></h5>
                    <input type="text" id="sessionUsername" name="sessionUsername" title="Username" />
                </div>
                <div>
                    <h5><label id="sessionPasswordLabel" for="sessionPassword">Password:</label></h5>
                    <input type="password" id="sessionPassword" name="sessionPassword" title="Password" />
                    <input type="button" id="createUserSession" value="Create" />
                </div>
                <div>
                    <h5>Session URL:</h5>
                    <textarea  id="sessionURL" readonly="readonly" style="width:100%" rows="4" cols="2" ></textarea>
                    <center><h5>Copy the URL and use how required, this URL can only be used once.</h5></center>
                </div>
                <br />
                <br />

                <div style="float: right">
                    <input type="button" id="closeSessionPopupButton" value="Close" onclick="closeModal('popupSession');"/>
                </div>
            </div>
        </div>

        <script type="text/javascript" language="javascript" defer="defer">
            // start program from url
            // if the display resolution isn't set, the remote session isn't able to start; redirect with the client resolution
            if (window.location.href.indexOf('&program=') != -1 && (window.location.href.indexOf('&width=') == -1 || window.location.href.indexOf('&height=') == -1))
            {
                showToolbar();

                var width = document.getElementById('<%=width.ClientID%>').value;
                var height = document.getElementById('<%=height.ClientID%>').value;

                var redirectUrl = window.location.href;

                if (window.location.href.indexOf('&width=') == -1)
                {
                    redirectUrl += '&width=' + width;
                }

                if (window.location.href.indexOf('&height=') == -1)
                {
                    redirectUrl += '&height=' + height;
                }

                //alert('reloading page with url:' + redirectUrl);

                window.location.href = redirectUrl;
            }

           

		</script>

	</body>

</html>