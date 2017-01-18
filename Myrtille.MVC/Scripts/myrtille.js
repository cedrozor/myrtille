/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2016 Cedric Coste

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

function Myrtille(httpServerUrl, httpSessionId, webSocketPort, webSocketPortSecured, statEnabled, debugEnabled, compatibilityMode)
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
            config = new Config(httpServerUrl, httpSessionId, webSocketPort, webSocketPortSecured, statEnabled, debugEnabled, compatibilityMode);
            
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

var fullscreenPending = false;

function startMyrtille(httpSessionId, remoteSessionActive, webSocketPort, webSocketPortSecured, statEnabled, debugEnabled, compatibilityMode)
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
    
        var httpServerUrl = window.location.protocol + '//' + window.location.hostname + '/' + pathname + '/';
        //alert('http server url: ' + httpServerUrl);

        myrtille = new Myrtille(httpServerUrl, httpSessionId, webSocketPort, webSocketPortSecured, statEnabled, debugEnabled, compatibilityMode);
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

function pushImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen)
{
    try
    {
        if (config.additionalLatency > 0)
        {
            window.setTimeout(function() { processImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen); }, Math.round(config.additionalLatency / 2));
        }
        else
        {
            processImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen);
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille pushImage error: ' + exc.Message);
    }
}

function processImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen)
{
    try
    {
        // update bandwidth usage
        if (base64Data != '')
        {
            network.setBandwidthUsageB64(network.getBandwidthUsageB64() + base64Data.length);
        }

        // if a fullscreen request is pending, release it
        if (fullscreen && fullscreenPending)
        {
            //dialog.showDebug('received a fullscreen update, divs will be cleaned');
            fullscreenPending = false;
        }

        // add image to display
        display.addImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen);

        // if using divs and count reached a reasonable number, request a fullscreen update
        if (!config.getCanvasEnabled() && display.getImgCount() >= config.getImageCountOk() && !fullscreenPending)
        {
            //dialog.showDebug('reached a reasonable number of divs, requesting a fullscreen update');
            fullscreenPending = true;
            network.send(null);
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille processImage error: ' + exc.Message);
    }
}

function sendText(text)
{
    try
    {
        //alert('text to send: ' + text);

        if (text == null || text == '')
            return;

        if (config.getAdaptiveFullscreenTimeoutDelay() > 0)
            user.triggerActivity();

        var keys = new Array();
        for (var i = 0; i < text.length; i++)
        {
            var charCode = text.charCodeAt(i);
            //alert('sending charCode: ' + charCode);
                    
            if (charCode == 10)     // LF
                charCode = 13;      // CR

            keys.push((charCode == 13 ? 'K' : 'U') + charCode + '-1');
            keys.push((charCode == 13 ? 'K' : 'U') + charCode + '-0');
        }

        if (keys.length > 0)
            network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        alert('myrtille sendText error: ' + exc.Message);
    }
}

function sendKey(keyCode, release)
{
    try
    {
        //alert('key to send: ' + keyCode);
    
        if (keyCode == null || keyCode == '')
            return;

        if (config.getAdaptiveFullscreenTimeoutDelay() > 0)
            user.triggerActivity();

        var keys = new Array();
                
        keys.push('K' + keyCode + '-1');

        if (release)
            keys.push('K' + keyCode + '-0');
                
        network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        alert('myrtille sendKey error: ' + exc.Message);
    }
}