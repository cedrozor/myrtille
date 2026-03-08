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

<%@ Page Language="C#" Inherits="Myrtille.Web.OnScreenKeyboard" Codebehind="OnScreenKeyboard.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="Myrtille.Web" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="<%=BundleTable.Bundles.ResolveBundleUrl("~/node_modules/simple-keyboard/build/css/index.css", true)%>"/>
        <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/node_modules/simple-keyboard/build/index.js", true)%>"></script>
        <script language="javascript" type="text/javascript" src="<%=BundleTable.Bundles.ResolveBundleUrl("~/node_modules/simple-keyboard-layouts/build/index.js", true)%>"></script>
        <script language="javascript" type="text/javascript" src="../js/popup-i18n.js"></script>
	</head>

    <body>
        
        <form method="get" runat="server">
            <div>
                <div class="simple-keyboard"></div>
                <div id="keyboardWarning" style="color: red; padding-top: 5px; padding-bottom: 5px;"></div>
                <select id="keyboardLayout" onchange="changeLayout(this);">
                    <option value="0" selected="selected" data-i18n="keyboardDefault">Default</option>
                    <option value="1">Arabic</option>
                    <option value="2">Assamese</option>
                    <option value="3">Burmese</option>
                    <option value="4" data-i18n="keyboardChinese">Chinese</option>
                    <option value="5">Czech</option>
                    <option value="6" data-i18n="keyboardEnglish">English</option>
                    <option value="7">Farsi</option>
                    <option value="8">French</option>
                    <option value="9">Georgian</option>
                    <option value="10">German</option>
                    <option value="11">Greek</option>
                    <option value="12">Hebrew</option>
                    <option value="13">Hindi</option>
                    <option value="14">Italian</option>
                    <option value="15">Japanese</option>
                    <option value="16">Kannada</option>
                    <option value="17">Korean</option>
                    <option value="18">Polish</option>
                    <option value="19">Russian</option>
                    <option value="20">Sindhi</option>
                    <option value="21">Spanish</option>
                    <option value="22">Swedish</option>
                    <option value="23">Thai</option>
                    <option value="24">Turkish</option>
                    <option value="25">Ukrainian</option>
                    <option value="26">Urdu</option>
                </select>
                <input type="button" id="closePopupButton" data-i18n-value="close" value="Close" onclick="parent.closePopup();"/>
            </div>
        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            let OnScreenKeyboard = window.SimpleKeyboard.default;
            let KeyboardLayouts = window.SimpleKeyboardLayouts.default;
            popupI18n.apply();

            let keyboard = new OnScreenKeyboard({
                onKeyPress: key => onKeyPress(key)
            });

            // an Hyper-V VM console session (RDP over VM bus) can be connected in standard or enhanced mode (faster display, printer/clipboard/audio redirection, etc.)
            // Windows does support enhanced mode since 2012 R2 while Linux doesn't support it (except if using xRDP with Ubuntu, see https://c-nergy.be/blog/?p=12429)
            // unicode characters are not supported when enhanced mode is disabled, so keys must be sent as RDP scancode (refers to a physical keyboard, using key location whatever the layout, and must be mapped to a localized layout)
            // below is a character to javascript keycode mapping (same keycode as triggered by the keydown and keyup events) for the en-US keyboard layout
            // a javascript keycode to RDP scancode mapping is done server side, and RDP scancodes are sent through FreeRDP

            // changing the keyboard layout is only allowed when using unicode characters (so, when enhanced mode is enabled)
            document.getElementById('keyboardLayout').disabled = parent.getMyrtille().getConfig().getVMNotEnhanced();

            function KeycodeInfo(keyCode, keyShift)
            {
                this.KeyCode = keyCode;
                this.KeyShift = keyShift;
            }

            // CAUTION! the mapping below requires the keyboard layout to be en-US (QWERTY) into the remote session
            // it applies to Hyper-V console session when enhanced mode is disabled (=no unicode support!) and translates to RDP scancodes (server side)
            // when enhanced mode is enabled, unicode characters are sent independently from the remote keyboard layout

            if (parent.getMyrtille().getConfig().getVMNotEnhanced())
            {
                document.getElementById('keyboardWarning').innerText = popupI18n.t('keyboardWarning');
            }

            let keycode = new Array();
            keycode[' '] = new KeycodeInfo(32, false);
            keycode['0'] = new KeycodeInfo(48, false);
            keycode[')'] = new KeycodeInfo(48, true);
            keycode['1'] = new KeycodeInfo(49, false);
            keycode['!'] = new KeycodeInfo(49, true);
            keycode['2'] = new KeycodeInfo(50, false);
            keycode['@'] = new KeycodeInfo(50, true);
            keycode['3'] = new KeycodeInfo(51, false);
            keycode['#'] = new KeycodeInfo(51, true);
            keycode['4'] = new KeycodeInfo(52, false);
            keycode['$'] = new KeycodeInfo(52, true);
            keycode['5'] = new KeycodeInfo(53, false);
            keycode['%'] = new KeycodeInfo(53, true);
            keycode['6'] = new KeycodeInfo(54, false);
            keycode['^'] = new KeycodeInfo(54, true);
            keycode['7'] = new KeycodeInfo(55, false);
            keycode['&'] = new KeycodeInfo(55, true);
            keycode['8'] = new KeycodeInfo(56, false);
            keycode['*'] = new KeycodeInfo(56, true);
            keycode['9'] = new KeycodeInfo(57, false);
            keycode['('] = new KeycodeInfo(57, true);
            keycode['a'] = new KeycodeInfo(65, false);
            keycode['A'] = new KeycodeInfo(65, true);
            keycode['b'] = new KeycodeInfo(66, false);
            keycode['B'] = new KeycodeInfo(66, true);
            keycode['c'] = new KeycodeInfo(67, false);
            keycode['C'] = new KeycodeInfo(67, true);
            keycode['d'] = new KeycodeInfo(68, false);
            keycode['D'] = new KeycodeInfo(68, true);
            keycode['e'] = new KeycodeInfo(69, false);
            keycode['E'] = new KeycodeInfo(69, true);
            keycode['f'] = new KeycodeInfo(70, false);
            keycode['F'] = new KeycodeInfo(70, true);
            keycode['g'] = new KeycodeInfo(71, false);
            keycode['G'] = new KeycodeInfo(71, true);
            keycode['h'] = new KeycodeInfo(72, false);
            keycode['H'] = new KeycodeInfo(72, true);
            keycode['i'] = new KeycodeInfo(73, false);
            keycode['I'] = new KeycodeInfo(73, true);
            keycode['j'] = new KeycodeInfo(74, false);
            keycode['J'] = new KeycodeInfo(74, true);
            keycode['k'] = new KeycodeInfo(75, false);
            keycode['K'] = new KeycodeInfo(75, true);
            keycode['l'] = new KeycodeInfo(76, false);
            keycode['L'] = new KeycodeInfo(76, true);
            keycode['m'] = new KeycodeInfo(77, false);
            keycode['M'] = new KeycodeInfo(77, true);
            keycode['n'] = new KeycodeInfo(78, false);
            keycode['N'] = new KeycodeInfo(78, true);
            keycode['o'] = new KeycodeInfo(79, false);
            keycode['O'] = new KeycodeInfo(79, true);
            keycode['p'] = new KeycodeInfo(80, false);
            keycode['P'] = new KeycodeInfo(80, true);
            keycode['q'] = new KeycodeInfo(81, false);
            keycode['Q'] = new KeycodeInfo(81, true);
            keycode['r'] = new KeycodeInfo(82, false);
            keycode['R'] = new KeycodeInfo(82, true);
            keycode['s'] = new KeycodeInfo(83, false);
            keycode['S'] = new KeycodeInfo(83, true);
            keycode['t'] = new KeycodeInfo(84, false);
            keycode['T'] = new KeycodeInfo(84, true);
            keycode['u'] = new KeycodeInfo(85, false);
            keycode['U'] = new KeycodeInfo(85, true);
            keycode['v'] = new KeycodeInfo(86, false);
            keycode['V'] = new KeycodeInfo(86, true);
            keycode['w'] = new KeycodeInfo(87, false);
            keycode['W'] = new KeycodeInfo(87, true);
            keycode['x'] = new KeycodeInfo(88, false);
            keycode['X'] = new KeycodeInfo(88, true);
            keycode['y'] = new KeycodeInfo(89, false);
            keycode['Y'] = new KeycodeInfo(89, true);
            keycode['z'] = new KeycodeInfo(90, false);
            keycode['Z'] = new KeycodeInfo(90, true);
            keycode[';'] = new KeycodeInfo(186, false);
            keycode[':'] = new KeycodeInfo(186, true);
            keycode['='] = new KeycodeInfo(187, false);
            keycode['+'] = new KeycodeInfo(187, true);
            keycode[','] = new KeycodeInfo(188, false);
            keycode['<'] = new KeycodeInfo(188, true);
            keycode['-'] = new KeycodeInfo(189, false);
            keycode['_'] = new KeycodeInfo(189, true);
            keycode['.'] = new KeycodeInfo(190, false);
            keycode['>'] = new KeycodeInfo(190, true);
            keycode['/'] = new KeycodeInfo(191, false);
            keycode['?'] = new KeycodeInfo(191, true);
            keycode['`'] = new KeycodeInfo(192, false);
            keycode['~'] = new KeycodeInfo(192, true);
            keycode['['] = new KeycodeInfo(219, false);
            keycode['{'] = new KeycodeInfo(219, true);
            keycode['\\'] = new KeycodeInfo(220, false);
            keycode['|'] = new KeycodeInfo(220, true);
            keycode[']'] = new KeycodeInfo(221, false);
            keycode['}'] = new KeycodeInfo(221, true);
            keycode['\''] = new KeycodeInfo(222, false);
            keycode['"'] = new KeycodeInfo(222, true);

            function onKeyPress(key)
            {
                //console.log('key pressed', key);

                try
                {
                    if (key === '{space}')
                    {
                        key = ' ';
                    }

                    if (key === '{shift}' || key === '{lock}')
                    {
                        handleShift();
                    }
                    else if (key === '.com')
                    {
                        if (parent.getMyrtille().getConfig().getVMNotEnhanced())
                        {
                            parent.sendKey(keycode['.'].KeyCode, true);
                            parent.sendKey(keycode['c'].KeyCode, true);
                            parent.sendKey(keycode['o'].KeyCode, true);
                            parent.sendKey(keycode['m'].KeyCode, true);
                        }
                        else
                        {
                            parent.sendText(key);
                        }
                    }
                    else if (key === '{bksp}')
                    {
                        parent.sendKey(8, true);
                    }
                    else if (key === '{tab}')
                    {
                        parent.sendKey(9, true);
                    }
                    else if (key === '{enter}')
                    {
                        parent.sendKey(13, true);
                    }
                    else
                    {
                        if (parent.getMyrtille().getConfig().getVMNotEnhanced())
                        {
                            let keycodeInfo = keycode[key];
                            if (!keycodeInfo.KeyShift)
                            {
                                parent.sendKey(keycodeInfo.KeyCode, true);
                            }
                            else
                            {
                                // shift + key
                                parent.sendKey(16, false);
                                parent.sendKey(keycodeInfo.KeyCode, true);
                                parent.sendKey(16, true);
                            }
                        }
                        else
                        {
                            parent.sendChar(key, true);
                        }
                    }
                }
                catch (exc)
                {
                    console.error('onKeyPress error', exc.message);
                }
             }

            function handleShift()
            {
                let currentLayout = keyboard.options.layoutName;
                let shiftToggle = currentLayout === 'default' ? 'shift' : 'default';

                keyboard.setOptions({
                    layoutName: shiftToggle
                });
            }

            function changeLayout(layoutList)
            {
                let layoutName;

                switch (layoutList.selectedIndex)
                {
                    case 0:
                        layoutName = 'default';
                        break;

                    case 1:
                        layoutName = 'arabic';
                        break;

                    case 2:
                        layoutName = 'assamese';
                        break;

                    case 3:
                        layoutName = 'burmese';
                        break;

                    case 4:
                        layoutName = 'chinese';
                        break;

                    case 5:
                        layoutName = 'czech';
                        break;

                    case 6:
                        layoutName = 'english';
                        break;

                    case 7:
                        layoutName = 'farsi';
                        break;

                    case 8:
                        layoutName = 'french';
                        break;

                    case 9:
                        layoutName = 'georgian';
                        break;

                    case 10:
                        layoutName = 'german';
                        break;

                    case 11:
                        layoutName = 'greek';
                        break;

                    case 12:
                        layoutName = 'hebrew';
                        break;

                    case 13:
                        layoutName = 'hindi';
                        break;

                    case 14:
                        layoutName = 'italian';
                        break;

                    case 15:
                        layoutName = 'japanese';
                        break;

                    case 16:
                        layoutName = 'kannada';
                        break;

                    case 17:
                        layoutName = 'korean';
                        break;

                    case 18:
                        layoutName = 'polish';
                        break;

                    case 19:
                        layoutName = 'russian';
                        break;

                    case 20:
                        layoutName = 'sindhi';
                        break;

                    case 21:
                        layoutName = 'spanish';
                        break;

                    case 22:
                        layoutName = 'swedish';
                        break;

                    case 23:
                        layoutName = 'thai';
                        break;

                    case 24:
                        layoutName = 'turkish';
                        break;

                    case 25:
                        layoutName = 'ukrainian';
                        break;

                    case 26:
                        layoutName = 'urdu';
                        break;

                    default:
                        layoutName = 'default';
                }

                //console.log('change layout', layoutName);

                try
                {
                    let layout = new KeyboardLayouts().get(layoutName);

                    keyboard.setOptions({
                        layout: layout
                    });
                }
                catch (exc)
                {
                    console.error('changeLayout error', exc.message);
                }
            }

		</script>

	</body>

</html>
