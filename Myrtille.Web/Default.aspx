<%--
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright (c) 2014-2016 Cedric Coste

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
        <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1"/>
        <meta name="viewport" content="initial-scale=1.0"/>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="css/Default.css"/>
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
	</head>

    <body onload="startMyrtille(
       '<%=HttpContext.Current.Session.SessionID%>',
        <%=(RemoteSessionManager != null && (RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected)).ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPort.ToString()]%>,
        <%=(HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()] == null ? "null" : HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()])%>,
        <%=(stat.Value == "Stat enabled").ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=(debug.Value == "Debug enabled").ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=(browser.Value == "HTML4").ToString(CultureInfo.InvariantCulture).ToLower()%>);">

        <form method="post" runat="server" id="mainForm">

            <div runat="server" id="controlDiv" class="controlDiv">

                <%-- connection settings --%>
                <%
                if(!Boolean.Parse((ConfigurationSettings.AppSettings["HideServer"] == null ? "false" : ConfigurationSettings.AppSettings["HideServer"]))){
                %>
                  <span runat="server" id="serverLabel" class="controlLabel">Server</span><input type="text" runat="server" id="server" class="serverText" title="server address" value='<%$ AppSettings:DefaultServerName %>'/>
                <%
                }else{
                %>
                  <input type="hidden" runat="server" id="server2" class="serverText" title="server address" value='<%$ AppSettings:DefaultServerName %>'/>
                <%
                }
                %>
                <%
                if(!Boolean.Parse((ConfigurationSettings.AppSettings["HideDomain"] == null ? "false" : ConfigurationSettings.AppSettings["HideDomain"]))){
                %>
                  <span runat="server" id="domainLabel" class="controlLabel">Domain (optional)</span><input type="text" runat="server" id="domain" class="domainText" title="user domain" value='<%$ AppSettings:DefaultDomainName %>'/>
                <%
                }else{
                %>
                  <input type="hidden" runat="server" id="domain2" class="domainText" title="user domain" value='<%$ AppSettings:DefaultDomainName %>'/>
                <%
                }
                %>
                <span runat="server" id="userLabel" class="controlLabel">User</span><input type="text" runat="server" id="user" class="userText" title="user name"/>
                <span runat="server" id="passwordLabel" class="controlLabel">Password</span><input type="password" runat="server" id="password" class="passwordText" title="user password"/>
                <%
                if(!Boolean.Parse((ConfigurationSettings.AppSettings["HideStats"] == null ? "false" : ConfigurationSettings.AppSettings["HideStats"]))){
                %>
                  <span runat="server" id="statsLabel" class="controlLabel">Stats</span><select runat="server" id="stat" class="statSelect" title="display stats bar"><option selected="selected">Stat disabled</option><option>Stat enabled</option></select>
                <%
                }
                %>
                <%
                if(!Boolean.Parse((ConfigurationSettings.AppSettings["HideDebug"] == null ? "false" : ConfigurationSettings.AppSettings["HideDebug"]))){
                %>
                  <span runat="server" id="debugLabel" class="controlLabel">Debug</span><select runat="server" id="debug" class="debugSelect" title="display debug info and save session logs"><option selected="selected">Debug disabled</option><option>Debug enabled</option></select>
                <%
                }
                %>
                <span runat="server" id="browserLabel" class="controlLabel">Browser</span><select runat="server" id="browser" class="browserSelect" title="rendering mode"><option>HTML4</option><option selected="selected">HTML5</option></select>
                <%
                if(!Boolean.Parse((ConfigurationSettings.AppSettings["HideProgram"] == null ? "false" : ConfigurationSettings.AppSettings["HideProgram"]))){
                %>
                  <span runat="server" id="programLabel" class="controlLabel">Program to run (optional)</span><input type="text" runat="server" id="program" class="programText" title="executable path, name and parameters (double quotes must be escaped)"/>
                <%
                }
                %>
                <input type="hidden" runat="server" id="width"/>
                <input type="hidden" runat="server" id="height"/>
                <input type="submit" runat="server" id="connect" class="connectButton" value="Connect!" onclick="setClientResolution();" onserverclick="ConnectButtonClick" title="open session"/>
                <input type="button" runat="server" id="disconnect" class="disconnectButton" value="Disconnect" visible="false" onserverclick="DisconnectButtonClick" title="close session"/>

                <%-- virtual keyboard. on devices without a physical keyboard, forces the device virtual keyboard to pop up --%>
                <input type="button" runat="server" id="keyboard" class="keyboardButton" value="Keyboard" visible="false" onclick="openPopup('virtualKeyboardPopup', 'VirtualKeyboard.aspx');" title="send text to the remote session (tip: can be used to send the local clipboard content (text only))"/>

                <%-- remote clipboard. display the remote clipboard content and allow to copy it locally (text only) --%>
                <input type="button" runat="server" id="clipboard" class="clipboardButton" value="Clipboard" visible="false" onclick="doIFrameCall('RemoteClipboard.aspx');" title="retrieve the remote clipboard content (text only)"/>

                <%-- upload/download file(s). only enabled if the connected server is localhost or if a domain is specified (so file(s) can be accessed within the rdp session) --%>
                <input type="button" runat="server" id="files" class="filesButton" value="My Documents" visible="false" onclick="openPopup('fileStoragePopup', 'FileStorage.aspx');" title="upload/download files to/from the user documents folder"/>

                <%-- send ctrl+alt+del to the rdp session. may be useful to change the user password, for example --%>
                <input type="button" runat="server" id="cad" class="cadButton" value="Ctrl+Alt+Del" visible="false" onclick="sendCtrlAltDel();" title="send Ctrl+Alt+Del to the remote session"/>

            </div>

            <%-- remote session display --%>
            <div id="displayDiv"></div>

            <%-- remote session helpers --%>
            <div id="statDiv"></div>
		    <div id="debugDiv"></div>
            <div id="msgDiv"></div>
            <div id="kbhDiv"></div>
            <div id="bgfDiv"></div>

        </form>

        <script type="text/javascript" language="javascript" defer="defer">

		    // browser size. default 1024x768
		    function setClientResolution()
		    {
		        var display = new Display();
	            document.getElementById('<%=width.ClientID%>').value = display.getBrowserWidth() - display.getHorizontalOffset();
		        document.getElementById('<%=height.ClientID%>').value = display.getBrowserHeight() - display.getVerticalOffset() - display.getToolbarHeight();
		    }

		</script>

	</body>

</html>
