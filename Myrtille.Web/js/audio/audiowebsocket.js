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
/*** Audio Websocket                                                                                                                                                                               ***/
/*****************************************************************************************************************************************************************************************************/

function AudioWebsocket(base, config, dialog, display, network)
{
    var ws = null;
    var wsOpened = false;
    var wsError = false;

    this.init = function()
    {
        try
        {
            // IE doesn't support the WAV format
            if (display.isIEBrowser() && config.getAudioFormat() == config.getAudioFormatEnum().WAV)
            {
                config.setAudioFormat(config.getAudioFormatEnum().MP3);
                config.setAudioBitrate(320);
            }

            var wsUrl = config.getHttpServerUrl().replace('http', 'ws') + 'handlers/AudioSocketHandler.ashx?binary=true';

            //dialog.showDebug('audio websocket server url: ' + wsUrl);

            var wsImpl = window.WebSocket || window.MozWebSocket;
            ws = new wsImpl(wsUrl);
            ws.binaryType = 'arraybuffer';

            ws.onopen = function() { open(); };
            ws.onmessage = function(e) { message(e); };
            ws.onerror = function() { error(); };
            ws.onclose = function(e) { close(e); };
        }
        catch (exc)
        {
            dialog.showDebug('audio websocket init error: ' + exc.message);
            config.setAudioFormat(config.getAudioFormatEnum().NONE);
            ws = null;
        }
    };

    function open()
    {
        //dialog.showDebug('audio websocket connection opened');
        wsOpened = true;
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
        dialog.showDebug('audio websocket connection error');
        wsError = true;
    }

    function close(e)
    {
        if (wsOpened && !wsError)
        {
            //dialog.showDebug('audio websocket connection closed');
        }

        wsOpened = false;
    }

    function receive(data)
    {
        try
        {
            //dialog.showDebug('received audio data, byteLength: ' + data.byteLength);

            if (data != null && data.byteLength > 0)
            {
                // audio bufferization is done by the gateway
                var buffer = new Uint8Array(data, 0, data.byteLength);

                // play the audio
                var audio = new Audio();
                audio.autoplay = true;
                audio.src = 'data:audio/' + config.getAudioFormat().text.toLowerCase() + ';base64,' + bytesToBase64(buffer);

                // update bandwidth usage
                network.setBandwidthUsage(network.getBandwidthUsage() + data.byteLength);
            }
        }
        catch (exc)
        {
            dialog.showDebug('audio websocket receive error: ' + exc.message);
        }
    }
}