/*
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
*/

/*****************************************************************************************************************************************************************************************************/
/*** Main                                                                                                                                                                                          ***/
/*****************************************************************************************************************************************************************************************************/

function Myrtille(httpServerUrl, connectionState, statEnabled, debugEnabled, compatibilityMode, browserResize, displayWidth, displayHeight, hostType, vmNotEnhanced)
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

    /*
    prefixes (3 chars) are used to serialize commands with strings instead of numbers
    they make it easier to read log traces to find out which commands are issued
    they must match the prefixes used server side
    commands can also be reordered without any issue
    */
    var commandEnum =
        {
            // connection
            SEND_SERVER_ADDRESS: { value: 0, text: 'SRV' },
            SEND_VM_GUID: { value: 1, text: 'VMG' },
            SEND_USER_DOMAIN: { value: 2, text: 'DOM' },
            SEND_USER_NAME: { value: 3, text: 'USR' },
            SEND_USER_PASSWORD: { value: 4, text: 'PWD' },
            SEND_START_PROGRAM: { value: 5, text: 'PRG' },
            CONNECT_CLIENT: { value: 6, text: 'CON' },

            // browser
            SEND_BROWSER_RESIZE: { value: 7, text: 'RSZ' },
            SEND_BROWSER_PULSE: { value: 8, text: 'PLS' },

            // keyboard
            SEND_KEY_UNICODE: { value: 9, text: 'KUC' },
            SEND_KEY_SCANCODE: { value: 10, text: 'KSC' },

            // mouse
            SEND_MOUSE_MOVE: { value: 11, text: 'MMO' },
            SEND_MOUSE_LEFT_BUTTON: { value: 12, text: 'MLB' },
            SEND_MOUSE_MIDDLE_BUTTON: { value: 13, text: 'MMB' },
            SEND_MOUSE_RIGHT_BUTTON: { value: 14, text: 'MRB' },
            SEND_MOUSE_WHEEL_UP: { value: 15, text: 'MWU' },
            SEND_MOUSE_WHEEL_DOWN: { value: 16, text: 'MWD' },

            // control
            SET_SCALE_DISPLAY: { value: 17, text: 'SCA' },
            SET_RECONNECT_SESSION: { value: 18, text: 'RCN' },
            SET_IMAGE_ENCODING: { value: 19, text: 'ECD' },
            SET_IMAGE_QUALITY: { value: 20, text: 'QLT' },
            SET_IMAGE_QUANTITY: { value: 21, text: 'QNT' },
            SET_AUDIO_FORMAT: { value: 22, text: 'AUD' },
            SET_AUDIO_BITRATE: { value: 23, text: 'BIT' },
            SET_SCREENSHOT_CONFIG: { value: 24, text: 'SSC' },
            START_TAKING_SCREENSHOTS: { value: 25, text: 'SS1' },
            STOP_TAKING_SCREENSHOTS: { value: 26, text: 'SS0' },
            TAKE_SCREENSHOT: { value: 27, text: 'SCN' },
            REQUEST_FULLSCREEN_UPDATE: { value: 28, text: 'FSU' },
            SEND_LOCAL_CLIPBOARD: { value: 29, text: 'CLP' },
            CLOSE_CLIENT: { value: 30, text: 'CLO' }
        };

    if (Object.freeze)
    {
        Object.freeze(commandEnum);
    }

    this.getCommandEnum = function() { return commandEnum; };

    this.init = function()
    {
        try
        {
            config = new Config(httpServerUrl, connectionState, statEnabled, debugEnabled, compatibilityMode, browserResize, displayWidth, displayHeight, hostType, vmNotEnhanced);
            
            dialog = new Dialog(config);
            
            display = new Display(this, config, dialog);
            display.init();

            network = new Network(this, config, dialog, display);
            network.init();

            user = new User(this, config, dialog, display, network);
            user.init();
        }
        catch (exc)
        {
            alert('myrtille init error: ' + exc.message);
            throw exc;
        }
    };

    this.initConnection = function()
    {
        try
        {
            // if connecting, send any command for the gateway to connect the remote server
            if (config.getConnectionState() == 'CONNECTING')
            {
                network.send(this.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text + 'initial');
            }
            // if connected, send the client settings and request a fullscreen update
            else if (config.getConnectionState() == 'CONNECTED')
            {
                this.initClient();
            }
        }
        catch (exc)
        {
            dialog.showDebug('myrtille initConnection error: ' + exc.message);
        }
    };

    this.initClient = function()
    {
        if (config.getHostType() != config.getHostTypeEnum().RDP)
        {
            sendSettings();
        }
        else
        {
            // retrieve the clipboard (async) first
            this.readClipboard(true);
        }
    };

    this.readClipboard = function(init)
    {
        //dialog.showDebug('reading clipboard text');

        try
        {
            if (navigator.clipboard)
            {
                navigator.clipboard.readText()
                    .then(function(text)
                    {
                        // local clipboard
                        // max length 1MB (truncated above this size)
                        if (text.length > 1048576)
                        {
                            text = text.substr(0, 1048576) + '--- TRUNCATED ---';
                        }

                        dialog.showDebug('text read from clipboard: ' + (text.length <= 100 ? text : text.substr(0, 100) + '...') + ' (length: ' + (text.length <= 1048576 ? text.length : '1048576, truncated') + ')');
                        if (init)
                        {
                            sendSettings(text);
                        }
                        else
                        {
                            try
                            {
                                // send the clipboard text as unicode code points
                                network.send(commandEnum.SEND_LOCAL_CLIPBOARD.text + strToUnicode(text));
                            }
                            catch (exc)
                            {
                                dialog.showDebug('failed to send clipboard: ' + exc.message);
                            }
                        }
                    })
                    .catch(function(err)
                    {
                        dialog.showDebug('failed to read text from clipboard (' + err + ')');
                        if (init)
                        {
                            sendSettings();
                        }
                    });
            }
            else
            {
                dialog.showDebug('async clipboard API is not supported or clipboard read access is denied (do you use HTTPS?)');
                if (init)
                {
                    sendSettings();
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('myrtille readClipboard error: ' + exc.message);
            if (init)
            {
                sendSettings();
            }
        }
    };

    function sendSettings(clipboardText)
    {
        try
        {
            var commands = new Array();

            if (config.getHostType() == config.getHostTypeEnum().RDP)
            {
                //dialog.showDebug('sending display config');
                commands.push(commandEnum.SET_IMAGE_ENCODING.text + config.getImageEncoding().value);
                commands.push(commandEnum.SET_IMAGE_QUALITY.text + config.getImageQuality());
                commands.push(commandEnum.SET_IMAGE_QUANTITY.text + config.getImageQuantity());

                //dialog.showDebug('sending audio config');
                commands.push(commandEnum.SET_AUDIO_FORMAT.text + config.getAudioFormat().value);
                if (config.getAudioFormat() != config.getAudioFormatEnum().NONE)
                {
                    commands.push(commandEnum.SET_AUDIO_BITRATE.text + config.getAudioBitrate());
                }

                // set action on browser resize
                if (config.getBrowserResize() == null)
                {
                    switch (config.getDefaultResize())
                    {
                        case config.getBrowserResizeEnum().SCALE:
                            var width = display.getBrowserWidth();
                            var height = display.getBrowserHeight();
                            commands.push(commandEnum.SET_SCALE_DISPLAY.text + (config.getKeepAspectRatio() ? '1' : '0') + '|' + width + 'x' + height);
                            break;

                        case config.getBrowserResizeEnum().RECONNECT:
                            commands.push(commandEnum.SET_RECONNECT_SESSION.text + 1 + '|' + 0);
                            break;

                        default:
                            commands.push(commandEnum.SET_RECONNECT_SESSION.text + 0 + '|' + 0);
                    }

                    config.setBrowserResize(config.getDefaultResize().text);
                }

                // initial clipboard synchronization
                if (clipboardText != null && clipboardText != '')
                {
                    // send the clipboard text as unicode code points
                    commands.push(commandEnum.SEND_LOCAL_CLIPBOARD.text + strToUnicode(clipboardText));
                }
            }

            //dialog.showDebug('initial fullscreen update');
            commands.push(commandEnum.REQUEST_FULLSCREEN_UPDATE.text + 'initial');

            network.send(commands.toString());
        }
        catch (exc)
        {
            dialog.showDebug('myrtille sendSettings error: ' + exc.message);
            throw exc;
        }
    }
}

/*****************************************************************************************************************************************************************************************************/
/*** External Calls                                                                                                                                                                                ***/
/*****************************************************************************************************************************************************************************************************/

var myrtille = null;
this.getMyrtille = function() { return myrtille; };
var config = null;
var dialog = null;
var display = null;
var network = null;
var user = null;

var fullscreenPending = false;

var messageTypeEnum =
{
    Connected: 0,
    Disconnected: 1,
    PageReload: 2,
    RemoteClipboard: 3,
    TerminalOutput: 4,
    PrintJob: 5,
    Ack: 6
};

function startMyrtille(connectionState, statEnabled, debugEnabled, compatibilityMode, browserResize, displayWidth, displayHeight, hostType, vmNotEnhanced)
{
    try
    {
        // if no remote session is running, leave
        if (connectionState == null || (connectionState != 'CONNECTING' && connectionState != 'CONNECTED'))
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

        myrtille = new Myrtille(httpServerUrl, connectionState, statEnabled, debugEnabled, compatibilityMode, browserResize, displayWidth, displayHeight, hostType, vmNotEnhanced);
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

function lpInitConnection()
{
    myrtille.initConnection();
}

function lpProcessMessage(text)
{
    if (config.getAdditionalLatency() > 0)
    {
        window.setTimeout(function() { processMessage(text); }, Math.round(config.getAdditionalLatency() / 2));
    }
    else
    {
        processMessage(text);
    }
}

function lpProcessImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData)
{
    if (config.getAdditionalLatency() > 0)
    {
        window.setTimeout(function() { processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData); }, Math.round(config.getAdditionalLatency() / 2));
    }
    else
    {
        processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);
    }
}

