<%-- 
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2021 Cedric Coste

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

<%@ Page 
    Language="C#" 
    Inherits="Myrtille.Web.Default" 
    Codebehind="Default.aspx.cs" 
    AutoEventWireup="true" 
    Culture="auto" 
    UICulture="auto" 
%>
<%@ OutputCache Location="None" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="Myrtille.Web" %>
<%@ Import Namespace="Myrtille.Services.Contracts" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
                    "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <!-- 强制 IE 使用最新引擎，并激活 Chrome Frame -->
    <meta http-equiv="X-UA-Compatible" content="IE=edge, chrome=1" />
    <!-- 移动端适配 -->
    <meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0" />

    <title>
        Myrtille
        <%= RemoteSession != null 
            && !RemoteSession.ConnectionService 
            && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected)
            && !string.IsNullOrEmpty(RemoteSession.ServerAddress) 
                ? " - " + RemoteSession.ServerAddress.ToString() 
                : "" 
        %>
    </title>

    <link rel="icon" type="image/x-icon" href="favicon.ico" />
    <link rel="stylesheet" type="text/css" href="<%=BundleTable.Bundles.ResolveBundleUrl("~/css/Default.css", true)%>" />
    <link rel="stylesheet" type="text/css" href="<%=BundleTable.Bundles.ResolveBundleUrl("~/css/xterm.css", true)%>" />

    <!-- JS 脚本引用 -->
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/tools/common.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/tools/convert.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/myrtille.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/config.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/dialog.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/display.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/display/canvas.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/display/divs.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/display/terminaldiv.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network/buffer.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network/eventsource.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network/longpolling.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network/websocket.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/network/xmlhttp.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/user.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/user/keyboard.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/user/mouse.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/user/touchscreen.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/xterm/xterm.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/xterm/addons/fit/fit.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/js/audio/audiowebsocket.js", true)%>"></script>
    <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/node_modules/interactjs/dist/interact.js", true)%>"></script>

    <!-- 自定义CSS（示例，美化 + 顶栏 + 居中登录模态） -->
    <style>
      /* 背景及整体布局 */
   
    </style>
</head>

