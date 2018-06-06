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

<%@ Page Language="C#" Inherits="Myrtille.Web.EditHostSession" Codebehind="EditHostSession.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
	</head>    

    <body onload="selectText();">
        
        <form method="post" runat="server">
            
            <div id="editHostSessionPopupInner">
                <span id="editHostSessionPopupTitle">
                    <strong>Create Host Session for <span runat="server" id="hostName"></span></strong>
                </span>
                <br/>
                <div class="editHostSessionPopupInput">
                    <h5><label id="userNameLabel" for="userName">User name</label></h5>
                    <input type="text" runat="server" id="userName" title="user name" />
                </div>
                <div class="editHostSessionPopupInput">
                    <h5><label id="userPasswordLabel" for="userPassword">User password</label></h5>
                    <input type="password" runat="server" id="userPassword" title="user password" />
                </div>
                <div class="editHostSessionPopupInput">
                    <h5>Session URL</h5>
                    <input type="button" runat="server" id="createSessionUrl" value="Create" onserverclick="CreateSessionUrlButtonClick"/>
                    <br/>
                    <br/>
                    <textarea runat="server" id="sessionUrl" readonly="readonly" rows="4" cols="50"></textarea>
                    <span><h5>Copy the URL and use how required; it can only be used once</h5></span>
                </div>

                <div class="editHostSessionPopupInput">
                    <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
                </div>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

		    function selectText()
		    {
		        var sessionUrl = document.getElementById('sessionUrl');
		        if (sessionUrl != null)
                {
		            sessionUrl.focus();
		            sessionUrl.select();
                }
            }

		</script>

	</body>

</html>