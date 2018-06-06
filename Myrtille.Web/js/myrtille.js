/*
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
*/

/*****************************************************************************************************************************************************************************************************/
/*** Main                                                                                                                                                                                          ***/
/*****************************************************************************************************************************************************************************************************/

function Myrtille(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight, hostType)
{
    var config = null;
    this.getConfig = function() { return config; };

    var dialog = null;
    this.getDialog = function() { return dialog; };

    var display = null;
    this.getDisplay = function() { return display; };

    var network = null;
    this.getNetwork = function() { return network; };
   
    var user = null;
    this.getUser = function() { return user; };

    this.init = function()
    {
        try
        {
            config = new Config(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight, hostType);
            
            dialog = new Dialog(config);
            
            display = new Display(config, dialog);
            display.init();

            network = new Network(config, dialog, display);
            network.init();

            user = new User(config, dialog, display, network);
            user.init();
        }
        catch (exc)
        {
            alert('myrtille init error: ' + exc.message);
            throw exc;
        }
    };
}

/*****************************************************************************************************************************************************************************************************/
/*** External Calls                                                                                                                                                                                ***/
/*****************************************************************************************************************************************************************************************************/

var myrtille = null;
var config = null;
var dialog = null;
var display = null;
var network = null;
var user = null;

this.getConfig = function() { return config; };

var fullscreenPending = false;

function startMyrtille(remoteSessionActive, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight, hostType)
{
    try
    {
        // if no remote session is running, leave
        if (!remoteSessionActive)
            return;

        // retrieve the http server url
        var pathname = '';
        var parts = new Array();
        parts = window.location.pathname.split('/');
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
    
        var httpServerUrl = window.location.protocol + '//' + window.location.hostname + (window.location.port ? ':' + window.location.port : '') + '/' + pathname + '/';
        //alert('http server url: ' + httpServerUrl);

        myrtille = new Myrtille(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight, hostType);
        myrtille.init();

        // code shortcuts
        config = myrtille.getConfig();
        dialog = myrtille.getDialog();
        display = myrtille.getDisplay();
        network = myrtille.getNetwork();
        user = myrtille.getUser();
    }
    catch (exc)
    {
        alert('failed to start myrtille: ' + exc.message);
        myrtille = null;
    }
}