<body onload="startMyrtille(
    <%= (RemoteSession != null ? "'" + RemoteSession.State.ToString().ToUpper() + "'" : "null") %>,
    getToggleCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'stat'),
    getToggleCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'debug'),
    getToggleCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'browser'),
    <%= (RemoteSession != null && RemoteSession.BrowserResize.HasValue 
            ? "'" + RemoteSession.BrowserResize.Value.ToString().ToUpper() + "'"
            : "null") %>,
    <%= (RemoteSession != null ? RemoteSession.ClientWidth.ToString() : "null") %>,
    <%= (RemoteSession != null ? RemoteSession.ClientHeight.ToString() : "null") %>,
    '<%= (RemoteSession != null ? RemoteSession.HostType.ToString() : HostType.RDP.ToString()) %>',
    <%= (RemoteSession != null && !string.IsNullOrEmpty(RemoteSession.VMGuid) && !RemoteSession.VMEnhancedMode)
            .ToString().ToLower() %>
);">

    <div class="top-bar">
        <div class="logo">VNC</div>
        
        <div class="title" data-i18n="appTitle">Myrtille</div>

        <button
            type="button"
            id="mobileKeyboardButton"
            class="mobile-keyboard-trigger"
            onclick="openMobileKeyboard();"
            data-i18n-title="mobileKeyboardTitle"
            title="Open keyboard"
            aria-label="Keyboard"
        >
            <svg viewBox="0 0 24 24" aria-hidden="true" width="16" height="16">
                <path fill="currentColor" d="M20 5H4a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Zm0 12H4V7h16v10ZM6 9h2v2H6V9Zm3 0h2v2H9V9Zm3 0h2v2h-2V9Zm3 0h2v2h-2V9ZM6 12h2v2H6v-2Zm3 0h8v2H9v-2Z"/>
            </svg>
        </button>
        
        <div class="actions">
            <a
                class="ctf"
                href="javascript:sendCtrlAltDel();"
                data-i18n="sendCtrlAltDel"
                data-i18n-title="sendCtrlAltDelTitle"
                title="Ctrl+Alt+Del"
            >Ctrl + Alt + Del</a>
            
            <div class="color-boxes">
                <div class="color-box blue" data-theme="blue"></div>
    <div class="color-box green" data-theme="green"></div>
    <div class="color-box grey" data-theme="grey"></div>
    <div class="color-box navy" data-theme="navy"></div>
            </div>
        </div>
    </div>

    <form method="post" runat="server" id="mainForm">
  
        <input type="hidden" runat="server" id="width" />
        <input type="hidden" runat="server" id="height" />
        <textarea id="mobileKeyboardInput" autocapitalize="none" autocomplete="off" autocorrect="off" spellcheck="false" enterkeyhint="done" aria-hidden="true"></textarea>

        <div runat="server" id="login" visible="false">

            <div class="login-card-inner">
            <h2 data-i18n="loginTitle">Sign in to VNC</h2>

     
            <div runat="server" id="hostConnectDiv">
                <div class="inputDiv">
                    <label id="hostTypeLabel" for="hostType" data-i18n="hostTypeLabel">Protocol</label>
                    <select
                        runat="server"
                        id="hostType"
                        onchange="onHostTypeChange(this);"
                        data-i18n-title="hostTypeTitle"
                        title="Select connection protocol"
                    >
                        <option value="0" selected="selected">RDP</option>
                        <option value="0" data-i18n="rdpVmBus">RDP (Virtual Machine Bus)</option>
                        <option value="1">SSH</option>
                    </select>
                </div>

                <div class="inputDiv" id="securityProtocolDiv">
                    <label id="securityProtocolLabel" for="securityProtocol" data-i18n="securityProtocolLabel">Security</label>
                    <select
                        runat="server"
                        id="securityProtocol"
                        data-i18n-title="securityProtocolTitle"
                        title="NLA = most secure; RDP = best compatibility; use AUTO if unsure"
                    >
                        <option value="0" selected="selected">AUTO</option>
                        <option value="1">RDP</option>
                        <option value="2">TLS</option>
                        <option value="3">NLA</option>
                        <option value="4">NLA-EXT</option>
                    </select>
                </div>

                <div class="inputDiv">
                    <label id="serverLabel" for="server" data-i18n="serverLabel">Server (:port)</label>
                    <input
                        type="text"
                        runat="server"
                        id="server"
                        data-i18n-title="serverTitle"
                        title="Server hostname or IP address (optional custom port)"
                    />
                </div>

                <div id="vmDiv" style="visibility:hidden;display:none;">
                    <div class="inputDiv" id="vmGuidDiv">
                        <label id="vmGuidLabel" for="vmGuid" data-i18n="vmGuidLabel">VM GUID</label>
                        <input
                            type="text"
                            runat="server"
                            id="vmGuid"
                            data-i18n-title="vmGuidTitle"
                            title="Hyper-V virtual machine GUID to connect"
                        />
                    </div>
                    <div class="inputDiv" id="vmEnhancedModeDiv">
                        <label id="vmEnhancedModeLabel" for="vmEnhancedMode" data-i18n="vmEnhancedModeLabel">Enhanced mode</label>
                        <input
                            type="checkbox"
                            runat="server"
                            id="vmEnhancedMode"
                            data-i18n-title="vmEnhancedModeTitle"
                            title="Supports faster display and clipboard/printer redirection"
                        />
                    </div>
                </div>

                <div class="inputDiv" id="domainDiv">
                    <label id="domainLabel" for="domain" data-i18n="domainLabel">Domain (optional)</label>
                    <input
                        type="text"
                        runat="server"
                        id="domain"
                        data-i18n-title="domainTitle"
                        title="Domain information, if applicable"
                    />
                </div>
            </div>

            <div class="inputDiv">
                <label id="userLabel" for="user" data-i18n="userLabel">Username</label>
                <input
                    type="text"
                    runat="server"
                    id="user"
                    data-i18n-title="userTitle"
                    title="Sign-in username"
                />
            </div>

            <div class="inputDiv">
                <label id="passwordLabel" for="password" data-i18n="passwordLabel">Password</label>
                <input
                    type="password"
                    runat="server"
                    id="password"
                    data-i18n-title="passwordTitle"
                    title="Sign-in password"
                />
            </div>

          
            <input type="hidden" runat="server" id="passwordHash" />

            <div class="inputDiv" runat="server" id="mfaDiv" visible="false">
                <a
                    runat="server"
                    id="mfaProvider"
                    href="#"
                    target="_blank"
                    tabindex="-1"
                    data-i18n-title="mfaProviderTitle"
                    title="MFA provider"
                ></a>
                <input
                    type="text"
                    runat="server"
                    id="mfaPassword"
                    data-i18n-title="mfaPasswordTitle"
                    title="MFA code"
                />
            </div>

            <div class="inputDiv">
                <label id="programLabel" for="program" data-i18n="programLabel">Program to start (optional)</label>
                <input
                    type="text"
                    runat="server"
                    id="program"
                    data-i18n-title="programTitle"
                    title="Executable path, name, and arguments (optional)"
                />
            </div>

            <input
                type="submit"
                runat="server"
                id="connect"
                data-i18n-value="connectButton"
                value="Sign In"
                onserverclick="ConnectButtonClick"
                data-i18n-title="connectTitle"
                title="Start remote session"
            />

           
            <div id="version">
                <a href="https://www.myrtille.io/" target="_blank" data-i18n-title="websiteTitle" title="Myrtille website">
                    <img src="img/myrtille.png" alt="myrtille" width="15px" height="15px" />
                </a>
                <span>
                    <%= typeof(Default).Assembly.GetName().Version %>
                </span>
            </div>
            <div runat="server" id="adminDiv" visible="false">
                <a runat="server" id="adminUrl" href="?mode=admin">
                    <span runat="server" id="adminText" data-i18n="adminText">Host Management</span>
                </a>
            </div>

            <!-- 错误提示 -->
            <div id="errorDiv">
                <span runat="server" id="connectError"></span>
            </div>
