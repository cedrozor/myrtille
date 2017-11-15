/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste

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

function Myrtille(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight)
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
            config = new Config(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight);
            
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

function startMyrtille(remoteSessionActive, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight)
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

        // prevent session fixation attack by generating a new session ID upon login
        // also, using http get method to prevent the browser asking for http post data confirmation if the page is reloaded
        // https://www.owasp.org/index.php/Session_Fixation
        //var redirectUrl = '';

        //if (window.location.href.indexOf('?') == -1)
        //{
        //    //alert('no querystring detected');
        //    // retrieve the http session id from url; in cookieless mode (default), it has the format "(S(sessionId))"
        //    // reload the page without it, so that a new session is generated while the current one is abandonned
        //    var startIdx = httpServerUrl.indexOf('(S(');
        //    if (startIdx != -1)
        //    {
        //        var endIdx = httpServerUrl.indexOf('))');
        //        var httpSessionId = httpServerUrl.substr(startIdx + 3, endIdx - startIdx - 3);
        //        //alert('http session id: ' + httpSessionId);
        //        redirectUrl = httpServerUrl.replace('/(S(' + httpSessionId + '))', '') + '?oldSID=' + httpSessionId;
        //    }
        //    // http session id missing from url
        //    // the http session may use a cookie (CAUTION! in this case, multi tabs/sessions support is disabled)
        //    // simple redirect with an empty querystring
        //    else
        //    {
        //        //alert('no http session id into url');
        //        redirectUrl = httpServerUrl + '?';
        //    }
        //}
        //else
        //{
        //    //alert('querystring detected');
        //    // the http session used on login was abandonned; remove it from url
        //    if (window.location.href.indexOf('?oldSID=') != -1)
        //    {
        //        //alert('clearing old session id from url');
        //        redirectUrl = window.location.href.substr(0, window.location.href.indexOf('?oldSID=')) + '?';
        //    }
        //    // auto-connect / start program from url
        //    // there is no need for any session fixation protection in this case because the user credentials are already into the url (with password either plain text or hashed)
        //    else if (window.location.href.indexOf('&connect=') != -1)
        //    {
        //        // redirect with an empty querystring
        //        redirectUrl = httpServerUrl + '?';
        //    }
        //}

        //if (redirectUrl != '')
        //{
        //    //alert('reloading page with url: ' + redirectUrl);
        //    window.location.href = redirectUrl;
        //    return;
        //}

        myrtille = new Myrtille(httpServerUrl, statEnabled, debugEnabled, compatibilityMode, scaleDisplay, displayWidth, displayHeight);
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

        if (config.getAdaptiveFullscreenTimeout() > 0)
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

        if (config.getAdaptiveFullscreenTimeout() > 0)
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

function sendRightClick()
{
    try
    {
        // check touchscreen first (same logic as mouse)
        var lastMouseClickX = user.getTouchscreen().getLastTouchTapX();
        var lastMouseClickY = user.getTouchscreen().getLastTouchTapY();

        // if there is no touchscreen tap activity, check mouse click
        // note that, if mouse and touchscreen are both used, the touchscreen activity is prioritary
        if (lastMouseClickX == null || lastMouseClickY == null)
        {
            lastMouseClickX = user.getMouse().getLastMouseClickX();
            lastMouseClickY = user.getMouse().getLastMouseClickY();
        }

        if (lastMouseClickX != null && lastMouseClickY != null)
        {
            if (config.getAdaptiveFullscreenTimeout() > 0)
                user.triggerActivity();

            network.processUserEvent('mouse', network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + '1' + lastMouseClickX + '-' + lastMouseClickY);
            network.processUserEvent('mouse', network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + '0' + lastMouseClickX + '-' + lastMouseClickY);
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendRightClick error: ' + exc.message);
    }
}