this.pushImage = function(idx, posX, posY, width, height, format, quality, fullscreen, base64Data)
{
    try
    {
        if (config.additionalLatency > 0)
        {
            window.setTimeout(function() { processImage(idx, posX, posY, width, height, format, quality, fullscreen, base64Data); }, Math.round(config.additionalLatency / 2));
        }
        else
        {
            processImage(idx, posX, posY, width, height, format, quality, fullscreen, base64Data);
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille pushImage error: ' + exc.message);
    }
}

function processImage(idx, posX, posY, width, height, format, quality, fullscreen, base64Data)
{
    try
    {
        // update bandwidth usage
        network.setBandwidthUsage(network.getBandwidthUsage() + base64Data.length);

        // if a fullscreen request is pending, release it
        if (fullscreen && fullscreenPending)
        {
            //dialog.showDebug('received a fullscreen update, divs will be cleaned');
            fullscreenPending = false;
        }

        // add image to display
        display.addImage(idx, posX, posY, width, height, format, quality, fullscreen, base64Data);

        // if using divs and count reached a reasonable number, request a fullscreen update
        if (config.getDisplayMode() != config.getDisplayModeEnum().CANVAS && display.getImgCount() >= config.getImageCountOk() && !fullscreenPending)
        {
            //dialog.showDebug('reached a reasonable number of divs, requesting a fullscreen update');
            fullscreenPending = true;
            network.send(network.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text);
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille processImage error: ' + exc.message);
    }
}

function toggleStatMode()
{
    try
    {
        disableToolbar();
        network.send(network.getCommandEnum().SET_STAT_MODE.text + (config.getStatEnabled() ? 0 : 1));
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleStatMode error: ' + exc.message);
    }
}

function toggleDebugMode()
{
    try
    {
        disableToolbar();
        network.send(network.getCommandEnum().SET_DEBUG_MODE.text + (config.getDebugEnabled() ? 0 : 1));
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleDebugMode error: ' + exc.message);
    }
}

function toggleCompatibilityMode()
{
    try
    {
        disableToolbar();
        network.send(network.getCommandEnum().SET_COMPATIBILITY_MODE.text + (config.getCompatibilityMode() ? 0 : 1));
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleCompatibilityMode error: ' + exc.message);
    }
}

function toggleScaleDisplay()
{
    try
    {
        disableToolbar();

        var width = display.getBrowserWidth() - display.getHorizontalOffset();
        var height = display.getBrowserHeight() - display.getVerticalOffset();

        // send resolution while enabling display scaling
        network.send(network.getCommandEnum().SET_SCALE_DISPLAY.text + (config.getScaleDisplay() ? 0 : (width + 'x' + height)));
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleScaleDisplay error: ' + exc.message);
    }
}

function toggleRightClick(button)
{
    try
    {
        // client side only; no need to persist the button state over the session (page reload = reset the toggle button)
        user.toggleRightClick(button);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleRightClick error: ' + exc.message);
    }
}

function requestRemoteClipboard()
{
    try
    {
        network.send(network.getCommandEnum().REQUEST_REMOTE_CLIPBOARD.text);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille requestRemoteClipboard error: ' + exc.message);
    }
}

this.sendText = function(text)
{
    try
    {
        //dialog.showDebug('text to send: ' + text);

        if (text == null || text == '')
            return;

        user.triggerActivity();

        var keys = new Array();
        for (var i = 0; i < text.length; i++)
        {
            var charCode = text.charCodeAt(i);
            //dialog.showDebug('sending charCode: ' + charCode);
                    
            if (charCode == 10)     // LF
                charCode = 13;      // CR

            keys.push((charCode == 13 ? network.getCommandEnum().SEND_KEY_SCANCODE.text : network.getCommandEnum().SEND_KEY_UNICODE.text) + charCode + '-1');
            keys.push((charCode == 13 ? network.getCommandEnum().SEND_KEY_SCANCODE.text : network.getCommandEnum().SEND_KEY_UNICODE.text) + charCode + '-0');
        }

        if (keys.length > 0)
            network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendText error: ' + exc.message);
    }
}

function sendKey(keyCode, release)
{
    try
    {
        //dialog.showDebug('key to send: ' + keyCode);
    
        if (keyCode == null || keyCode == '')
            return;

        user.triggerActivity();

        var keys = new Array();
                
        keys.push(network.getCommandEnum().SEND_KEY_SCANCODE.text + keyCode + '-1');

        if (release)
            keys.push(network.getCommandEnum().SEND_KEY_SCANCODE.text + keyCode + '-0');
                
        network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendKey error: ' + exc.message);
    }
}

function sendCtrlAltDel()
{
    try
    {
        // ctrl
        sendKey(17, false);
        window.setTimeout(function()
        {
            // alt
            sendKey(18, false);
            window.setTimeout(function()
            {
                // del
                sendKey(46, false);
                window.setTimeout(function()
                {
                    // release all keys at once
                    sendKey(17, true);
                    sendKey(18, true);
                    sendKey(46, true);
                }, 100)
            }, 100)
        }, 100);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendCtrlAltDel error: ' + exc.message);
    }
}

function handleRemoteSessionExit(exitCode)
{
    if (exitCode == 0)
        return;

    switch (exitCode)
    {
        // wfreerdp.exe process killed
        case 1:
            alert('The remote connection was disconnected after the wfreerdp.exe process was killed');
            break;

        // session disconnect from admin console
        case 65537:
            alert('The remote connection was disconnected from admin console');
            break;

        // session logout from admin console
        case 65538:
            alert('The remote connection was logged out from admin console');
            break;

        // idle timeout
        case 65539:
            alert('The remote connection was disconnected after the idle timeout has expired');
            break;

        // maximum time
        case 65540:
            alert('The remote connection was disconnected after the maximum session time was reached');
            break;

        // session disconnect from windows menu
        case 65547:
            //alert('The remote connection was disconnected from windows menu');
            break;

        // session logout from windows menu
        case 65548:
            //alert('The remote connection was logged out from windows menu');
            break;

        // invalid server
        case 131077:
        // unreachable server
        case 131084:
            alert('The remote connection failed due to invalid or unreachable server address');
            break;

        // missing username
        case 131083:
        // missing password
        case 131085:
        // invalid credentials
        case 131092:
            alert('The remote connection failed due to missing or invalid credentials');
            break;

        default:
            alert('The remote connection failed or was closed unexpectedly');
    }
}

this.downloadPdf = function(name)
{
    try
    {
        //alert('creating iframe to download pdf: ' + name);

        var pdf = document.createElement('iframe');

        pdf.style.width = '0px';
        pdf.style.height = '0px';
        pdf.frameBorder = 0;

        pdf.src = config.getHttpServerUrl() + 'PrintDocument.aspx?name=' + name;
            
        document.body.appendChild(pdf);
    }
	catch (exc)
	{
        alert('myrtille downloadPdf error: ' + exc.message);
	}
}