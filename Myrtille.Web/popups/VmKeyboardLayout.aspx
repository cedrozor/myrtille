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

<%@ Page Language="C#" Inherits="Myrtille.Web.VmKeyboardLayout" Codebehind="VmKeyboardLayout.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="Myrtille.Web" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
	</head>

    <body>
        
        <form method="get" runat="server">
            <div>
                <select id="keyboardLayout" onchange="changeLayout(this);">
                    <option value="0" selected="selected">English (en-us)</option>
                    <option value="1">German (de-de)</option>
                </select>
                <br />
                <br />
                <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
            </div>
        </form>

		<script type="text/javascript" language="javascript" defer="defer">
            try {
                var dropdown = document.getElementById('keyboardLayout');
                // init layout dropdown
                var layoutName = parent.getMyrtille().getConfig().getVmKeyboardLayout();
                if (layoutName == 'en-us')
                    dropdown.selectedIndex = 0;
                else if (layoutName == 'de-de')
                    dropdown.selectedIndex = 1
            }
            catch (exc) {
                console.error('layout dropdown init error', exc.message);
            }

            function changeLayout(layoutList)
            {
                let layoutName;

                switch (layoutList.selectedIndex)
                {
                    case 0:
                        layoutName = 'en-us';
                        break;
                    case 1:
                        layoutName = 'de-de';
                        break;
                    default:
                        layoutName = 'en-us';
                }

                console.log('change VM keyboard layout', layoutName);

                try
                {
                    // update myrtille configured VM keyboard layout
                    parent.getMyrtille().getConfig().setVmKeyboardLayout(layoutName);
                }
                catch (exc)
                {
                    console.error('changeLayout error', exc.message);
                }
            }

        </script>

	</body>

</html>