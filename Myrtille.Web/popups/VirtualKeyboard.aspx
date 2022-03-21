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

<%@ Page Language="C#" Inherits="Myrtille.Web.VirtualKeyboard" Codebehind="VirtualKeyboard.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="../css/Default.css"/>
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
                <input type="button" id="sendTextButton" value="Send" onclick="keyboardSendText(virtualKeyboardPopupText.value);"/>
                <input type="button" id="sendTextClipboardButton" value="From Clipboard" onclick="keyboardSendClipboardText();"/>
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

            function KeycodeInfo(keyCode, keyShift, keyAltGr = false) {
                this.KeyCode = keyCode;
                this.KeyShift = keyShift;
                this.KeyAltGr = keyAltGr;
            }

            function keyboardSendClipboardText() {
                if (navigator.clipboard) {
                    navigator.clipboard.readText().then(function (text) {
                        keyboardSendText(text);
                    });
                }
            }

            function keyboardSendText(text) {
                // enhanced mode: unicode
                if (!parent.getMyrtille().getConfig().getVMNotEnhanced()) {
                    parent.sendText(text);
                } else {
                    // no enhanced mode: emulate key strokes
                    let keycodes = getKeyCodes(parent.getMyrtille().getConfig().getVmKeyboardLayout());

                    for (var i = 0; i < text.length; i++) {
                        var char = text[i];
                        var keycode = keycodes[char];

                        // shift
                        if (keycode.KeyShift)
                            parent.sendKey(16, false);

                        // alt gr
                        if (keycode.KeyAltGr) {
                            parent.sendKey(17, false);
                            parent.sendKey(18, false);
                        }

                        // actual key
                        parent.sendKey(keycode.KeyCode, true);

                        // release shift
                        if (keycode.KeyShift)
                            parent.sendKey(16, true);

                        // release alt gr
                        if (keycode.KeyAltGr) {
                            parent.sendKey(17, true);
                            parent.sendKey(18, true);
                        }
                    }
                }
            }

            function getKeyCodes(keyboardLayout) {
                if (keyboardLayout == 'en-us') {
                    var keycodes = new Array();
                    keycodes[' '] = new KeycodeInfo(32, false);
                    keycodes['0'] = new KeycodeInfo(48, false);
                    keycodes[')'] = new KeycodeInfo(48, true);
                    keycodes['1'] = new KeycodeInfo(49, false);
                    keycodes['!'] = new KeycodeInfo(49, true);
                    keycodes['2'] = new KeycodeInfo(50, false);
                    keycodes['@'] = new KeycodeInfo(50, true);
                    keycodes['3'] = new KeycodeInfo(51, false);
                    keycodes['#'] = new KeycodeInfo(51, true);
                    keycodes['4'] = new KeycodeInfo(52, false);
                    keycodes['$'] = new KeycodeInfo(52, true);
                    keycodes['5'] = new KeycodeInfo(53, false);
                    keycodes['%'] = new KeycodeInfo(53, true);
                    keycodes['6'] = new KeycodeInfo(54, false);
                    keycodes['^'] = new KeycodeInfo(54, true);
                    keycodes['7'] = new KeycodeInfo(55, false);
                    keycodes['&'] = new KeycodeInfo(55, true);
                    keycodes['8'] = new KeycodeInfo(56, false);
                    keycodes['*'] = new KeycodeInfo(56, true);
                    keycodes['9'] = new KeycodeInfo(57, false);
                    keycodes['('] = new KeycodeInfo(57, true);
                    keycodes['a'] = new KeycodeInfo(65, false);
                    keycodes['A'] = new KeycodeInfo(65, true);
                    keycodes['b'] = new KeycodeInfo(66, false);
                    keycodes['B'] = new KeycodeInfo(66, true);
                    keycodes['c'] = new KeycodeInfo(67, false);
                    keycodes['C'] = new KeycodeInfo(67, true);
                    keycodes['d'] = new KeycodeInfo(68, false);
                    keycodes['D'] = new KeycodeInfo(68, true);
                    keycodes['e'] = new KeycodeInfo(69, false);
                    keycodes['E'] = new KeycodeInfo(69, true);
                    keycodes['f'] = new KeycodeInfo(70, false);
                    keycodes['F'] = new KeycodeInfo(70, true);
                    keycodes['g'] = new KeycodeInfo(71, false);
                    keycodes['G'] = new KeycodeInfo(71, true);
                    keycodes['h'] = new KeycodeInfo(72, false);
                    keycodes['H'] = new KeycodeInfo(72, true);
                    keycodes['i'] = new KeycodeInfo(73, false);
                    keycodes['I'] = new KeycodeInfo(73, true);
                    keycodes['j'] = new KeycodeInfo(74, false);
                    keycodes['J'] = new KeycodeInfo(74, true);
                    keycodes['k'] = new KeycodeInfo(75, false);
                    keycodes['K'] = new KeycodeInfo(75, true);
                    keycodes['l'] = new KeycodeInfo(76, false);
                    keycodes['L'] = new KeycodeInfo(76, true);
                    keycodes['m'] = new KeycodeInfo(77, false);
                    keycodes['M'] = new KeycodeInfo(77, true);
                    keycodes['n'] = new KeycodeInfo(78, false);
                    keycodes['N'] = new KeycodeInfo(78, true);
                    keycodes['o'] = new KeycodeInfo(79, false);
                    keycodes['O'] = new KeycodeInfo(79, true);
                    keycodes['p'] = new KeycodeInfo(80, false);
                    keycodes['P'] = new KeycodeInfo(80, true);
                    keycodes['q'] = new KeycodeInfo(81, false);
                    keycodes['Q'] = new KeycodeInfo(81, true);
                    keycodes['r'] = new KeycodeInfo(82, false);
                    keycodes['R'] = new KeycodeInfo(82, true);
                    keycodes['s'] = new KeycodeInfo(83, false);
                    keycodes['S'] = new KeycodeInfo(83, true);
                    keycodes['t'] = new KeycodeInfo(84, false);
                    keycodes['T'] = new KeycodeInfo(84, true);
                    keycodes['u'] = new KeycodeInfo(85, false);
                    keycodes['U'] = new KeycodeInfo(85, true);
                    keycodes['v'] = new KeycodeInfo(86, false);
                    keycodes['V'] = new KeycodeInfo(86, true);
                    keycodes['w'] = new KeycodeInfo(87, false);
                    keycodes['W'] = new KeycodeInfo(87, true);
                    keycodes['x'] = new KeycodeInfo(88, false);
                    keycodes['X'] = new KeycodeInfo(88, true);
                    keycodes['y'] = new KeycodeInfo(89, false);
                    keycodes['Y'] = new KeycodeInfo(89, true);
                    keycodes['z'] = new KeycodeInfo(90, false);
                    keycodes['Z'] = new KeycodeInfo(90, true);
                    keycodes[';'] = new KeycodeInfo(186, false);
                    keycodes[':'] = new KeycodeInfo(186, true);
                    keycodes['='] = new KeycodeInfo(187, false);
                    keycodes['+'] = new KeycodeInfo(187, true);
                    keycodes[','] = new KeycodeInfo(188, false);
                    keycodes['<'] = new KeycodeInfo(188, true);
                    keycodes['-'] = new KeycodeInfo(189, false);
                    keycodes['_'] = new KeycodeInfo(189, true);
                    keycodes['.'] = new KeycodeInfo(190, false);
                    keycodes['>'] = new KeycodeInfo(190, true);
                    keycodes['/'] = new KeycodeInfo(191, false);
                    keycodes['?'] = new KeycodeInfo(191, true);
                    keycodes['`'] = new KeycodeInfo(192, false);
                    keycodes['~'] = new KeycodeInfo(192, true);
                    keycodes['['] = new KeycodeInfo(219, false);
                    keycodes['{'] = new KeycodeInfo(219, true);
                    keycodes['\\'] = new KeycodeInfo(220, false);
                    keycodes['|'] = new KeycodeInfo(220, true);
                    keycodes[']'] = new KeycodeInfo(221, false);
                    keycodes['}'] = new KeycodeInfo(221, true);
                    keycodes['\''] = new KeycodeInfo(222, false);
                    keycodes['"'] = new KeycodeInfo(222, true);
                    return keycodes;
                } else if (keyboardLayout == 'de-de') {
                    var keycodes = new Array();
                    keycodes[' '] = new KeycodeInfo(32, false);
                    keycodes['1'] = new KeycodeInfo(49, false);
                    keycodes['!'] = new KeycodeInfo(49, true);
                    keycodes['2'] = new KeycodeInfo(50, false);
                    keycodes['"'] = new KeycodeInfo(50, true);
                    keycodes['3'] = new KeycodeInfo(51, false);
                    keycodes['§'] = new KeycodeInfo(51, true);
                    keycodes['4'] = new KeycodeInfo(52, false);
                    keycodes['$'] = new KeycodeInfo(52, true);
                    keycodes['5'] = new KeycodeInfo(53, false);
                    keycodes['%'] = new KeycodeInfo(53, true);
                    keycodes['6'] = new KeycodeInfo(54, false);
                    keycodes['&'] = new KeycodeInfo(54, true);
                    keycodes['7'] = new KeycodeInfo(55, false);
                    keycodes['/'] = new KeycodeInfo(55, true);
                    keycodes['{'] = new KeycodeInfo(55, false, true);
                    keycodes['8'] = new KeycodeInfo(56, false);
                    keycodes['('] = new KeycodeInfo(56, true);
                    keycodes['['] = new KeycodeInfo(56, false, true);
                    keycodes['9'] = new KeycodeInfo(57, false);
                    keycodes[')'] = new KeycodeInfo(57, true);
                    keycodes[']'] = new KeycodeInfo(57, false, true);
                    keycodes['0'] = new KeycodeInfo(48, false);
                    keycodes['='] = new KeycodeInfo(48, true);
                    keycodes['}'] = new KeycodeInfo(48, false, true);
                    keycodes['a'] = new KeycodeInfo(65, false);
                    keycodes['A'] = new KeycodeInfo(65, true);
                    keycodes['b'] = new KeycodeInfo(66, false);
                    keycodes['B'] = new KeycodeInfo(66, true);
                    keycodes['c'] = new KeycodeInfo(67, false);
                    keycodes['C'] = new KeycodeInfo(67, true);
                    keycodes['d'] = new KeycodeInfo(68, false);
                    keycodes['D'] = new KeycodeInfo(68, true);
                    keycodes['e'] = new KeycodeInfo(69, false);
                    keycodes['E'] = new KeycodeInfo(69, true);
                    keycodes['€'] = new KeycodeInfo(69, false, true);
                    keycodes['f'] = new KeycodeInfo(70, false);
                    keycodes['F'] = new KeycodeInfo(70, true);
                    keycodes['g'] = new KeycodeInfo(71, false);
                    keycodes['G'] = new KeycodeInfo(71, true);
                    keycodes['h'] = new KeycodeInfo(72, false);
                    keycodes['H'] = new KeycodeInfo(72, true);
                    keycodes['i'] = new KeycodeInfo(73, false);
                    keycodes['I'] = new KeycodeInfo(73, true);
                    keycodes['j'] = new KeycodeInfo(74, false);
                    keycodes['J'] = new KeycodeInfo(74, true);
                    keycodes['k'] = new KeycodeInfo(75, false);
                    keycodes['K'] = new KeycodeInfo(75, true);
                    keycodes['l'] = new KeycodeInfo(76, false);
                    keycodes['L'] = new KeycodeInfo(76, true);
                    keycodes['m'] = new KeycodeInfo(77, false);
                    keycodes['M'] = new KeycodeInfo(77, true);
                    keycodes['n'] = new KeycodeInfo(78, false);
                    keycodes['N'] = new KeycodeInfo(78, true);
                    keycodes['o'] = new KeycodeInfo(79, false);
                    keycodes['O'] = new KeycodeInfo(79, true);
                    keycodes['p'] = new KeycodeInfo(80, false);
                    keycodes['P'] = new KeycodeInfo(80, true);
                    keycodes['q'] = new KeycodeInfo(81, false);
                    keycodes['Q'] = new KeycodeInfo(81, true);
                    keycodes['@'] = new KeycodeInfo(81, false, true);
                    keycodes['r'] = new KeycodeInfo(82, false);
                    keycodes['R'] = new KeycodeInfo(82, true);
                    keycodes['s'] = new KeycodeInfo(83, false);
                    keycodes['S'] = new KeycodeInfo(83, true);
                    keycodes['t'] = new KeycodeInfo(84, false);
                    keycodes['T'] = new KeycodeInfo(84, true);
                    keycodes['u'] = new KeycodeInfo(85, false);
                    keycodes['U'] = new KeycodeInfo(85, true);
                    keycodes['v'] = new KeycodeInfo(86, false);
                    keycodes['V'] = new KeycodeInfo(86, true);
                    keycodes['w'] = new KeycodeInfo(87, false);
                    keycodes['W'] = new KeycodeInfo(87, true);
                    keycodes['x'] = new KeycodeInfo(88, false);
                    keycodes['X'] = new KeycodeInfo(88, true);
                    keycodes['y'] = new KeycodeInfo(89, false);
                    keycodes['Y'] = new KeycodeInfo(89, true);
                    keycodes['z'] = new KeycodeInfo(90, false);
                    keycodes['Z'] = new KeycodeInfo(90, true);
                    keycodes['ß'] = new KeycodeInfo(219, false);
                    keycodes['?'] = new KeycodeInfo(219, true);
                    keycodes['\\'] = new KeycodeInfo(219, false, true);
                    keycodes['ü'] = new KeycodeInfo(186, false);
                    keycodes['Ü'] = new KeycodeInfo(186, true);
                    keycodes['ä'] = new KeycodeInfo(222, false);
                    keycodes['Ä'] = new KeycodeInfo(222, true);
                    keycodes['ö'] = new KeycodeInfo(192, false);
                    keycodes['Ö'] = new KeycodeInfo(192, true);
                    keycodes[','] = new KeycodeInfo(188, false);
                    keycodes[';'] = new KeycodeInfo(188, true);
                    keycodes['.'] = new KeycodeInfo(190, false);
                    keycodes[':'] = new KeycodeInfo(190, true);
                    keycodes['-'] = new KeycodeInfo(189, false);
                    keycodes['_'] = new KeycodeInfo(189, true);
                    keycodes['#'] = new KeycodeInfo(191, false);
                    keycodes["'"] = new KeycodeInfo(191, true);
                    keycodes['+'] = new KeycodeInfo(187, false);
                    keycodes['*'] = new KeycodeInfo(187, true);
                    keycodes['~'] = new KeycodeInfo(187, false, true);
                    keycodes['^'] = new KeycodeInfo(220, false);
                    keycodes['°'] = new KeycodeInfo(220, true);
                    keycodes['´'] = new KeycodeInfo(221, false);
                    keycodes['`'] = new KeycodeInfo(221, true);
                    keycodes['<'] = new KeycodeInfo(226, false);
                    keycodes['>'] = new KeycodeInfo(226, true);
                    keycodes['|'] = new KeycodeInfo(226, false, true);

                    return keycodes;
                }
            }

        </script>

	</body>

</html>