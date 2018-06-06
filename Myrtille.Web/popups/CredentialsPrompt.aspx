<%--
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2018 Paul Oliver

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

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CredentialsPrompt.aspx.cs" Inherits="Myrtille.Web.popups.CredentialsPrompt" Culture="auto" UICulture="auto"  %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
	</head>    

    <body onload="onCredentialSuccess();">
        
        <form method="post" runat="server">
            
            <div id="editCredentialPopupInner">
                <input type="hidden" id="hostID" runat="server" />
                <span id="editCredentialsPopupTitle">
                    <strong>Login Credentials</strong>
                </span>
                <br/>
                <div class="editCredentialPopupInput">
                    <h5><label id="usernameLabel" for="promptUserName">Username</label></h5>
                    <input type="text" runat="server" id="promptUserName" title="user name" tabindex="1" />
                </div>
                <div class="editCredentialPopupInput">
                    <h5><label id="passwordLabel" for="promptPassword">Password</label></h5>
                    <input type="password" runat="server" id="promptPassword" title="password" />
                </div>
                <br/>
                <div class="editCredentialPopupInput">
                    <input type="button" runat="server" id="ConnectHost" value="Connect" onserverclick="ConnectButtonClick"/>
                    <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
                </div>
            </div>
        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            // edit host success
            function onCredentialSuccess()
            {
                var idx = window.location.search.indexOf('edit=success');
                if (idx != -1)
                {
                    var hostID = document.getElementById('<%=hostID.ClientID%>');

                    var pathname = '';
                    var parts = new Array();
                    parts = parent.location.pathname.split('/');
                    for (var i = 0; i < parts.length - 1; i++)
                    {
                        if (parts[i] != '')
                        {
                            if (pathname == '')
                            {
                                pathname = parts[i];
                            }
                            else
                            {
                                pathname += '/' + parts[i];
                            }
                        }
                    }
    
                    var connectUrl = '/' + pathname + '/?SD=' + hostID.value + '&__EVENTTARGET=&__EVENTARGUMENT=&connect=Connect%21';

                    parent.location.href = connectUrl ;
                }
            }

		</script>

	</body>

</html>