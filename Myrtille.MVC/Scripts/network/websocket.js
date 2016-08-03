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
/*** Websocket                                                                                                                                                                                     ***/
/*****************************************************************************************************************************************************************************************************/

function Websocket(config, dialog, display, network)
{
    var ws = null;
    var wsNew = true;
    var wsOpened = false;
    var wsError = false;

    var fullscreenPending = false;

    this.init = function()
    {
        try
        {
            // websocket server url
            var wsUrl;
            if (window.location.protocol == 'http:')
            {
                wsUrl = 'ws://' + window.location.hostname + ':' + config.getWebSocketPort();
            }
            else
            {
                // undefined secure websocket port means that there is no available secure websocket server (missing .pfx certificate?); disable websocket
                // note that it's no longer possible to use unsecure websocket (ws://) when using https for the page... nowadays browsers block it...
                if (config.getWebSocketPortSecured() == null)
                {
                    alert('no available secure websocket server. Please ensure a .pfx certificate is installed on the server');
                    config.setWebSocketEnabled(false);
                    return;
                }
                wsUrl = 'wss://' + window.location.hostname + ':' + config.getWebSocketPortSecured();
            }

            //dialog.showDebug('websocket server url: ' + wsUrl);

            var wsImpl = window.WebSocket || window.MozWebSocket;
            ws = new wsImpl(wsUrl);

            ws.onopen = function() { open(); };
            ws.onmessage = function(e) { message(e); };
            ws.onerror = function() { error(); };
            ws.onclose = function(e) { close(e); };
        }
        catch (exc)
        {
            dialog.showDebug('websocket init error: ' + exc.Message);
            config.setWebSocketEnabled(false);
            ws = null;
        }
    };

    function open()
    {
        //dialog.showDebug('websocket connection opened');
        wsOpened = true;

        // as websocket is now active, long-polling is disabled (both can't be active at the same time...)
        dialog.showStat('longpolling', false);

        // as websockets don't involve any standard http communication, the http session will timeout after a given time (default 20mn)
        // below is a dummy call, using xhr, to keep it alive
        window.setInterval(function()
        {
            //dialog.showDebug('http session keep alive');
            network.getXmlhttp().send('', new Date().getTime());
        },
        config.getHttpSessionKeepAliveIntervalDelay());

        //dialog.showDebug('initial fullscreen update');
        network.send(null);
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
        wsOpened = false;

        if (!wsError)
        {
            //dialog.showDebug('websocket connection closed');
        }
        else
        {
            alert('websocket connection closed with error (code ' + e.code + '). Please ensure the port ' + (window.location.protocol == 'http:' ? config.getWebSocketPort() + ' (standard' : config.getWebSocketPortSecured() + ' (secured') + ' port) is opened on your firewall and no third party program is blocking the network traffic');

            // the websocket failed, disable it
            config.setWebSocketEnabled(false);
            dialog.showStat('websocket', config.getWebSocketEnabled());

            // if long-polling is enabled, start it
            if (config.getLongPollingEnabled())
            {
                alert('falling back to long-polling');
                dialog.showStat('longpolling', true);
                // as a websocket was used, long-polling should be null at this step; ensure it
                if (network.getLongPolling() == null)
                {
                    network.setLongPolling(new LongPolling(config, dialog, display, network));
                    network.getLongPolling().init();
                }
                else
                {
                    dialog.showDebug('network inconsistency... both websocket and long-polling were active at the same time; now using long-polling only');
                    network.getLongPolling().reset();
                }
            }

            // otherwise, both websocket and long-polling are disabled, fallback to xhr only
            else
            {
                alert('falling back to xhr only');
                // if using xhr only, force enable the user inputs buffer in order to allow polling update(s) even if the user does nothing ("send empty" feature, see comments in buffer.js)
                config.setBufferEnabled(true);
                // create a buffer if not already exists
                if (network.getBuffer() == null)
                {
                    network.setBuffer(new Buffer(config, dialog, network));
                    network.getBuffer().init();
                }
                // otherwise just enable its "send empty" feature
                else
                {
                    network.getBuffer().setBufferDelay(config.getBufferDelayEmpty());
                    network.getBuffer().setSendEmptyBuffer(true);
                }
            }
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

            ws.send(
                'input' +
                '|' + config.getHttpSessionId() +
                '|' + (data == null ? '' : data) +
                '|' + (data == null ? 1 : 0) +
                '|' + display.getImgIdx() +
                '|' + config.getImageEncoding() +
                '|' + config.getImageQuality() +
                '|' + (network.getBandwidthUsageKBSec() != null && network.getBandwidthSizeKBSec() != null && network.getBandwidthSizeKBSec() > 0 ? Math.round((network.getBandwidthUsageKBSec() * 100) / network.getBandwidthSizeKBSec()) : 0) +
                '|' + (wsNew ? 1 : 0) +
                '|' + startTime);

            wsNew = false;

            if (buffer != null)
            {
                buffer.setClearBuffer(true);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket send error: ' + exc.Message);
        }
    };

    function receive(data)
    {
        try
        {
            //dialog.showDebug('received websocket message: ' + data);
            
            if (data != null && data != '')
            {
                var parts = new Array();
                parts = data.split(',');
                
                // session disconnect
                if (parts.length == 1)
                {
                    if (parts[0] == 'disconnected')
                    {
                        // the websocket can now be closed server side
                        ws.send(
                            'close' +
                            '|' + config.getHttpSessionId());

                        // the remote session is disconnected
                        window.location.href = window.location.href.replace('connect', 'disconnect');
                    }
                }
                // server ack
                else if (parts.length == 2)
                {
                    if (parts[0] == 'ack')
                    {
                        //dialog.showDebug('websocket server ack');
                        
                        // update the average "latency"
                        network.updateLatency(parseInt(parts[1]));
                    }
                }
                // new image
                else
                {
                    var idx = parts[0];
                    var posX = parts[1];
                    var posY = parts[2];
                    var width = parts[3];
                    var height = parts[4];
                    var format = parts[5];
                    var quality = parts[6];
                    var base64Data = parts[7];
                    var fullscreen = parts[8] == 'true';

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
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket receive error: ' + exc.Message);
        }
    }
}