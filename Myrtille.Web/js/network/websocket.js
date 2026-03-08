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
/*** Websocket                                                                                                                                                                                     ***/
/*****************************************************************************************************************************************************************************************************/

function Websocket(base, config, dialog, display, network)
{
    function websocket(connection, opened, error)
    {
        this.connection = connection;
        this.opened = opened;
        this.error = error;
    }

    var ws = new Array();

    this.init = function()
    {
        try
        {
            var wsImpl = window.WebSocket || window.MozWebSocket;

            // using the IIS 8+ websockets support, the websocket server url is the same as http (there is just a protocol scheme change and a specific handler; standard and secured ports are the same)
            var wsBaseUrl = config.getHttpServerUrl().replace('http', 'ws') + 'handlers/SocketHandler.ashx?binary=' + (config.getImageMode() == config.getImageModeEnum().BINARY ? 'true' : 'false');
            var wsUrl;

            // 1 websocket up, n down
            if (config.getHostType() == config.getHostTypeEnum().RDP && config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && config.getWebsocketCount() > 1)
            {
                wsUrl = wsBaseUrl + '&direction=up';
                //dialog.showDebug('websocket server url: ' + wsUrl);
                ws[0] = new websocket(new wsImpl(wsUrl));

                // distribute the display updates load between different websockets (round robin)
                wsUrl = wsBaseUrl + '&direction=down';
                for (let i = 1; i <= config.getWebsocketCount() - 1; i++)
                {
                    ws[i] = new websocket(new wsImpl(wsUrl));
                }
            }
            // 1 websocket up, with ack
            else if (config.getNetworkMode() == config.getNetworkModeEnum().EVENTSOURCE)
            {
                config.setWebsocketCount(1);
                wsUrl = wsBaseUrl + '&direction=upWithAck';
                //dialog.showDebug('websocket server url: ' + wsUrl);
                ws[0] = new websocket(new wsImpl(wsUrl));
            }
            // 1 websocket up/down
            else
            {
                config.setWebsocketCount(1);
                wsUrl = wsBaseUrl + '&direction=duplex';
                //dialog.showDebug('websocket server url: ' + wsUrl);
                ws[0] = new websocket(new wsImpl(wsUrl));
            }
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
                ws[0].connection.binaryType = 'arraybuffer';

                if (config.getHostType() == config.getHostTypeEnum().RDP && config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && config.getWebsocketCount() > 1)
                {
                    for (let i = 1; i <= config.getWebsocketCount() - 1; i++)
                    {
                        ws[i].connection.binaryType = 'arraybuffer';
                    }
                }
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
            ws[0].connection.onopen = function() { open(0); };
            ws[0].connection.onmessage = function(e) { message(e); };
            ws[0].connection.onerror = function() { error(0); };
            ws[0].connection.onclose = function(e) { close(e, 0); };

            if (config.getHostType() == config.getHostTypeEnum().RDP && config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && config.getWebsocketCount() > 1)
            {
                for (let i = 1; i <= config.getWebsocketCount() - 1; i++)
                {
                    ws[i].connection.onopen = function() { open(i); };
                    ws[i].connection.onmessage = function(e) { message(e); };
                    ws[i].connection.onerror = function() { error(i); };
                    ws[i].connection.onclose = function(e) { close(e, i); };
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket events init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            ws = null;
        }
    };

    function open(i)
    {
        try
        {
            dialog.showDebug('websocket ' + i + ' opened');

            ws[i].opened = true;

            var init = false;

            // the init client sequence can't be done until the first websocket down is open (because the client won't be able to receive the connected notification otherwise)
            if (i == 0)
            {
                init = config.getWebsocketCount() <= 1 || ws[1].opened;
            }
            // the init client sequence can't be done until the websocket up is open (because the client won't be able to send the init sequence otherwise)
            else if (i == 1)
            {
                init = ws[0].opened;
            }

            if (init)
            {
                // as websockets don't involve any standard http communication, the http session will timeout after a given time (default 20mn)
                // below is a periodical dummy call, using xhr, to keep it alive
                window.setInterval(function()
                {
                    //dialog.showDebug('http session keep alive');
                    network.getXmlhttp().send(null, new Date().getTime());
                },
                config.getHttpSessionKeepAliveInterval());

                // if connecting, the gateway will automatically connect the remote server when the socket is opened server side
                // if connected, send the client settings and request a fullscreen update
                if (config.getConnectionState() == 'CONNECTED')
                {
                    base.initClient();
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket open error: ' + exc.message);
        }
    }

    function message(e)
    {
        try
        {
            // if the websocket is up, it's not meant to receive data
            // however, it's just a logical limitation; the websocket is still technically duplex, so let's handle any received data
            // it happens with event source for example, because server acks are retuned on the websocket

            if (config.getAdditionalLatency() > 0)
            {
                window.setTimeout(function() { receive(e.data); }, Math.round(config.getAdditionalLatency() / 2));
            }
            else
            {
                receive(e.data);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket message error: ' + exc.message);
        }
    }

    function error(i)
    {
        try
        {
            dialog.showDebug('websocket ' + i + ' error');
            ws[i].error = true;
        }
        catch (exc)
        {
            dialog.showDebug('websocket error error: ' + exc.message);
        }
    }

    function close(e, i)
    {
        try
        {
            if (ws[i].opened && !ws[i].error)
            {
                dialog.showDebug('websocket ' + i + ' closed');
            }
            else
            {
                // the websocket failed, fallback to long-polling
                dialog.showDebug('websocket ' + i + ' closed with error code ' + e.code + ' (if you have IIS 8 or greater, ensure the websocket protocol is enabled), falling back to long-polling');
                config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
                network.init();
            }

            ws[i].opened = false;
        }
        catch (exc)
        {
            dialog.showDebug('websocket close error: ' + exc.message);
        }
    }

    function wsSend(text)
    {
        try
        {
            if (config.getImageMode() != config.getImageModeEnum().BINARY)
            {
                ws[0].connection.send(text);
            }
            else
            {
                ws[0].connection.send(strToBytes(text));
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket send error: ' + exc.message);
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

            if (!ws[0].opened)
            {
                //dialog.showDebug('ws is not ready');
                return;
            }

            wsSend(
                (data == null ? '' : encodeURIComponent(data)) +
                '&' + display.getImgIdx() +
                '&' + network.getRoundtripDurationAvg() +
                '&' + startTime);

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
            //dialog.showDebug('received websocket data: ' + data + ', length: ' + data.length + ', byteLength: ' + data.byteLength);

            if (data != null && data != '')
            {
                var text = '';
                var message = true;
                var dataView = null;

                if (config.getImageMode() != config.getImageModeEnum().BINARY)
                {
                    text = data;

                    if (config.getHostType() == config.getHostTypeEnum().RDP)
                    {
                        try
                        {
                            // messages are serialized in JSON, unlike images; check a valid JSON string
                            JSON.parse(text);
                            //dialog.showDebug('message data: ' + text);
                        }
                        catch (exc)
                        {
                            message = false;
                            //dialog.showDebug('image data: ' + text);
                        }
                    }
                    else
                    {
                        //dialog.showDebug('terminal data: ' + text);
                    }
                }
                else
                {
                    dataView = new DataView(data);

                    var imgTag = dataView.getUint32(0, true);
                    //dialog.showDebug('image tag: ' + imgTag);

                    if (imgTag != 0)
                    {
                        var bytes = new Uint8Array(data, 0, data.byteLength);

                        if ('TextDecoder' in window)
                        {
                            var utf8Decoder = new TextDecoder();
                            text = utf8Decoder.decode(bytes);
                        }
                        else
                        {
                            text = decodeUtf8(bytes);
                        }

                        if (config.getHostType() == config.getHostTypeEnum().RDP)
                        {
                            //dialog.showDebug('message data: ' + text);
                        }
                        else
                        {
                            //dialog.showDebug('terminal data: ' + text);
                        }
                    }
                    else
                    {
                        message = false;
                        //dialog.showDebug('binary image');
                    }
                }

                // message
                if (message)
                {
                    processMessage(text);
                }
                // image
                else
                {
                    var imgInfo, idx, posX, posY, width, height, format, quality, fullscreen, imgData;

                    if (config.getImageMode() != config.getImageModeEnum().BINARY)
                    {
                        imgInfo = text.split(',');

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
                        idx = dataView.getUint32(4, true);
                        posX = dataView.getUint32(8, true);
                        posY = dataView.getUint32(12, true);
                        width = dataView.getUint32(16, true);
                        height = dataView.getUint32(20, true);
                        format = display.getFormatText(dataView.getUint32(24, true));
                        quality = dataView.getUint32(28, true);
                        fullscreen = dataView.getUint32(32, true) == 1;
                        imgData = new Uint8Array(data, 36, data.byteLength - 36);
                    }

                    processImageInOrder(idx, posX, posY, width, height, format, quality, fullscreen, imgData, 0);
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket receive error: ' + exc.message);
        }
    }

    function processImageInOrder(idx, posX, posY, width, height, format, quality, fullscreen, imgData, delayedCount)
    {
        try
        {
            let curIdx = display.getImgIdx();

            // image is the next in order or was delayed up to 10 times, display it
            if (curIdx == 0 || curIdx == idx - 1 || (curIdx < idx && delayedCount >= 10))
            {
                //dialog.showDebug('current image: ' + curIdx + ', displaying image ' + idx);
                processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);
            }
            // wait for the image turn in order
            else if (curIdx < idx)
            {
                //dialog.showDebug('current image: ' + curIdx + ', delaying image ' + idx + ', delayed count: ' + delayedCount);
                window.setTimeout(function() { processImageInOrder(idx, posX, posY, width, height, format, quality, fullscreen, imgData, ++delayedCount); }, 1);
            }
            // the image is older than or equal to the current image, drop it
            else
            {
                //dialog.showDebug('current image: ' + curIdx + ', dropping image ' + idx);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket processImageInOrder error: ' + exc.message);
        }
    }
}