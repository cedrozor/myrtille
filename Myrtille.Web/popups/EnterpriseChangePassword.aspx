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

<%@ Page Language="C#" Inherits="Myrtille.Web.EnterpriseChangePassword" Codebehind="EnterpriseChangePassword.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
        <script language="javascript" type="text/javascript" src="../js/popup-i18n.js"></script>
	</head>

    <body onload="onChangeSuccess();">
        
        <form method="post" runat="server">
            
            <div id="changePasswordPopupInner">
                <span id="changePasswordPopupTitle">
                    <strong data-i18n="changePassword">Change Password</strong>
                </span>
                <br/>
                <p class="changePasswordMessage" data-i18n="changePasswordMessage">Your password has expired and must be changed</p>
               
                <div class="changePasswordPopupInput">
                    <h5><label id="userNameLabel" for="hostName" data-i18n="userName">User name</label></h5>
                    <input type="text" runat="server" id="userName" data-i18n-title="userNameTitle" title="user name" readonly="readonly" />
                </div>
                <div class="changePasswordPopupInput">
                    <h5><label id="oldPasswordLabel" for="oldPassword" data-i18n="oldPassword">Old Password</label></h5>
                    <input type="password" runat="server" id="oldPassword" data-i18n-title="oldPasswordTitle" title="Old Password" />
                </div>
                <div class="changePasswordPopupInput">
                    <h5><label id="newPasswordLabel" for="newPassword" data-i18n="newPassword">New Password</label></h5>
                    <input type="password" runat="server" id="newPassword" data-i18n-title="newPasswordTitle" title="new password"/>
                </div>
                <div class="changePasswordPopupInput">
                    <h5><label id="confirmPasswordLabel" for="confirmPassword" data-i18n="confirmPassword">Confirm Password</label></h5>
                    <input type="password" runat="server" id="confirmPassword" data-i18n-title="confirmPasswordTitle" title="confirm password"/>
                </div>
                <label runat="server" id="changeError" class="changeError"></label>
                <br/>
                <br />
                <div class="changePasswordPopupInput">
                    <input type="submit" runat="server" id="changePassword" data-i18n-value="changePassword" value="Change Password" onserverclick="ChangePasswordButtonClick"/>
                    <input type="button" id="closePopupButton" data-i18n-value="close" value="Close" onclick="parent.closePopup();"/>
                </div>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            // change password success
            function onChangeSuccess()
            {
                popupI18n.apply();
                var idx = window.location.search.indexOf('change=success');
                if (idx != -1)
                {
                    parent.location.href = parent.location.href;
                }
            }

		</script>

	</body>

</html>