</div>

        </div>

        <!-- ================================
             主机管理区
        ================================ -->
        <div runat="server" id="hosts" visible="false">
            <div id="hostsControl">
                <input
                    type="text"
                    runat="server"
                    id="enterpriseUserInfo"
                    data-i18n-title="enterpriseUserInfoTitle"
                    title="Signed-in user info"
                    disabled="disabled"
                />
                <input
                    type="button"
                    runat="server"
                    id="newRDPHost"
                    data-i18n-value="newRdpHost"
                    value="New RDP Host"
                    onclick="openPopup('editHostPopup', 'EditHost.aspx?hostType=RDP');"
                    data-i18n-title="newRdpHostTitle"
                    title="Create a new RDP host configuration"
                />
                <input
                    type="button"
                    runat="server"
                    id="newSSHHost"
                    data-i18n-value="newSshHost"
                    value="New SSH Host"
                    onclick="openPopup('editHostPopup', 'EditHost.aspx?hostType=SSH');"
                    data-i18n-title="newSshHostTitle"
                    title="Create a new SSH host configuration"
                />
                <input
                    type="button"
                    runat="server"
                    id="logout"
                    data-i18n-value="logoutButton"
                    value="Sign Out"
                    onserverclick="LogoutButtonClick"
                    data-i18n-title="logoutTitle"
                    title="Sign out current user"
                />
            </div>

            <asp:Repeater runat="server" id="hostsList" OnItemDataBound="hostsList_ItemDataBound">
                <ItemTemplate>
                    <div class="hostDiv">
                        <a runat="server" id="hostLink" data-i18n-title="hostLinkTitle" title="Connect">
                            <img
                                src="<%# Eval("HostImage").ToString() %>"
                                alt="host"
                                width="128px"
                                height="128px"
                            />
                        </a>
                        <br />
                        <span runat="server" id="hostName"></span>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>

        <!-- ================================
             工具栏
        ================================ -->
        <div runat="server" id="toolbarToggle" visible="false">
            <svg onclick="toggleToolbar();" focusable="false" data-icon="setting" width="1em" height="1em" fill="currentColor" aria-hidden="true" viewBox="64 64 896 896"><path d="M924.8 625.7l-65.5-56c3.1-19 4.7-38.4 4.7-57.8s-1.6-38.8-4.7-57.8l65.5-56a32.03 32.03 0 009.3-35.2l-.9-2.6a443.74 443.74 0 00-79.7-137.9l-1.8-2.1a32.12 32.12 0 00-35.1-9.5l-81.3 28.9c-30-24.6-63.5-44-99.7-57.6l-15.7-85a32.05 32.05 0 00-25.8-25.7l-2.7-.5c-52.1-9.4-106.9-9.4-159 0l-2.7.5a32.05 32.05 0 00-25.8 25.7l-15.8 85.4a351.86 351.86 0 00-99 57.4l-81.9-29.1a32 32 0 00-35.1 9.5l-1.8 2.1a446.02 446.02 0 00-79.7 137.9l-.9 2.6c-4.5 12.5-.8 26.5 9.3 35.2l66.3 56.6c-3.1 18.8-4.6 38-4.6 57.1 0 19.2 1.5 38.4 4.6 57.1L99 625.5a32.03 32.03 0 00-9.3 35.2l.9 2.6c18.1 50.4 44.9 96.9 79.7 137.9l1.8 2.1a32.12 32.12 0 0035.1 9.5l81.9-29.1c29.8 24.5 63.1 43.9 99 57.4l15.8 85.4a32.05 32.05 0 0025.8 25.7l2.7.5a449.4 449.4 0 00159 0l2.7-.5a32.05 32.05 0 0025.8-25.7l15.7-85a350 350 0 0099.7-57.6l81.3 28.9a32 32 0 0035.1-9.5l1.8-2.1c34.8-41.1 61.6-87.5 79.7-137.9l.9-2.6c4.5-12.3.8-26.3-9.3-35zM788.3 465.9c2.5 15.1 3.8 30.6 3.8 46.1s-1.3 31-3.8 46.1l-6.6 40.1 74.7 63.9a370.03 370.03 0 01-42.6 73.6L721 702.8l-31.4 25.8c-23.9 19.6-50.5 35-79.3 45.8l-38.1 14.3-17.9 97a377.5 377.5 0 01-85 0l-17.9-97.2-37.8-14.5c-28.5-10.8-55-26.2-78.7-45.7l-31.4-25.9-93.4 33.2c-17-22.9-31.2-47.6-42.6-73.6l75.5-64.5-6.5-40c-2.4-14.9-3.7-30.3-3.7-45.5 0-15.3 1.2-30.6 3.7-45.5l6.5-40-75.5-64.5c11.3-26.1 25.6-50.7 42.6-73.6l93.4 33.2 31.4-25.9c23.7-19.5 50.2-34.9 78.7-45.7l37.9-14.3 17.9-97.2c28.1-3.2 56.8-3.2 85 0l17.9 97 38.1 14.3c28.7 10.8 55.4 26.2 79.3 45.8l31.4 25.8 92.8-32.9c17 22.9 31.2 47.6 42.6 73.6L781.8 426l6.5 39.9zM512 326c-97.2 0-176 78.8-176 176s78.8 176 176 176 176-78.8 176-176-78.8-176-176-176zm79.2 255.2A111.6 111.6 0 01512 614c-29.9 0-58-11.7-79.2-32.8A111.6 111.6 0 01400 502c0-29.9 11.7-58 32.8-79.2C454 401.6 482.1 390 512 390c29.9 0 58 11.6 79.2 32.8A111.6 111.6 0 01624 502c0 29.9-11.7 58-32.8 79.2z"></path></svg>
          
        </div>

        <div runat="server" id="toolbar" visible="false" style="visibility:hidden;display:none;">
            <!-- 服务器信息 -->
            <input
                type="text"
                runat="server"
                id="serverInfo"
                disabled="disabled"
                data-i18n-title="serverInfoTitle"
                title="Current connected server"
            />
            <!-- 用户信息 -->
            <input
                type="text"
                runat="server"
                id="userInfo"
                disabled="disabled"
                data-i18n-title="userInfoTitle"
                title="Current signed-in user"
            />

            <!-- 统计、调试、浏览器、缩放、重连等按钮 -->
            <input type="button" id="stat" value="Statistics: Off" onclick="toggleStatMode();" data-i18n-title="statTitle" title="Show network status and rendering info" />
            <input type="button" id="debug" value="Debug: Off" onclick="toggleDebugMode();" data-i18n-title="debugTitle" title="Show debug logs" />
            <input type="button" id="browser" value="HTML5: Off" onclick="toggleCompatibilityMode();" data-i18n-title="browserTitle" title="Toggle HTML5 and compatibility mode" />
            <input type="button" runat="server" id="scale" value="Scale: Off" onclick="toggleScaleDisplay();" data-i18n-title="scaleTitle" title="Scale based on browser size" disabled="disabled" />
            <input type="button" runat="server" id="reconnect" value="Reconnect: Off" onclick="toggleReconnectSession();" data-i18n-title="reconnectTitle" title="Automatically try to reconnect after disconnect" disabled="disabled" />

            <!-- 文本输入/屏幕键盘/剪贴板等 -->
            <input type="button" runat="server" id="keyboard" data-i18n-value="keyboardButton" value="Text" onclick="openPopup('virtualKeyboardPopup', 'VirtualKeyboard.aspx', false);" data-i18n-title="keyboardTitle" title="Send text to remote session" disabled="disabled" />
            <input type="button" runat="server" id="osk" data-i18n-value="oskButton" value="Keyboard" onclick="openPopup('onScreenKeyboardPopup', 'OnScreenKeyboard.aspx', false);" data-i18n-title="oskTitle" title="Show on-screen keyboard" disabled="disabled" />
            <input type="button" runat="server" id="clipboard" data-i18n-value="clipboardButton" value="Clipboard" onclick="openPopup('pasteClipboardPopup', 'PasteClipboard.aspx', false);" data-i18n-title="clipboardTitle" title="Sync text to the remote clipboard" disabled="disabled" />
            <input type="button" runat="server" id="files" data-i18n-value="filesButton" value="Files" onclick="openPopup('fileStoragePopup', 'FileStorage.aspx');" data-i18n-title="filesTitle" title="Upload or download files" disabled="disabled" />
            <input type="button" runat="server" id="cad" data-i18n-value="cadButton" value="Ctrl+Alt+Del" onclick="sendCtrlAltDel();" data-i18n-title="cadTitle" title="Send Ctrl+Alt+Del" disabled="disabled" />
            <input type="button" runat="server" id="mrc" value="Right Click: Off" onclick="toggleRightClick(this);" data-i18n-title="mrcTitle" title="Simulate right click on the next touch or click" disabled="disabled" />
            <input type="button" runat="server" id="vswipe" value="Swipe: On" onclick="toggleVerticalSwipe(this);" data-i18n-title="vswipeTitle" title="Touchscreen vertical swipe (experimental)" disabled="disabled" />
            <input type="button" runat="server" id="share" data-i18n-value="shareButton" value="Share" onclick="openPopup('shareSessionPopup', 'ShareSession.aspx');" data-i18n-title="shareTitle" title="Generate a session sharing link" disabled="disabled" />
            <input type="button" runat="server" id="disconnect" data-i18n-value="disconnectButton" value="Disconnect" onclick="doDisconnect();" data-i18n-title="disconnectTitle" title="Disconnect the remote session" disabled="disabled" />

            <!-- 图像质量控制 -->
            <input type="range" runat="server" id="imageQuality" min="5" max="90" step="5" onchange="changeImageQuality(this.value);updateRange();" data-i18n-title="imageQualityTitle" title="Image quality" disabled="disabled" />

            <!-- 网络 & 调试信息区域 -->
            <div id="statDiv"></div>
            <div id="debugDiv"></div>
        </div>

        <!-- 远程桌面显示 -->
        <div id="display-mid">
        <div id="displayDiv"></div>
        </div>
        <!-- 后台辅助区 -->
        <div id="cacheDiv"></div>
        <div id="msgDiv"></div>
        <div id="kbhDiv"></div>
        <div id="bgfDiv"></div>

        <!-- 拖拽弹窗 -->
        <div id="dragDiv">
            <div id="dragHandle"><button type="button" class="closebtn" onclick="closePopup(); return false;">❌</button></div>
        </div>
    </form>

   
    <script type="text/javascript" language="javascript" defer="defer">
        var appTranslations = {
            en: {
                appTitle: 'Myrtille',
                sendCtrlAltDel: 'Ctrl + Alt + Del',
                sendCtrlAltDelTitle: 'Ctrl+Alt+Del',
                languageSwitcherTitle: 'Switch language',
                loginTitle: 'Sign in to VNC',
                hostTypeLabel: 'Protocol',
                hostTypeTitle: 'Select connection protocol',
                rdpVmBus: 'RDP (Virtual Machine Bus)',
                securityProtocolLabel: 'Security',
                securityProtocolTitle: 'NLA = most secure; RDP = best compatibility; use AUTO if unsure',
                serverLabel: 'Server (:port)',
                serverTitle: 'Server hostname or IP address (optional custom port)',
                vmGuidLabel: 'VM GUID',
                vmGuidTitle: 'Hyper-V virtual machine GUID to connect',
                vmEnhancedModeLabel: 'Enhanced mode',
                vmEnhancedModeTitle: 'Supports faster display and clipboard/printer redirection',
                domainLabel: 'Domain (optional)',
                domainTitle: 'Domain information, if applicable',
                userLabel: 'Username',
                userTitle: 'Sign-in username',
                passwordLabel: 'Password',
                passwordTitle: 'Sign-in password',
                mfaProviderTitle: 'MFA provider',
                mfaPasswordTitle: 'MFA code',
                programLabel: 'Program to start (optional)',
                programTitle: 'Executable path, name, and arguments (optional)',
                connectButton: 'Sign In',
                connectTitle: 'Start remote session',
                websiteTitle: 'Myrtille website',
                adminText: 'Host Management',
                enterpriseUserInfoTitle: 'Signed-in user info',
                newRdpHost: 'New RDP Host',
                newRdpHostTitle: 'Create a new RDP host configuration',
                newSshHost: 'New SSH Host',
                newSshHostTitle: 'Create a new SSH host configuration',
                logoutButton: 'Sign Out',
                logoutTitle: 'Sign out current user',
                hostLinkTitle: 'Connect',
                serverInfoTitle: 'Current connected server',
                userInfoTitle: 'Current signed-in user',
                statTitle: 'Show network status and rendering info',
                debugTitle: 'Show debug logs',
                browserTitle: 'Toggle HTML5 and compatibility mode',
                scaleTitle: 'Scale based on browser size',
                reconnectTitle: 'Automatically try to reconnect after disconnect',
                keyboardButton: 'Text',
                keyboardTitle: 'Send text to remote session',
                oskButton: 'Keyboard',
                oskTitle: 'Show on-screen keyboard',
                clipboardButton: 'Clipboard',
                clipboardTitle: 'Sync text to the remote clipboard',
                filesButton: 'Files',
                filesTitle: 'Upload or download files',
                cadButton: 'Ctrl+Alt+Del',
                cadTitle: 'Send Ctrl+Alt+Del',
                mrcTitle: 'Simulate right click on the next touch or click',
                vswipeTitle: 'Touchscreen vertical swipe (experimental)',
                shareButton: 'Share',
                shareTitle: 'Generate a session sharing link',
                disconnectButton: 'Disconnect',
                disconnectTitle: 'Disconnect the remote session',
                imageQualityTitle: 'Image quality',
                mobileKeyboardTitle: 'Open system keyboard',
                statLabel: 'Statistics',
                debugLabel: 'Debug',
                browserLabel: 'HTML5',
                scaleLabel: 'Scale',
                reconnectLabel: 'Reconnect',
                mrcLabel: 'Right Click',
                vswipeLabel: 'Swipe',
                stateOn: 'On',
                stateOff: 'Off',
                initErrorPrefix: 'Myrtille initialization error: ',
                vswipeDisabledOnIe: 'This experimental feature is disabled on IE/Edge'
            },
            'zh-CN': {
                appTitle: 'Myrtille',
                sendCtrlAltDel: 'Ctrl + Alt + Del',
                sendCtrlAltDelTitle: 'Ctrl+Alt+Del',
                languageSwitcherTitle: '切换语言',
                loginTitle: '登录 VNC',
                hostTypeLabel: '协议',
                hostTypeTitle: '选择连接协议',
                rdpVmBus: 'RDP（虚拟机总线）',
                securityProtocolLabel: '安全性',
                securityProtocolTitle: 'NLA = 最安全；RDP = 兼容性好；若不确定可选 AUTO',
                serverLabel: '服务器（:端口）',
                serverTitle: '服务器主机名或 IP 地址（可带非默认端口）',
                vmGuidLabel: '虚拟机 GUID',
                vmGuidTitle: '要连接的 Hyper-V 虚拟机 GUID',
                vmEnhancedModeLabel: '增强模式',
                vmEnhancedModeTitle: '支持更快的显示和剪贴板/打印机重定向',
                domainLabel: '域（可选）',
                domainTitle: '若适用，可填域信息',
                userLabel: '用户名',
                userTitle: '登录用户名',
                passwordLabel: '密码',
                passwordTitle: '登录密码',
                mfaProviderTitle: '多因素认证提供者',
                mfaPasswordTitle: '多因素认证代码',
                programLabel: '要运行的程序（可选）',
                programTitle: '可执行文件路径、名称及参数（可选）',
                connectButton: '立即登录',
                connectTitle: '建立远程会话',
                websiteTitle: 'Myrtille 官网',
                adminText: '主机管理',
                enterpriseUserInfoTitle: '登录用户信息',
                newRdpHost: '新建 RDP 主机',
                newRdpHostTitle: '创建新的 RDP 主机配置',
                newSshHost: '新建 SSH 主机',
                newSshHostTitle: '创建新的 SSH 主机配置',
                logoutButton: '注销',
                logoutTitle: '注销当前登录',
                hostLinkTitle: '连接',
                serverInfoTitle: '当前连接的服务器',
                userInfoTitle: '当前登录用户',
                statTitle: '显示网络状态及渲染信息',
                debugTitle: '显示调试日志',
                browserTitle: '切换 HTML5 和兼容模式',
                scaleTitle: '根据浏览器大小进行缩放',
                reconnectTitle: '断线后自动尝试重连',
                keyboardButton: '文本',
                keyboardTitle: '向远程会话输入文本',
                oskButton: '键盘',
                oskTitle: '显示屏幕虚拟键盘',
                clipboardButton: '剪贴板',
                clipboardTitle: '将文本同步到远程剪贴板',
                filesButton: '文件',
                filesTitle: '上传/下载文件',
                cadButton: 'Ctrl+Alt+Del',
                cadTitle: '发送 Ctrl+Alt+Del',
                mrcTitle: '下次触摸或点击时模拟右键',
                vswipeTitle: '触屏设备上下滑动（实验）',
                shareButton: '共享',
                shareTitle: '生成会话共享链接',
                disconnectButton: '断开',
                disconnectTitle: '断开远程会话',
                imageQualityTitle: '图像质量',
                mobileKeyboardTitle: '打开系统输入法',
                statLabel: '统计',
                debugLabel: '调试',
                browserLabel: 'HTML5',
                scaleLabel: '缩放',
                reconnectLabel: '重连',
                mrcLabel: '右键',
                vswipeLabel: '滑动',
                stateOn: '开启',
                stateOff: '关闭',
                initErrorPrefix: 'Myrtille 初始化显示时发生错误: ',
                vswipeDisabledOnIe: '此实验功能在 IE/Edge 下不可用'
            }
        };

        var appLanguage = 'en';

        function getTranslation(key) {
            var locale = appTranslations[appLanguage] || appTranslations.en;
            return locale[key] || appTranslations.en[key] || key;
        }

        function formatToggleLabel(key, enabled) {
            return getTranslation(key) + ': ' + getTranslation(enabled ? 'stateOn' : 'stateOff');
        }

        function setInputValue(id, value) {
            var element = document.getElementById(id);
            if (element) {
                element.value = value;
            }
        }

        function resolveToggleState(value, defaultState) {
            if (!value) {
                return defaultState;
            }

            if (value.indexOf('On') !== -1 || value.indexOf('开启') !== -1) {
                return true;
            }

            if (value.indexOf('Off') !== -1 || value.indexOf('关闭') !== -1) {
                return false;
            }

            return defaultState;
        }

        function applyStaticTranslations() {
            var textNodes = document.querySelectorAll('[data-i18n]');
            for (var i = 0; i < textNodes.length; i++) {
                var textKey = textNodes[i].getAttribute('data-i18n');
                textNodes[i].textContent = getTranslation(textKey);
            }

            var titleNodes = document.querySelectorAll('[data-i18n-title]');
            for (var j = 0; j < titleNodes.length; j++) {
                var titleKey = titleNodes[j].getAttribute('data-i18n-title');
                titleNodes[j].setAttribute('title', getTranslation(titleKey));
            }

            var valueNodes = document.querySelectorAll('[data-i18n-value]');
            for (var k = 0; k < valueNodes.length; k++) {
                var valueKey = valueNodes[k].getAttribute('data-i18n-value');
                valueNodes[k].value = getTranslation(valueKey);
            }
        }

        function updateToolbarLabels() {
            setInputValue('stat', formatToggleLabel('statLabel', getToggleCookie((parent != null && window.name !== '' ? window.name + '_' : '') + 'stat')));
            setInputValue('debug', formatToggleLabel('debugLabel', getToggleCookie((parent != null && window.name !== '' ? window.name + '_' : '') + 'debug')));
            setInputValue('browser', formatToggleLabel('browserLabel', !getToggleCookie((parent != null && window.name !== '' ? window.name + '_' : '') + 'browser')));

            var scaleButton = document.getElementById('<%=scale.ClientID%>');
            if (scaleButton) {
                var scaleEnabled = <%= (RemoteSession != null && RemoteSession.BrowserResize.HasValue && RemoteSession.BrowserResize.Value == BrowserResize.Scale).ToString(CultureInfo.InvariantCulture).ToLower() %>;
                scaleButton.value = formatToggleLabel('scaleLabel', scaleEnabled);
            }

            var reconnectButton = document.getElementById('<%=reconnect.ClientID%>');
            if (reconnectButton) {
                reconnectButton.value = formatToggleLabel('reconnectLabel', resolveToggleState(reconnectButton.value, false));
            }

            var rightClickButton = document.getElementById('<%=mrc.ClientID%>');
            if (rightClickButton) {
                rightClickButton.value = formatToggleLabel('mrcLabel', resolveToggleState(rightClickButton.value, false));
            }

            var swipeButton = document.getElementById('<%=vswipe.ClientID%>');
            if (swipeButton) {
                swipeButton.value = formatToggleLabel('vswipeLabel', resolveToggleState(swipeButton.value, true));
            }
        }

        function applyLanguage(language) {
            appLanguage = appTranslations[language] ? language : 'en';
            document.documentElement.lang = appLanguage;

            applyStaticTranslations();
            updateToolbarLabels();
        }

        function setAppLanguage(language) {
            var resolvedLanguage = appTranslations[language] ? language : 'en';
            localStorage.setItem('myrtilleLanguage', resolvedLanguage);
            applyLanguage(resolvedLanguage);
        }

        function resolveInitialLanguage() {
            var queryMatch = window.location.search.match(/[?&]lang=([^&]+)/i);
            if (queryMatch && appTranslations[decodeURIComponent(queryMatch[1])]) {
                return decodeURIComponent(queryMatch[1]);
            }

            var storedLanguage = localStorage.getItem('myrtilleLanguage');
            if (storedLanguage && appTranslations[storedLanguage]) {
                return storedLanguage;
            }

            if (navigator.language && navigator.language.toLowerCase().indexOf('zh') === 0) {
                return 'zh-CN';
            }

            return 'en';
        }

        window.setAppLanguage = setAppLanguage;
        window.myrtilleI18n = {
            getTranslation: getTranslation,
            formatToggleLabel: formatToggleLabel
        };

        var mobileKeyboardState = {
            composing: false
        };

        function focusMobileKeyboardInput() {
            var input = document.getElementById('mobileKeyboardInput');
            if (!input) {
                return;
            }

            input.focus();
            try {
                input.setSelectionRange(input.value.length, input.value.length);
            }
            catch (exc) {}
        }

        function flushMobileKeyboardInput() {
            var input = document.getElementById('mobileKeyboardInput');
            if (!input || mobileKeyboardState.composing) {
                return;
            }

            var value = input.value;
            if (!value) {
                return;
            }

            try {
                if (window.parent && typeof window.parent.sendText === 'function') {
                    window.parent.sendText(value);
                }
                else if (typeof window.sendText === 'function') {
                    window.sendText(value);
                }
                else if (window.getMyrtille && window.getMyrtille()) {
                    window.getMyrtille().sendText(value);
                }
            }
            finally {
                input.value = '';
                window.setTimeout(function() {
                    focusMobileKeyboardInput();
                }, 0);
            }
        }

        function bindMobileKeyboard() {
            var input = document.getElementById('mobileKeyboardInput');
            if (!input || input.dataset.bound === '1') {
                return;
            }

            input.dataset.bound = '1';

            input.addEventListener('compositionstart', function() {
                mobileKeyboardState.composing = true;
            });

            input.addEventListener('compositionend', function() {
                mobileKeyboardState.composing = false;
                flushMobileKeyboardInput();
            });

            input.addEventListener('input', function() {
                if (!mobileKeyboardState.composing) {
                    flushMobileKeyboardInput();
                }
            });

            input.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' && !mobileKeyboardState.composing) {
                    e.preventDefault();
                    flushMobileKeyboardInput();
                }
            });
        }

        function openMobileKeyboard() {
            bindMobileKeyboard();
            focusMobileKeyboardInput();
        }

        window.openMobileKeyboard = openMobileKeyboard;

        var dragDiv = document.getElementById('dragDiv');
        var dragHandle = document.getElementById('dragHandle');

        interact(dragDiv).draggable({
    allowFrom: dragHandle,
    onstart: function () {
        // 阻止选择、防止iframe响应鼠标事件
        document.body.style.userSelect = 'none';
        document.body.style.pointerEvents = 'none';
        dragDiv.style.pointerEvents = 'auto'; // 恢复自身事件响应
        // 可选：如果内部有 iframe
        const iframe = dragDiv.querySelector('iframe');
        if (iframe) iframe.style.pointerEvents = 'none';
    },
    onmove: onDragMove,
    onend: function () {
        // 恢复交互行为
        document.body.style.userSelect = '';
        document.body.style.pointerEvents = '';
        const iframe = dragDiv.querySelector('iframe');
        if (iframe) iframe.style.pointerEvents = '';
    }
});