function processMessage(text)
{
    try
    {
        var message = JSON.parse(text);

        //dialog.showDebug('processing message, type: ' + message.Type + ', text: ' + message.Text);

        switch (message.Type)
        {
            // reload page
            case messageTypeEnum.PageReload:
                window.location.href = window.location.href;
                break;

            // receive terminal data, send to xtermjs
            case messageTypeEnum.TerminalOutput:
                /* IE hack!

                for some reason, IE (all versions) is very slow to render the terminal, whatever the connection speed
                while I was debugging, I found the rendering was way faster after displaying the received data into the debug div (?!)
                
                I don't really understand why... perhaps it's due to the fact the data is already into the DOM when the terminal handles it...
                so I made up an hidden "cache div" and put the data on it before writing to the terminal
                I didn't found any other solution but it's pretty harmless anyway as the cache div is hidden

                other browsers don't seem to have the same issue, neither benefit from that hack, so IE only for now...
                also interesting to note, this issue occurs only when using websockets (long-polling and xhr only: ok)

                */

                if (display.isIEBrowser())
                {
                    var cacheDiv = document.getElementById('cacheDiv');
                    if (cacheDiv != null)
                    {
                        cacheDiv.innerHTML = message.Text;
                    }
                }

                display.getTerminalDiv().writeTerminal(message.Text);
                break;

            // remote clipboard
            case messageTypeEnum.RemoteClipboard:
                writeClipboard(message.Text);
                break;

            // print job
            case messageTypeEnum.PrintJob:
                downloadPdf(message.Text);
                break;

            // connected session
            case messageTypeEnum.Connected:
                // if running myrtille into an iframe, register the iframe url (into a cookie)
                // this is necessary to prevent a new http session from being generated when reloading the page, due to the missing http session id into the iframe url (!)
                // multiple iframes (on the same page), like multiple connections/tabs, requires cookieless="UseUri" for sessionState into web.config
                if (parent != null && window.name != '')
                {
                    parent.setCookie(window.name, window.location.href);
                }

                // send settings and request a fullscreen update
                myrtille.initClient();
                break;

            // disconnected session
            case messageTypeEnum.Disconnected:
                // if running myrtille into an iframe, unregister the iframe url
                if (parent != null && window.name != '')
                {
                    parent.eraseCookie(window.name);
                }

                // back to default page
                window.location.href = config.getHttpServerUrl();
                break;

            // server ack
            case messageTypeEnum.Ack:
                //dialog.showDebug('server ack: ' + message.Text);

                // update the average "latency"
                network.updateLatency(parseInt(message.Text));
                break;
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille processMessage error: ' + exc.message);
    }
}

function processImage(idx, posX, posY, width, height, format, quality, fullscreen, data)
{
    try
    {
        //dialog.showDebug('processing image, idx: ' + idx + ', posX: ' + posX + ', posY: ' + posY + ', width: ' + width + ', height: ' + height + ', format: ' + format + ', quality: ' + quality + ', fullscreen: ' + fullscreen + ', data: ' + data);

        // update bandwidth usage
        network.setBandwidthUsage(network.getBandwidthUsage() + data.length);

        // if a fullscreen request is pending, release it
        if (fullscreen && fullscreenPending)
        {
            //dialog.showDebug('received a fullscreen update, divs will be cleaned');
            fullscreenPending = false;
        }

        // add image to display
        display.addImage(idx, posX, posY, width, height, format, quality, fullscreen, data);

        // if using divs and count reached a reasonable number, request a fullscreen update
        if (config.getDisplayMode() != config.getDisplayModeEnum().CANVAS && display.getImgCount() >= config.getImageCountOk() && !fullscreenPending)
        {
            //dialog.showDebug('reached a reasonable number of divs, requesting a fullscreen update');
            fullscreenPending = true;
            network.send(myrtille.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text + 'cleanup');
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
        setCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'stat', config.getStatEnabled() ? 0 : 1);
        window.location.href = window.location.href;
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
        setCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'debug', config.getDebugEnabled() ? 0 : 1);
        window.location.href = window.location.href;
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
        setCookie((parent != null && window.name != '' ? window.name + '_' : '') + 'browser', config.getCompatibilityMode() ? 0 : 1);
        window.location.href = window.location.href;
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

        var width = display.getBrowserWidth();
        var height = display.getBrowserHeight();

        // send resolution while enabling display scaling
        network.send(myrtille.getCommandEnum().SET_SCALE_DISPLAY.text + (config.getBrowserResize() == config.getBrowserResizeEnum().SCALE ? 0 : ((config.getKeepAspectRatio() ? '1' : '0') + '|' + width + 'x' + height)));
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleScaleDisplay error: ' + exc.message);
    }
}

