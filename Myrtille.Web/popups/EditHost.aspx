<%--
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

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
	</head>    

    <body onload="onEditHostSuccess();">
        
        <form method="post" runat="server">
            
            <div id="editHostPopupInner">
                <span id="editHostPopupTitle">
                    <strong>Host Configuration</strong>
                </span>
                <br/>
                <div class="editHostPopupInput">
                    <h5><label id="hostNameLabel" for="hostName">Host name</label></h5>
                    <input type="text" runat="server" id="hostName" title="host name" />
                </div>
                <div class="editHostPopupInput">
                    <h5><label id="hostAddressLabel" for="hostAddress">Host address (optional, uses hostname if not specified)</label></h5>
                    <input type="text" runat="server" id="hostAddress" title="host address" />
                </div>
                <div class="editHostPopupInput">
                    <h5><label id="groupsAccessLabel" for="groupsAccess">Domain Groups Allowed (comma separated)</label></h5>
                    <input type="text" runat="server" id="groupsAccess" title="groups access"/>
                </div>
                <div class="editHostPopupInput" runat="server" id="startProgramInput">
                    <h5><label id="startProgramLabel" for="startProgram">Start Remote App Program</label></h5>
                    <input type="text" runat="server" id="startProgram" title="remote program"/>
                </div>
                <div class="editHostPopupInput" runat="server" id="rdpSecurityInput">
                    <h5><label id="securityProtocolLabel" for="securityProtocol">RDP Security Protocol</label></h5>
                    <select runat="server" id="securityProtocol">
                        <option value="0">auto</option>
                        <option value="1">rdp</option>
                        <option value="2">tls</option>
                        <option value="3">nla</option>
                        <option value="4">nla-ext</option>
                    </select>
                </div>
                <div class="editHostPopupInput" runat="server" id="Div1">
                    <h5><label id="promptCredentialsLabel" for="promptCredentials">Prompt for Credentials</label></h5>
                    <input type="checkbox" runat="server" id="promptCredentials" title="prompt for credentials"/>
                </div>

                <br/>
                <div class="editHostPopupInput">
                    <input type="button" runat="server" id="createSessionUrl" value="Create Single Use Session URL"/>
                    <input type="button" runat="server" id="saveHost" value="Save" onserverclick="SaveHostButtonClick"/>
                    <input type="button" runat="server" id="deleteHost" value="Delete" onserverclick="DeleteHostButtonClick"/>
                    <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
                </div>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            // edit host success
            function onEditHostSuccess()
            {
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