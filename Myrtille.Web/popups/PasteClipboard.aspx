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

<%@ Page Language="C#" Inherits="Myrtille.Web.PasteClipboard" Codebehind="PasteClipboard.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
        <script language="javascript" type="text/javascript" src="../js/tools/convert.js"></script>
        <script language="javascript" type="text/javascript" src="../js/popup-i18n.js"></script>
	</head>

    <body onload="focusText();">
        
        <form method="get" runat="server">
            
            <div>
                <span id="pasteClipboardPopupDesc">
                    <span data-i18n="pasteClipboardDesc">Type or paste some text then click send</span>
                </span><hr/>
                <textarea id="pasteClipboardPopupText" rows="10" cols="50"></textarea><br/>
                <!--<input type="button" id="pasteClipboardButton" value="Paste" onclick="pasteClipboard();"/>-->
                <input type="button" id="sendClipboardButton" data-i18n-value="send" value="Send" onclick="sendClipboard();"/>
                <input type="button" id="closePopupButton" data-i18n-value="close" value="Close" onclick="parent.closePopup();"/>
            </div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

		    function focusText()
            {
                popupI18n.apply();
		        var pasteClipboardPopupText = document.getElementById('pasteClipboardPopupText');
		        if (pasteClipboardPopupText != null)
                {
		            pasteClipboardPopupText.focus();
                }
            }

            // unlike the "copy" command, "paste" doesn't work from plain javascript
            // it requires an extension (https://stackoverflow.com/questions/39245001/cross-browser-javascript-paste)

            /*
            function pasteClipboard()
            {
                try
                {
		            var pasteClipboardPopupText = document.getElementById('pasteClipboardPopupText');
		            if (pasteClipboardPopupText != null)
                    {
                        pasteClipboardPopupText.focus();
                        document.execCommand('paste');
                    }
                }
                catch (exc)
                {
                    alert('failed to paste clipboard (' + exc.message + ')');
                }
            }
            */

            function sendClipboard()
            {
                try
                {
                    var pasteClipboardPopupText = document.getElementById('pasteClipboardPopupText');
                    if (pasteClipboardPopupText != null && pasteClipboardPopupText.value != '')
                    {
                        // CR/LF is somewhat inverted into the textarea (?)
                        var textWithLineBreaks = pasteClipboardPopupText.value.replace(/\n\r?/g, '\r\n');

                        // send the clipboard text as unicode code points
                        parent.getMyrtille().getNetwork().send(parent.getMyrtille().getCommandEnum().SEND_LOCAL_CLIPBOARD.text + strToUnicode(textWithLineBreaks));

                        // update the local clipboard
		                pasteClipboardPopupText.focus();
                        pasteClipboardPopupText.select();
                        if (document.execCommand('copy'))
                        {
                            // IE BUG: IE always prompts the user whether or not to confirm the copy operation
                            // that's fine, but it always returns true on execCommand even if the copy was denied (...)
                            // don't store the textarea value because it may not match the local clipboard value
                            // working on Edge and every other browsers
                            if (!parent.getMyrtille().getDisplay().isIEBrowser())
                            {
                                alert(popupI18n.t('clipboardSyncSuccess'));
                            }
                            else
                            {
                                alert(popupI18n.t('clipboardSyncIe'));
                            }
                        }
                        else
                        {
                            alert(popupI18n.t('clipboardSyncFailed'));
                        }
                    }
                }
                catch (exc)
                {
                    alert(popupI18n.t('clipboardSendFailed') + exc.message);
                }
            }

		</script>

	</body>

</html>
