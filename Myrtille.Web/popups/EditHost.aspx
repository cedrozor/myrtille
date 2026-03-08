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

<%@ Page Language="C#" Inherits="Myrtille.Web.EditHost" Codebehind="EditHost.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
        <script language="javascript" type="text/javascript" src="../js/popup-i18n.js"></script>
	</head>

    <body onload="onEditHostSuccess();">
        
        <form method="post" runat="server">
            
            <input type="hidden" runat="server" id="hostType"/>

            <div id="editHostPopupInner">
                <span id="editHostPopupTitle">
                    <strong data-i18n="hostConfiguration">Host Configuration</strong>
                </span>
                <br/>
                <div class="editHostPopupInput">
                    <h5><label id="hostNameLabel" for="hostName" data-i18n="hostName">Host name</label></h5>
                    <input type="text" runat="server" id="hostName" data-i18n-title="hostNameTitle" title="host name" />
                </div>
                <div class="editHostPopupInput">
                    <h5><label id="hostAddressLabel" for="hostAddress" data-i18n="hostAddress">Host address (:port) (optional, uses hostname if not specified)</label></h5>
                    <input type="text" runat="server" id="hostAddress" data-i18n-title="hostAddressTitle" title="host address" />
                </div>
                <div class="editHostPopupInput" runat="server" id="vmGuidInput">
                    <h5><label id="vmGuidLabel" for="vmGuid" data-i18n="vmGuidOptional">VM GUID (optional, if the guest is an Hyper-V VM)</label></h5>
                    <input type="text" runat="server" id="vmGuid" data-i18n-title="vmGuidDirectTitle" title="VM GUID, for direct connection" />
                </div>
                <div class="editHostPopupInput" runat="server" id="vmEnhancedModeInput">
                    <h5><label id="vmEnhancedModeLabel" for="vmEnhancedMode" data-i18n="vmEnhancedMode">VM Enhanced Mode (if supported by the VM)</label></h5>
                    <input type="checkbox" runat="server" id="vmEnhancedMode" data-i18n-title="vmEnhancedModePopupTitle" title="faster display and clipboard/printer redirection, if supported by the guest VM" />
                </div>
                <div class="editHostPopupInput" runat="server" id="groupsAccessInput">
                    <h5><label id="groupsAccessLabel" for="groupsAccess" data-i18n="groupsAccess">Domain Groups or Users Allowed (comma separated)</label></h5>
                    <input type="text" runat="server" id="groupsAccess" data-i18n-title="groupsAccessTitle" title="access restrictions"/>
                </div>
                <div class="editHostPopupInput" runat="server" id="rdpSecurityInput">
                    <h5><label id="securityProtocolLabel" for="securityProtocol" data-i18n="rdpSecurityProtocol">RDP Security Protocol</label></h5>
                    <select runat="server" id="securityProtocol" data-i18n-title="rdpSecurityTitle" title="NLA = safest, RDP = backward compatibility (if the server doesn't enforce NLA) and interactive logon (leave user and password empty); AUTO for Hyper-V VM or if not sure">
                        <option value="0" selected="selected">AUTO</option>
                        <option value="1">RDP</option>
                        <option value="2">TLS</option>
                        <option value="3">NLA</option>
                        <option value="4">NLA-EXT</option>
                    </select>
                </div>
                <div class="editHostPopupInput" runat="server" id="startProgramInput">
                    <h5><label id="startProgramLabel" for="startProgram" data-i18n="startRemoteApp">Start Remote App Program</label></h5>
                    <input type="text" runat="server" id="startProgram" data-i18n-title="startRemoteAppTitle" title="remote program to run on session start; unavailable for Hyper-V VM"/>
                </div>
                <div class="editHostPopupInput">
                    <h5><label id="promptCredentialsLabel" for="promptCredentials" data-i18n="promptCredentials">Prompt for Credentials</label></h5>
                    <input type="checkbox" runat="server" id="promptCredentials" data-i18n-title="promptCredentialsTitle" title="prompt for credentials (or use the logged in user otherwise)"/>
                </div>

                <br/>
                <div class="editHostPopupInput">
                    <input type="button" runat="server" id="createSessionUrl" data-i18n-value="createSingleUseSessionUrl" value="Create Single Use Session URL"/>
                    <input type="button" runat="server" id="saveHost" data-i18n-value="save" value="Save" onserverclick="SaveHostButtonClick"/>
                    <input type="button" runat="server" id="deleteHost" data-i18n-value="delete" value="Delete" onserverclick="DeleteHostButtonClick"/>
                    <input type="button" id="closePopupButton" data-i18n-value="close" value="Close" onclick="parent.closePopup();"/>
                </div>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            // edit host success
            function onEditHostSuccess()
            {
                popupI18n.apply();
                var idx = window.location.search.indexOf('edit=success');
                if (idx != -1)
                {
                    //alert('host was edited successfully');
                    parent.location.href = parent.location.href;
                }
            }

		</script>

	</body>

</html>