function toggleReconnectSession()
{
    try
    {
        disableToolbar();
        network.send(myrtille.getCommandEnum().SET_RECONNECT_SESSION.text + (config.getBrowserResize() == config.getBrowserResizeEnum().RECONNECT ? 0 : 1) + '|' + 1);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleReconnectSession error: ' + exc.message);
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

function toggleVerticalSwipe(button)
{
    try
    {
        // client side only; no need to persist the button state over the session (page reload = reset the toggle button)
        user.toggleVerticalSwipe(button);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille toggleVerticalSwipe error: ' + exc.message);
    }
}

function changeImageQuality(quality)
{
    try
    {
        // display settings are applied by the network.js "tweakDisplay" function
        network.setOriginalImageEncoding(config.getImageEncodingEnum().JPEG);
        network.setOriginalImageQuality(quality <= 90 ? quality : 90);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille changeImageQuality error: ' + exc.message);
    }
}

var clipboardText = null;
this.getClipboardText = function() { return clipboardText; };

this.writeClipboard = function(text)
{
    //dialog.showDebug('received clipboard text: ' + text);
    clipboardText = text;

    try
    {
        if (navigator.clipboard)
        {
            navigator.clipboard.writeText(text)
                .then(function()
                {
                    dialog.showDebug('text copied to clipboard: ' + (text.length <= 100 ? text : text.substr(0, 100) + '...') + ' (length: ' + (text.length <= 1048576 ? text.length : '1048576, truncated') + ')');
                })
                .catch(function(err)
                {
                    dialog.showDebug('failed to write text to clipboard (' + err + ')');
                    openPopup('copyClipboardPopup', 'CopyClipboard.aspx');
                });
        }
        else
        {
            dialog.showDebug('async clipboard API is not supported or clipboard write access is denied (do you use HTTPS?)');
            openPopup('copyClipboardPopup', 'CopyClipboard.aspx');
        }
    }
    catch (exc)
    {
        dialog.showDebug('myrtille writeClipboard error: ' + exc.message);
        openPopup('copyClipboardPopup', 'CopyClipboard.aspx');
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
            if (config.getHostType() == config.getHostTypeEnum().RDP)
            {
                var charCode = text.charCodeAt(i);
                //dialog.showDebug('sending charCode: ' + charCode);
                    
                if (charCode == 10)     // LF
                    charCode = 13;      // CR

                keys.push((charCode == 13 ? myrtille.getCommandEnum().SEND_KEY_SCANCODE.text : myrtille.getCommandEnum().SEND_KEY_UNICODE.text) + charCode + '-1');
                keys.push((charCode == 13 ? myrtille.getCommandEnum().SEND_KEY_SCANCODE.text : myrtille.getCommandEnum().SEND_KEY_UNICODE.text) + charCode + '-0');
            }
            else
            {
                var char = text.charAt(i);
                //dialog.showDebug('sending char: ' + char);
                keys.push(myrtille.getCommandEnum().SEND_KEY_UNICODE.text + char);
            }
        }

        if (keys.length > 0)
            network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendText error: ' + exc.message);
    }
}

this.sendKey = function(keyCode, release)
{
    try
    {
        //dialog.showDebug('key to send: ' + keyCode);
    
        if (keyCode == null || keyCode == '')
            return;

        user.triggerActivity();

        var keys = new Array();

        keys.push(myrtille.getCommandEnum().SEND_KEY_SCANCODE.text + keyCode + '-1');

        if (release)
            keys.push(myrtille.getCommandEnum().SEND_KEY_SCANCODE.text + keyCode + '-0');
                
        network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendKey error: ' + exc.message);
    }
}

this.sendChar = function(char, release)
{
    try
    {
        //dialog.showDebug('char to send: ' + char);
    
        if (char == null || char == '')
            return;

        user.triggerActivity();

        var charCode = char.charCodeAt(0);

        var keys = new Array();

        keys.push(myrtille.getCommandEnum().SEND_KEY_UNICODE.text + charCode + '-1');

        if (release)
            keys.push(myrtille.getCommandEnum().SEND_KEY_UNICODE.text + charCode + '-0');
                
        network.processUserEvent('keyboard', keys.toString());
    }
    catch (exc)
    {
        dialog.showDebug('myrtille sendChar error: ' + exc.message);
    }
}

this.sendCtrlAltDel = function()
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

this.setKeyCombination = function()
{
    try
    {
        if (user == null)
            return;

        user.getKeyboard().setKeyCombination();
    }
    catch (exc)
    {
        alert('myrtille setKeyCombination error: ' + exc.message);
    }
}

function handleRemoteSessionExit(exitCode)
{
    // success
    if (exitCode == 0)
        return;

    switch (exitCode)
    {
        // host client process killed
        case -1:
        // host client process killed from task manager
        case 1:
            alert('The remote connection was disconnected after the host client process was killed');
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

        // invalid server address
        case 131077:
        // invalid security protocol
        case 131084:
            alert('The remote connection failed due to invalid server address or security protocol');
            break;

        // invalid username (Hyper-V host)
        case 131081:
        // invalid password (Hyper-V host)
        case 131082:
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

var pdf = null;
var pdfName = null;
var pdfLoad = false;

this.downloadPdf = function(name)
{
    try
    {
        //alert('creating iframe to download pdf: ' + name);

        pdfName = name;
        pdfLoad = false;

        pdf = document.createElement('iframe');

        pdf.onload = this.printPdf;

        pdf.style.width = '0px';
        pdf.style.height = '0px';
        pdf.frameBorder = 0;

        document.body.appendChild(pdf);
    }
	catch (exc)
	{
        alert('myrtille downloadPdf error: ' + exc.message);
	}
}

this.printPdf = function()
{
    try
    {
        if (!pdfLoad)
        {
            pdfLoad = true;
            // issue with firefox when using inline content disposition (erroneous cross-origin security message when using pdf.js internal resource?!); using attachment instead
            pdf.src = config.getHttpServerUrl() + 'PrintDocument.aspx?name=' + pdfName + '&disposition=' + (display.isFirefoxBrowser() ? 'attachment' : 'inline');
        }
        else
        {
            pdf.focus();
            pdf.contentWindow.print();
        }
    }
	catch (exc)
	{
        alert('myrtille printPdf error: ' + exc.message);
	}
}

this.writeTerminal = function(data)
{
    try
    {
        //alert('writing terminal data: ' + data);
        display.getTerminalDiv().writeTerminal(data);
    }
    catch (exc)
    {
        alert('myrtille writeTerminal error: ' + exc.message);
    }
}

this.doDisconnect = function()
{
    try
    {
        disableToolbar();
        network.send(myrtille.getCommandEnum().CLOSE_CLIENT.text);
    }
    catch (exc)
    {
        dialog.showDebug('myrtille doDisconnect error: ' + exc.message);
    }
}