function onDragMove(event) {
    var target = event.target;
    var x = (parseFloat(target.getAttribute('data-x')) || 0) + event.dx;
    var y = (parseFloat(target.getAttribute('data-y')) || 0) + event.dy;

    // 使用 transform 移动元素
    target.style.transform = 'translate(' + x + 'px, ' + y + 'px)';
    target.setAttribute('data-x', x);
    target.setAttribute('data-y', y);
}

        applyLanguage(resolveInitialLanguage());
        bindMobileKeyboard();

        // 初始化
        initDisplay();

        
        if (
            window.location.href.indexOf('&connect=') !== -1 &&
            (
                window.location.href.indexOf('&width=') === -1 ||
                window.location.href.indexOf('&height=') === -1
            )
        ) {
            var width = document.getElementById('<%=width.ClientID%>').value;
            var height = document.getElementById('<%=height.ClientID%>').value;
            var redirectUrl = window.location.href;

            if (window.location.href.indexOf('&width=') === -1) {
                redirectUrl += '&width=' + width;
            }
            if (window.location.href.indexOf('&height=') === -1) {
                redirectUrl += '&height=' + height;
            }
            window.location.href = redirectUrl;
        }

        function initDisplay() {
            try {
                var display = new Display();
                console.log(display);

                setClientResolution(display);

                if (<%= (RemoteSession != null 
                    && (RemoteSession.State == RemoteSessionState.Connecting 
                        || RemoteSession.State == RemoteSessionState.Connected))
                    .ToString(CultureInfo.InvariantCulture).ToLower() 
                %>) {
                    var toolbar = document.getElementById('<%=toolbar.ClientID%>');

                    if (toolbar) {
                        // 如果cookie中记录了toolbar开关，就恢复
                        if (getToggleCookie((parent != null && window.name !== '' ? window.name + '_' : '') + 'toolbar')) {
                            toggleToolbar();
                        }

                        // 统计：开启 / 关闭
                        updateToolbarLabels();

                        // IE/Edge 下禁用 vswipe
                        document.getElementById('<%= vswipe.ClientID%>').disabled =
                            document.getElementById('<%= vswipe.ClientID%>').disabled || display.isIEBrowser();
                    }
                }
            }
            catch (exc) {
                alert(getTranslation('initErrorPrefix') + exc.message);
            }
        }

        function onHostTypeChange(hostType) {
            var securityProtocolDiv = document.getElementById('securityProtocolDiv');
            if (securityProtocolDiv) {
                securityProtocolDiv.style.visibility =
                    (hostType.selectedIndex === 0 || hostType.selectedIndex === 1)
                        ? 'visible'
                        : 'hidden';
                securityProtocolDiv.style.display =
                    (hostType.selectedIndex === 0 || hostType.selectedIndex === 1)
                        ? 'flex'
                        : 'none';
            }

            var vmDiv = document.getElementById('vmDiv');
            if (vmDiv) {
                vmDiv.style.visibility = (hostType.selectedIndex === 1) ? 'visible' : 'hidden';
                vmDiv.style.display = (hostType.selectedIndex === 1) ? 'block' : 'none';
            }
        }

        function setClientResolution(display) {
            var med = document.getElementById('display-mid');
            var width = display.getBrowserWidth() - med.offsetLeft - 4;
            var height = display.getBrowserHeight() - med.offsetTop - 4;
            document.getElementById('<%=width.ClientID%>').value = width;
            
            document.getElementById('<%=height.ClientID%>').value = height;
        }

        function disableControl(controlId) {
            var control = document.getElementById(controlId);
            if (control) {
                control.disabled = true;
            }
        }
        function disableToolbar() {
            disableControl('stat');
            disableControl('debug');
            disableControl('browser');
            disableControl('<%=scale.ClientID%>');
            disableControl('<%=reconnect.ClientID%>');
            disableControl('<%=keyboard.ClientID%>');
            disableControl('<%=osk.ClientID%>');
            disableControl('<%=clipboard.ClientID%>');
            disableControl('<%=files.ClientID%>');
            disableControl('<%=cad.ClientID%>');
            disableControl('<%=mrc.ClientID%>');
            disableControl('<%=vswipe.ClientID%>');
            disableControl('<%=share.ClientID%>');
            disableControl('<%=disconnect.ClientID%>');
            disableControl('<%=imageQuality.ClientID%>');
        }

        // 显示/隐藏 工具栏
        function toggleToolbar() {
            var toolbar = document.getElementById('<%=toolbar.ClientID%>');
            if (!toolbar) return;

            if (toolbar.style.visibility === 'visible') {
                toolbar.style.visibility = 'hidden';
                toolbar.style.display = 'none';
            } else {
                toolbar.style.visibility = 'visible';
                toolbar.style.display = 'block';
            }

            setCookie(
                (parent != null && window.name !== '' ? window.name + '_' : '') + 'toolbar',
                (toolbar.style.visibility === 'visible') ? 1 : 0
            );
        }

        function getToggleCookie(name) {
            if (<%= (RemoteSession == null).ToString().ToLower() %>) {
                return false;
            }
            var value = getCookie(name);
            if (value == null) {
                return false;
            }
            return (value === '1');
        }

        // 拖拽移动
  
     
    </script>
    <script>  
         function syncCanvasSize() {
  const canvas = document.querySelector("canvas");
  if (!canvas) return;

  const width = canvas.width;
  const height = canvas.height;

  if (canvas.style.width !== width + "px" || canvas.style.height !== height + "px") {
    canvas.style.width = width + "px";
    canvas.style.height = height + "px";
    console.log("Canvas size updated:", width, height);
  }
}
const observer = new MutationObserver((mutations, obs) => {
  const canvas = document.querySelector("canvas");
  if (canvas) {
    console.log("Canvas created!", canvas);
    syncCanvasSize(); 
    obs.disconnect(); // 只执行一次，停止监听
  }
});

