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

<%@ Page Language="C#" Inherits="Myrtille.Web.VirtualKeyboard" Codebehind="VirtualKeyboard.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="css/Default.css"/>
	</head>    

    <body onload="focusText();">
        
        <form method="get" runat="server">
            
            <!-- virtual keyboard. for use on devices with no physical keyboard -->
            <!-- alternatively, osk.exe (the Windows on screen keyboard, located into %SystemRoot%\System32) can be used within the remote session -->
            <!-- it's especially handy on touchscreen devices and can even be run automatically on session start (https://www.cybernetman.com/kb/index.cfm/fuseaction/home.viewArticles/articleId/197) -->
            <div>
                <span id="virtualKeyboardPopupDesc">
                    Type or paste some text then click send<br/>
                    Alternatively, you can use the Windows on screen keyboard (%SystemRoot%\System32\osk.exe) within the session
                </span><hr/>
                <textarea id="virtualKeyboardPopupText" rows="10" cols="50"></textarea><br/>
                <input type="button" id="sendTextButton" value="Send" onclick="parent.sendText(virtualKeyboardPopupText.value);"/>
                <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

		    function focusText()
		    {
		        var virtualKeyboardPopupText = document.getElementById('virtualKeyboardPopupText');
		        if (virtualKeyboardPopupText != null)
                {
		            virtualKeyboardPopupText.focus();
                }
            }

		</script>

	</body>

</html>