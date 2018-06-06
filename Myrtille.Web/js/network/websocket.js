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
/*** Websocket                                                                                                                                                                                     ***/
/*****************************************************************************************************************************************************************************************************/

function Websocket(config, dialog, display, network)
{
    var ws = null;
    var wsOpened = false;
    var wsError = false;

    var fullscreenPending = false;

    this.init = function()
    {
        try
        {
            // using the IIS 8+ websockets support, the websocket server url is the same as http (there is just a protocol scheme change and a specific handler; standard and secured ports are the same)
            var wsUrl = config.getHttpServerUrl().replace('http', 'ws') + 'SocketHandler.ashx?type=' + (config.getImageMode() != config.getImageModeEnum().BINARY ? 'text' : 'binary');

            //dialog.showDebug('websocket server url: ' + wsUrl);

            var wsImpl = window.WebSocket || window.MozWebSocket;
            ws = new wsImpl(wsUrl);
        }
        catch (exc)
        {
            dialog.showDebug('websocket init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            ws = null;
            return;
        }

        // websocket binary transfer, instead of text, removes the base64 33% bandwidth overhead (preferred)
        if (config.getImageMode() == config.getImageModeEnum().BINARY)
        {
            try
            {
                // the another possible value is 'blob'; but it involves asynchronous processing whereas we need images to be displayed sequentially
                ws.binaryType = 'arraybuffer';
            }
            catch (exc)
            {
                dialog.showDebug('websocket binary init error: ' + exc.message + ', falling back to base64 (or roundtrip if not available)');
                config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            }
        }

        //dialog.showDebug('using ' + (config.getImageMode() != config.getImageModeEnum().BINARY ? 'text' : 'binary') + ' websocket');

        try
        {
            ws.onopen = function() { open(); };
            ws.onmessage = function(e) { message(e); };
            ws.onerror = function() { error(); };
            ws.onclose = function(e) { close(e); };
        }
        catch (exc)
        {
            dialog.showDebug('websocket events init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            ws = null;
        }
    };

    function open()
    {
        //dialog.showDebug('websocket connection opened');
        wsOpened = true;

        // as websockets don't involve any standard http communication, the http session will timeout after a given time (default 20mn)
        // below is a periodical dummy call, using xhr, to keep it alive
        window.setInterval(function()
        {
            //dialog.showDebug('http session keep alive');
            network.getXmlhttp().send(null, new Date().getTime());
        },
        config.getHttpSessionKeepAliveInterval());

        // send settings and request a fullscreen update
        network.initClient();
    }

    function message(e)
    {
        if (config.getAdditionalLatency() > 0)
        {
            window.setTimeout(function() { receive(e.data); }, Math.round(config.getAdditionalLatency() / 2));
        }
        else
        {
            receive(e.data);
        }
    }

    function error()
    {
        dialog.showDebug('websocket connection error');
        wsError = true;
    }

    function close(e)
    {
        if (wsOpened && !wsError)
        {
            //dialog.showDebug('websocket connection closed');
        }
        else
        {
            // the websocket failed, fallback to long-polling
            dialog.showDebug('websocket connection closed with error code ' + e.code + ' (if you have IIS 8 or greater, ensure the websocket protocol is enabled), falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            network.init();
        }

        wsOpened = false;
    }

    function wsSend(text)
    {
        if (config.getImageMode() != config.getImageModeEnum().BINARY)
        {
            ws.send(text);
        }
        else
        {
            ws.send(strToBytes(text));
        }
    }

    this.send = function(data, startTime)
    {
        try
        {
            // in case of ws issue, the event data is not sent
            // if that occurs and a buffer is used (not mandatory but will help to lower the network stress), the buffer must not be cleared... the event data can still be sent on its next flush tick!
            // this can't be done if no buffer is used (the unsent event data is lost...)
            var buffer = network.getBuffer();
            if (buffer != null)
            {
                buffer.setClearBuffer(false);
            }

            if (ws == null)
            {
                //dialog.showDebug('ws is null');
                return;
            }

            if (!wsOpened)
            {
                //dialog.showDebug('ws is not ready');
                return;
            }

            wsSend(
                (data == null ? '' : data) +
                '|' + display.getImgIdx() +
                '|' + startTime);

            if (buffer != null)
            {
                buffer.setClearBuffer(true);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket send error: ' + exc.message);
        }
    };

    function receive(data)
    {
        try
        {
            //dialog.showDebug('received websocket data: ' + data + ', length: ' + data.Length + ', byteLength: ' + data.byteLength);

            if (data != null && data != '')
            {
                var message = '';

                if (config.getImageMode() != config.getImageModeEnum().BINARY)
                {
                    message = data;
                }
                else
                {
                    var imgTag = new Uint32Array(data, 0, 1);

                    //dialog.showDebug('image tag: ' + imgTag[0]);
                    if (imgTag[0] != 0)
                    {
                        message = bytesToStr(data);
                    }
                }

                // reload page
                if (message == 'reload')
                {
                    window.location.href = window.location.href;
                }
                //receive terminal data, send to xtermjs
                else if (message.length >= 5 && message.substr(0, 5) == 'term|') {
                    display.getDivs().writeTerminal(message.substr(5, message.length - 5));
                }
                // remote clipboard
                else if (message.length >= 10 && message.substr(0, 10) == 'clipboard|')
                {
                    showDialogPopup('showDialogPopup', 'ShowDialog.aspx', 'Ctrl+C to copy to local clipboard (Cmd-C on Mac)', message.substr(10, message.length - 10), true);
                }
                // print job
                else if (message.length >= 9 && message.substr(0, 9) == 'printjob|')
                {
                    downloadPdf(message.substr(9, message.length - 9));
                }
                // disconnected session
                else if (message == 'disconnected')
                {
                    window.location.href = config.getHttpServerUrl();
                }
                // server ack
                else if (message.length >= 4 && message.substr(0, 4) == 'ack,')
                {
                    var ackInfo = message.split(',');
                    //dialog.showDebug('websocket ack: ' + ackInfo[1]);

                    // update the average "latency"
                    network.updateLatency(parseInt(ackInfo[1]));
                }
                // new image
                else
                {
                    var imgInfo, idx, posX, posY, width, height, format, quality, fullscreen, imgData;

                    if (config.getImageMode() != config.getImageModeEnum().BINARY)
                    {
                        imgInfo = message.split(',');

                        idx = parseInt(imgInfo[0]);
                        posX = parseInt(imgInfo[1]);
                        posY = parseInt(imgInfo[2]);
                        width = parseInt(imgInfo[3]);
                        height = parseInt(imgInfo[4]);
                        format = imgInfo[5];
                        quality = parseInt(imgInfo[6]);
                        fullscreen = imgInfo[7] == 'true';
                        imgData = imgInfo[8];
                    }
                    else
                    {
                        imgInfo = new Uint32Array(data, 4, 8);

                        idx = imgInfo[0];
                        posX = imgInfo[1];
                        posY = imgInfo[2];
                        width = imgInfo[3];
                        height = imgInfo[4];
                        format = display.getFormatText(imgInfo[5]);
                        quality = imgInfo[6];
                        fullscreen = imgInfo[7] == 1;
                        imgData = new Uint8Array(data, 36, data.byteLength - 36);
                    }

                    // update bandwidth usage
                    network.setBandwidthUsage(network.getBandwidthUsage() + imgData.length);

                    // if a fullscreen request is pending, release it
                    if (fullscreen && fullscreenPending)
                    {
                        //dialog.showDebug('received a fullscreen update, divs will be cleaned');
                        fullscreenPending = false;
                    }

                    // add image to display
                    display.addImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);

                    // if using divs and count reached a reasonable number, request a fullscreen update
                    if (config.getDisplayMode() != config.getDisplayModeEnum().CANVAS && display.getImgCount() >= config.getImageCountOk() && !fullscreenPending)
                    {
                        //dialog.showDebug('reached a reasonable number of divs, requesting a fullscreen update');
                        fullscreenPending = true;
                        network.send(network.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text);
                    }
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket receive error: ' + exc.message);
        }
    }

    function strToBytes(str)
    {
        var bytes = new ArrayBuffer(str.length);
        var arr = new Uint8Array(bytes);

        for (var i = 0; i < str.length; i++)
        {
            arr[i] = str.charCodeAt(i);
        }

        return bytes;
    }

    function bytesToStr(bytes)
    {
        var str = '';
        var arr = new Uint8Array(bytes);

        for (var i = 0; i < bytes.byteLength; i++)
        {
            str += String.fromCharCode(arr[i]);
        }

        return str;
    }
}