observer.observe(document.body, { childList: true, subtree: true });


</script>
<script>
    document.addEventListener("DOMContentLoaded", function() {
      // 定义各个主题对应的 CSS 变量值
      const themes = {
        blue: {
          "--primary-blue": "#007bff",
          "--primary-dark-blue": "#0056b3",
          "--topbar-bg": "#33b3dd",
          "--accent-blue": "#30a5ff",
          "--accent-blue-hover": "#2196f3",
          "--button-border": "#0099ff",
          "--ctf-blue": "#30a5ff",
          "--button-glow": "rgba(48, 165, 255, 0.28)",
          "--surface-tint": "rgba(255, 255, 255, 0.86)"
        },
        green: {
          "--primary-blue": "#28a745",
          "--primary-dark-blue": "#1e7e34",
          "--topbar-bg": "#28a745",
          "--accent-blue": "#28a745",
          "--accent-blue-hover": "#218838",
          "--button-border": "#28a745",
          "--ctf-blue": "#28a745",
          "--button-glow": "rgba(40, 167, 69, 0.24)",
          "--surface-tint": "rgba(250, 255, 251, 0.86)"
        },
        grey: {
          "--primary-blue": "#6c757d",
          "--primary-dark-blue": "#545b62",
          "--topbar-bg": "#6c757d",
          "--accent-blue": "#6c757d",
          "--accent-blue-hover": "#5a6268",
          "--button-border": "#6c757d",
          "--ctf-blue": "#6c757d",
          "--button-glow": "rgba(108, 117, 125, 0.24)",
          "--surface-tint": "rgba(252, 252, 252, 0.88)"
        },
        navy: {
          "--primary-blue": "#001f3f",
          "--primary-dark-blue": "#001f3f",
          "--topbar-bg": "#001f3f",
          "--accent-blue": "#001f3f",
          "--accent-blue-hover": "#001f3f",
          "--button-border": "#001f3f",
          "--ctf-blue": "#001f3f",
          "--button-glow": "rgba(0, 31, 63, 0.28)",
          "--surface-tint": "rgba(243, 247, 252, 0.88)"
        }
      };


      function applyTheme(themeName) {
        const theme = themes[themeName];
        document.body.classList.add("theme-transitioning");
        for (let variable in theme) {
          document.documentElement.style.setProperty(variable, theme[variable]);
        }

        window.setTimeout(() => {
          document.body.classList.remove("theme-transitioning");
        }, 420);

        localStorage.setItem("selectedTheme", themeName);
      }

      function updateSelectedBox(selectedTheme) {
        document.querySelectorAll(".color-box").forEach(box => {
          if (box.getAttribute("data-theme") === selectedTheme) {
            box.classList.add("selected");
          } else {
            box.classList.remove("selected");
          }
        });
      }

      // 初始化
      const storedTheme = localStorage.getItem("selectedTheme");
      if (storedTheme && themes[storedTheme]) {
        applyTheme(storedTheme);
        updateSelectedBox(storedTheme);
      } else {
      
        applyTheme("blue");
        updateSelectedBox("blue");
      }

      const colorBoxes = document.querySelectorAll(".color-box");
      colorBoxes.forEach(box => {
        box.addEventListener("click", function() {
          const themeName = this.getAttribute("data-theme");
          if (themes[themeName]) {
            applyTheme(themeName);
            updateSelectedBox(themeName);
          }
        });
      });
    });
    const loginCard = document.getElementById('login');
