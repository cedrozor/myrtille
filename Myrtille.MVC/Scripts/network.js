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
/*** Network                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function Network(config, dialog, display)
{
    // xmlhttp
    var xmlhttp = null;
    this.getXmlhttp = function() { return xmlhttp; };

    // websocket
    var websocket = null;

    // long-polling
    var longPolling = null;
    this.getLongPolling = function() { return longPolling; };
    this.setLongPolling = function(pLongPolling) { longPolling = pLongPolling; };

    // buffer
    var buffer = null;
    this.getBuffer = function() { return buffer; };
    this.setBuffer = function(pBuffer) { buffer = pBuffer; };

    // average roundtrip duration
    var roundtripDurationAvg = null;

    // roundtrip duration warning
    var roundtripDurationWarning = false;

    // bandwidth usage
    var bandwidthUsageB64 = null;
    this.getBandwidthUsageB64 = function() { return bandwidthUsageB64; };
    this.setBandwidthUsageB64 = function(value) { bandwidthUsageB64 = value; };
    this.getBandwidthUsageKBSec = function() { return doGetBandwidthUsageKBSec(); };
    function doGetBandwidthUsageKBSec() { return Math.ceil(((bandwidthUsageB64 * 3) / 4) / 1024); };

    // bandwidth size
    var bandwidthSizeKBSec = null;
    this.getBandwidthSizeKBSec = function() { return bandwidthSizeKBSec; };

    this.init = function()
    {
        try
        {
            // xhr support is the minimal network requirement
            xmlhttp = new XmlHttp(config, dialog, display, this);
            xmlhttp.init();

            // use websocket if enabled
            config.setWebSocketEnabled(config.getWebSocketEnabled() && (window.WebSocket || window.MozWebSocket) && !config.getCompatibilityMode());
            if (config.getWebSocketEnabled())
            {
                websocket = new Websocket(config, dialog, display, this);
                websocket.init();
            }

            dialog.showStat('websocket', config.getWebSocketEnabled());
            
            // if not using websocket, use xhr and * if enabled * long-polling
            if (!config.getWebSocketEnabled())
            {
                // if long-polling is enabled, updates are streamed into a zero sized iframe (with automatic (re)load)
                // otherwise (xhr only), they are returned within the xhr response
                if (config.getLongPollingEnabled())
                {
                    longPolling = new LongPolling(config, dialog, display, this);
                    longPolling.init();
                }

                //dialog.showDebug('initial fullscreen update');
                doSend(null);
            }

            dialog.showStat('longpolling', config.getLongPollingEnabled());

            // if using xhr only, force enable the user inputs buffer in order to allow polling update(s) even if the user does nothing ("send empty" feature, see comments in buffer.js)
            // even if using websocket or long-polling, using a buffer is recommended
            config.setBufferEnabled(config.getBufferEnabled() || (!config.getWebSocketEnabled() && !config.getLongPollingEnabled()));
            if (config.getBufferEnabled())
            {
                buffer = new Buffer(config, dialog, this);
                buffer.init();
            }

            // periodical fullscreen update; to fix potential display issues and clean the browser DOM when divs are used
            window.setInterval(function()
            {
                //dialog.showDebug('periodical fullscreen update');
                doSend(null);
            },
            config.getPeriodicalFullscreenIntervalDelay());

            // bandwidth usage per second; if the ratio goes up to 100% or above, the image quality is tweaked down server side to maintain a decent performance level
            window.setInterval(function()
            {
                //dialog.showDebug('checking bandwidth usage');
                dialog.showStat('bandwidthUsage', doGetBandwidthUsageKBSec());
                bandwidthUsageB64 = 0;
            },
            1000);

            // bandwidth size; 5MB test file (make sure not to set an interval too small as it may hinder performance if the bandwidth is weak; additionaly, the bandwidth is not meant to change very often...)
            updateBandwidth();
            window.setInterval(function()
            {
                updateBandwidth();
            },
            config.getBandwidthCheckIntervalDelay());
        }
        catch (exc)
        {
            dialog.showDebug('network init error: ' + exc.message);
            throw exc;
        }
    };

    this.updateLatency = function(startTime)
    {
        try
        {
            // check roundtrip start time
            if (startTime == null || startTime == '')
            {
                dialog.showDebug('updateLatency error: roundtrip start time is null or empty');
                return;
            }

            var now = new Date().getTime();
            if (now < startTime)
            {
                dialog.showDebug('updateLatency error: roundtrip start time inconsistency');
                return;
            }

            //dialog.showDebug('updateLatency: startTime: ' + startTime);

            // update the average "latency" (so called for simplification...); in fact, the client/server roundtrip duration ≈ connection physical latency (up/down link) + simulated latency (if enabled) + server process time
            // also, it's not an real average (more a linearization...)
            var roundtripDuration = now - startTime;
            if (roundtripDurationAvg == null)
            {
                roundtripDurationAvg = roundtripDuration;
            }
            else
            {
                roundtripDurationAvg = Math.round((roundtripDurationAvg + roundtripDuration) / 2);
            }

            dialog.showStat('latency', roundtripDurationAvg);

            // if the "latency" is above a certain limit, display a warning message
            if (roundtripDurationAvg > config.getRoundtripDurationMax())
            {
                dialog.showMessage('latency warning (> ' + config.getRoundtripDurationMax() + ' ms). Please check your network connection', 0);
                roundtripDurationWarning = true;
            }
            else
            {
                if (roundtripDurationWarning)
                {
                    roundtripDurationWarning = false;
                    dialog.hideMessage();
                }
                
                // if using an inputs buffer, update its delay accordingly (the more "latency", the more bufferization... and inversely)
                if (config.getBufferEnabled())
                {
                    if (buffer.getSendEmptyBuffer())
                    {
                        buffer.setBufferDelay(config.getBufferDelayEmpty() + roundtripDurationAvg);
                    }
                    else
                    {
                        buffer.setBufferDelay(config.getBufferDelayBase() + roundtripDurationAvg);
                    }
                    dialog.showStat('buffer', buffer.getBufferDelay());
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('network updateLatency error: ' + exc.message);
        }
    };

    function updateBandwidth()
    {
        try
        {
            //dialog.showDebug('checking available bandwidth');

            var startTime = new Date().getTime();

            var img = new Image();

            img.onload = function()
            {
                var endTime = new Date().getTime();
                var duration = endTime - startTime;
                bandwidthSizeKBSec = Math.ceil(((5087765 * 1000) / duration) / 1024);
                //dialog.showDebug('bandwidth check duration (ms): ' + duration + ', size (KB/s): ' + bandwidthSizeKBSec);
                dialog.showStat('bandwidthSize', bandwidthSizeKBSec);
            }

            img.onabort = function()
            {
                dialog.showDebug('bandwidth check aborted');
            };

            img.onerror = function()
            {
                dialog.showDebug('bandwidth check error');
            };

            img.src = config.getHttpServerUrl() + 'img/bandwidthTest.png?noCache=' + startTime;   // 5MB file size
        }
        catch (exc)
        {
            dialog.showDebug('network updateBandwidth error: ' + exc.message);
        }
    }

    this.processUserEvent = function(sender, event)
    {
        try
        {
            // if using a buffer, bufferize the user input
            if (config.getBufferEnabled())
            {
                //dialog.showDebug('buffering ' + sender + ' event: ' + event);
                if (buffer.getBufferData().length >= config.getBufferSize())
                {
                    //dialog.showDebug('buffer is full, flushing');
                    buffer.flush();
                }
                buffer.getBufferData().push(event);
            }
            // otherwise, send it over the network
		    else
            {
                dialog.showStat(sender, 'NONE');
                //dialog.showDebug('sending ' + sender + ' event: ' + event);
                doSend(event);
            }
        }
        catch (exc)
        {
            dialog.showDebug('network processUserEvent error: ' + exc.message);
        }
    };

    this.send = function(data)
    {
        doSend(data);
    };

    function doSend(data)
    {
        try
        {
            //dialog.showDebug('sending data: ' + data + ', current image index: ' + display.getImgIdx());
            var now = new Date().getTime();
            if (config.getAdditionalLatency() > 0)
            {
                window.setTimeout(function() { if (!config.getWebSocketEnabled()) { xmlhttp.send(data, now); } else { websocket.send(data, now); } }, Math.round(config.getAdditionalLatency() / 2));
            }
            else
            {
                if (!config.getWebSocketEnabled()) { xmlhttp.send(data, now); } else { websocket.send(data, now); }
            }
        }
        catch (exc)
        {
            dialog.showDebug('network send error: ' + exc.message);
        }
    }
}