const displayMid = document.getElementById('display-mid');

if (window.getComputedStyle(loginCard).display !== 'none') {
  displayMid.classList.add('login-visible');
} else {
  displayMid.classList.remove('login-visible');
}

  </script>
  <script>
    (function(){
  console.log("开始注入从中间扩散的水波纹效果 CSS 样式。");
  // 动态插入 CSS 样式
  var css = `
    /* 定义水波纹的基本样式 */
    .ripple {
      position: absolute;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.3);
      pointer-events: none;
      transform: scale(0);
      transform-origin: center center;
      animation: rippleEffect 0.6s linear;
    }
    /* 定义水波纹动画，从中心扩散 */
    @keyframes rippleEffect {
      0% {
        transform: scale(0);
        opacity: 0.5;
      }
      100% {
        transform: scale(1);
        opacity: 0;
      }
    }
  `;
  var style = document.createElement('style');
  style.type = 'text/css';
  style.appendChild(document.createTextNode(css));
  document.head.appendChild(style);


 
  function addRippleEffect(el) {
  
   
    var computedStyle = window.getComputedStyle(el);
    console.log("元素 computedStyle.position:", computedStyle.position);
    if (computedStyle.position === 'static') {
      el.style.position = 'relative';

    }
    el.style.overflow = 'hidden';

    el.addEventListener('click', function(e) {

     
      var oldRipple = el.querySelector('.ripple');
      if (oldRipple) {

        oldRipple.remove();
      }
    
      var ripple = document.createElement('span');
      ripple.className = 'ripple';

     
      var rect = el.getBoundingClientRect();

      var size = Math.max(rect.width, rect.height) * 2;
      ripple.style.width = ripple.style.height = size + 'px';
      

     
      var left = (el.offsetWidth - size) / 2;
      var top = (el.offsetHeight - size) / 2;
      ripple.style.left = left + 'px';
      ripple.style.top = top + 'px';
  

    
      el.appendChild(ripple);


      
      ripple.addEventListener('animationend', function() {
       
        ripple.remove();
      });
    });
  }

  var elements = document.querySelectorAll('button, input[type="button"], a');
 
  elements.forEach(function(el) {
    addRippleEffect(el);
  });
})();
document.addEventListener("DOMContentLoaded", () => {
  function updateRangeFill(range) {
    if (!range) {
      return;
    }

    const min = Number(range.min || 0);
    const max = Number(range.max || 100);
    const value = Number(range.value || min);
    const percent = max === min ? 0 : ((value - min) / (max - min)) * 100;

    range.style.setProperty('--range-percent', percent.toFixed(2));
  }

  function bindRange(range) {
    if (!range || range.dataset.rangeBound === "1") {
      return;
    }

    range.dataset.rangeBound = "1";
    updateRangeFill(range);
    range.addEventListener('input', function() {
      updateRangeFill(range);
    });
    range.addEventListener('change', function() {
      updateRangeFill(range);
    });
  }

  function bindAllRanges(scope) {
    const context = scope || document;
    const ranges = context.querySelectorAll('input[type="range"]');
    ranges.forEach(bindRange);
  }

  window.updateRange = function(target) {
    if (target && target.target) {
      updateRangeFill(target.target);
      return;
    }

    if (target && target.tagName === 'INPUT' && target.type === 'range') {
      updateRangeFill(target);
      return;
    }

    bindAllRanges(document);
  };

  bindAllRanges(document);

  const rangeObserver = new MutationObserver(() => {
    bindAllRanges(document);
  });
  rangeObserver.observe(document.body, { childList: true, subtree: true });
});

</script>

</body>
</html>
