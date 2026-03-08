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
/*** Network                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function Network(base, config, dialog, display)
{
    // xmlhttp
    var xmlhttp = null;
    this.getXmlhttp = function() { return xmlhttp; };

    // long-polling
    var longPolling = null;

    // event source
    var eventsource = null;

    // websockets
    var websocket = null;
    var audioWebsocket = null;

    // buffer
    var buffer = null;
    this.getBuffer = function() { return buffer; };

    // periodical fullscreen update
    var periodicalFullscreenInterval = null;

    // browser pulse
    var browserPulseInterval = null;
    var browserPulseWorker = null;

    // average roundtrip duration
    var roundtripDurationAvg = null;
    this.getRoundtripDurationAvg = function() { return roundtripDurationAvg == null ? 0 : roundtripDurationAvg; };

    // roundtrip duration warning
    var roundtripDurationWarning = false;

    // bandwidth usage
    var bandwidthUsage = null;
    this.getBandwidthUsage = function() { return bandwidthUsage; };
    this.setBandwidthUsage = function(value) { bandwidthUsage = value; };
    var bandwidthUsageInterval = null;

    // bandwidth size
    var bandwidthSize = null;
    var bandwidthSizeInterval = null;

    // display tweaking
    var originalImageEncoding = config.getImageEncoding();
    var originalImageQuality = config.getImageQuality();
    var originalImageQuantity = config.getImageQuantity();
    var originalMouseMoveSamplingRate = config.getMouseMoveSamplingRate();

    this.getOriginalImageEncoding = function() { return originalImageEncoding; };
    this.setOriginalImageEncoding = function(encoding) { originalImageEncoding = encoding; };

    this.getOriginalImageQuality = function() { return originalImageQuality; };
    this.setOriginalImageQuality = function(quality) { originalImageQuality = quality; };

    this.getOriginalMouseMoveSamplingRate = function() { return originalMouseMoveSamplingRate; };
    this.setOriginalMouseMoveSamplingRate = function(rate) { originalMouseMoveSamplingRate = rate; };

    this.init = function()
    {
        try
        {
            var wsAvailable = (window.WebSocket || window.MozWebSocket) && !config.getCompatibilityMode();

            if (config.getHostType() == config.getHostTypeEnum().RDP)
            {
                /* image mode

                ROUNDTRIP
                display images from raw data
                the simplest mode. each image is retrieved using a server call
                pros: reliable (works in all browsers); cons: slower in case of high latency connection (due to the roundtrip time)

                BASE64
                display images from base64 data
                pros: avoid server roundtrips to retrieve images (direct injection into the DOM); cons: base64 encoding has an 33% overhead over binary
                IE6/7: not supported
                IE8: supported up to 32KB
                IE9: supported in native mode; not supported in compatibility mode (use IE7 engine)
                IE10+: supported
                please note that, even if base64 data is disabled or not supported by the client, the server will always send them in order to display images size and compute bandwidth usage, and thus be able to tweak the images (quality & quantity) if the available bandwidth gets too low
                it also workaround a weird glitch in IE7 that prevents script execution if code length is too low (when script code is injected into the DOM through long-polling)

                BINARY
                display images from binary data
                pros: no bandwidth overhead; cons: requires an HTML5 browser with websocket (and binary type) support
            
                AUTO (default)
                automatic detection of the best available mode (in order: ROUNDTRIP < BASE64 < BINARY)

                */

                var base64Available = display.isBase64Available();
                var binaryAvailable = wsAvailable;

                switch (config.getImageMode())
                {
                    case config.getImageModeEnum().ROUNDTRIP:
                        break;

                    case config.getImageModeEnum().BASE64:
                        if (!base64Available)
                        {
                            config.setImageMode(config.getImageModeEnum().ROUNDTRIP);
                        }
                        break;
                    
                    case config.getImageModeEnum().BINARY:
                        if (!binaryAvailable)
                        {
                            if (!base64Available)
                            {
                                config.setImageMode(config.getImageModeEnum().ROUNDTRIP);
                            }
                            else
                            {
                                config.setImageMode(config.getImageModeEnum().BASE64);
                            }
                        }
                        break;
                    
                    default:
                        config.setImageMode((!binaryAvailable ? (!base64Available ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64) : config.getImageModeEnum().BINARY));
                }

                dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            }

            /* network mode

            XHR
            XmlHttpRequest is the basic requirement mode. user inputs and display updates are sent/received through the same request/response
            pros: reliable; cons: slower in case of high latency connection (due to the roundtrip time and many requests)

            LONGPOLLING
            long-polling (aka COMET) is a combination of xhr (to send user inputs) and long lived connection (to receive display updates)
            pros: faster than xhr because doesn't rely on roundtrip (and even brings a parallelized processing); cons: some proxies will timeout the connection passed a certain time

            EVENTSOURCE
            event source is an HTML5 alternative to long-polling; used in combination with websocket (to send data), it creates a persistent connection with an http server, which can send events on it (SSE)
            pros: it's not an hack but a standard HTML5 web API and it's fast; cons: it can only handle text events, so images must be encoded into base64 (33% overhead!); it's also not supported on old IE/Edge

            WEBSOCKET
            websocket is the nowadays preferred communication method
            pros: fast and stateful duplex communication; cons: requires HTML5 (the above 2 methods work with HTML4 browsers) and some proxies may filter/block the (still evolving) websocket protocol
            
            AUTO (default)
            automatic detection of the best available mode (in order: XHR < LP < ES < WS)

            */

            switch (config.getNetworkMode())
            {
                case config.getNetworkModeEnum().XHR:
                case config.getNetworkModeEnum().LONGPOLLING:
                    config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                    break;

                case config.getNetworkModeEnum().EVENTSOURCE:
                    if (!wsAvailable)
                    {
                        config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
                    }
                    config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                    break;

                case config.getNetworkModeEnum().WEBSOCKET:
                    if (!wsAvailable)
                    {
                        config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
                    }
                    break;
                    
                default:
                    config.setNetworkMode(!wsAvailable ? config.getNetworkModeEnum().LONGPOLLING : config.getNetworkModeEnum().WEBSOCKET);
            }

            // xhr support is the minimal network requirement
            xmlhttp = new XmlHttp(base, config, dialog, display, this);
            xmlhttp.init();

            // use websocket if enabled or along with event source
            if (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET || config.getNetworkMode() == config.getNetworkModeEnum().EVENTSOURCE)
            {
                // inputs, display and notifications if duplex
                websocket = new Websocket(base, config, dialog, display, this);
                websocket.init();

                // when using event source, websocket is only used to send inputs and receive acks (not duplex)
                if (config.getNetworkMode() == config.getNetworkModeEnum().EVENTSOURCE)
                {
                    // display and notifications
                    eventsource = new Eventsource(base, config, dialog, display, this);
                    eventsource.init();
                }
            }

            // websocket enabled (or along with event source) and functional, RDP host, audio enabled
            // audio playback can also be switched on the gateway (web.config); this parameter takes precedence over config.js
            if ((config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET || config.getNetworkMode() == config.getNetworkModeEnum().EVENTSOURCE) && config.getHostType() == config.getHostTypeEnum().RDP && config.getAudioFormat() != config.getAudioFormatEnum().NONE)
            {
                // audio
                audioWebsocket = new AudioWebsocket(base, config, dialog, display, this);
                audioWebsocket.init();
            }
            else
            {
                // audio disabled
                config.setAudioFormat(config.getAudioFormatEnum().NONE);
            }

            // only websocket supports the binary image mode
            if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET && config.getImageMode() == config.getImageModeEnum().BINARY)
            {
                config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            }
            
            // if not using websocket or event source, use xhr and long-polling (or xhr only if XHR mode is specified)
            if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET && config.getNetworkMode() != config.getNetworkModeEnum().EVENTSOURCE)
            {
                // if long-polling is enabled, updates are streamed into a zero sized iframe (with automatic (re)load)
                // otherwise (xhr only), they are returned within the xhr response
                if (config.getNetworkMode() == config.getNetworkModeEnum().LONGPOLLING)
                {
                    longPolling = new LongPolling(base, config, dialog, display, this);
                    longPolling.init();
                }
                else
                {
                    base.initConnection();
                }
            }
            else
            {
                // if using websocket or event source, the connection remains open; there is no real need for a buffer
                config.setBufferEnabled(false);
            }

            dialog.showStat(dialog.getShowStatEnum().NETWORK_MODE, config.getNetworkMode());
            dialog.showStat(dialog.getShowStatEnum().AUDIO_FORMAT, config.getAudioFormat());
            dialog.showStat(dialog.getShowStatEnum().AUDIO_BITRATE, config.getAudioBitrate());

            // if using xhr only, force enable the user inputs buffer in order to allow polling update(s) even if the user does nothing ("send empty" feature, see comments in buffer.js)
            config.setBufferEnabled(config.getBufferEnabled() || config.getNetworkMode() == config.getNetworkModeEnum().XHR);
            if (config.getBufferEnabled())
            {
                buffer = new Buffer(base, config, dialog, this);
                buffer.init();
            }

            // periodical fullscreen update; to fix potential display issues and clean the browser DOM when divs are used
            // if the host is SSH, it will be used to detect if the client is still connected and to close the terminal otherwise
            if (periodicalFullscreenInterval != null)
            {
                window.clearInterval(periodicalFullscreenInterval);
                periodicalFullscreenInterval = null;
            }

            periodicalFullscreenInterval = window.setInterval(function()
            {
                //dialog.showDebug('periodical fullscreen update');
                doSend(base.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text + 'periodical');
            },
            config.getPeriodicalFullscreenInterval());

            // browser pulse
            // if possible, have the pulse into a web worker in order to keep it going even if the main thread is paused; this can happen if the window/tab is inactive (focus lost, for example)
            // this is important because the pulse is used by the gateway to check if the browser and the network connection are alive and disconnect the session otherwise (to free a guest slot, for example)
            if (window.Worker)
            {
                browserPulseWorker = createWorker('function(self)\n' +
                    '{\n' +
                    '   browserPulseInterval = setInterval(function()\n' +
                    '   {\n' +
                    '    self.postMessage(null);\n' +
                    '   },\n' +
                    '   ' + config.getBrowserPulseInterval() + ');\n' +
                    '}\n');

                browserPulseWorker.onmessage = function(e)
                {
                    //dialog.showDebug('browser pulse');
                    doSend(base.getCommandEnum().SEND_BROWSER_PULSE.text);
                }
            }
            else
            {
                browserPulseInterval = window.setInterval(function()
                {
                    //dialog.showDebug('browser pulse');
                    doSend(base.getCommandEnum().SEND_BROWSER_PULSE.text);
                },
                config.getBrowserPulseInterval());
            }

            // bandwidth usage per second; if the ratio goes up to 100% or above, tweak down the image quality & quantity to maintain a decent performance level
            if (bandwidthUsageInterval != null)
            {
                window.clearInterval(bandwidthUsageInterval);
                bandwidthUsageInterval = null;
            }

            bandwidthUsageInterval = window.setInterval(function()
            {
                //dialog.showDebug('checking bandwidth usage');
                dialog.showStat(dialog.getShowStatEnum().BANDWIDTH_USAGE, Math.ceil(bandwidthUsage / 1024));

                //dialog.showDebug('checking image count per second');
                dialog.showStat(dialog.getShowStatEnum().IMAGE_COUNT_PER_SEC, display.getImgCountPerSec());

                // throttle image quality & quantity depending on the bandwidth usage
                if (config.getHostType() == config.getHostTypeEnum().RDP)
                {
                    tweakDisplay();
                }

                // reset bandwidth usage
                bandwidthUsage = 0;

                // reset image count per second
                display.resetImgCountPerSec();

                // dummy call to keep the latency updated even if the user does nothing
                //dialog.showDebug('updating latency');
                doSend(null);
            },
            1000);

            // bandwidth size; 5MB test file (make sure not to set an interval too small as it may hinder performance if the bandwidth is weak; additionaly, the bandwidth is not meant to change very often...)
            updateBandwidth();

            if (bandwidthSizeInterval != null)
            {
                window.clearInterval(bandwidthSizeInterval);
                bandwidthSizeInterval = null;
            }

            bandwidthSizeInterval = window.setInterval(function()
            {
                updateBandwidth();
            },
            config.getBandwidthCheckInterval());
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
            //dialog.showDebug('updateLatency startTime: ' + startTime);

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

            dialog.showStat(dialog.getShowStatEnum().LATENCY, roundtripDurationAvg);

            // if the "latency" is above a certain limit, display a warning message
            if (config.getRoundtripDurationMax() > 0)
            {
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
                }
            }
                
            // if using an inputs buffer, update its delay accordingly (the more "latency", the more bufferization... and inversely)
            if (config.getBufferEnabled())
            {
                if (buffer.getSendEmptyBuffer())
                {
                    buffer.setBufferDelay(config.getBufferDelayEmpty() + Math.round(roundtripDurationAvg / 2));
                }
                else
                {
                    buffer.setBufferDelay(config.getBufferDelayBase() + Math.round(roundtripDurationAvg / 2));
                }
                dialog.showStat(dialog.getShowStatEnum().BUFFER, buffer.getBufferDelay());
            }
            else
            {
                dialog.showStat(dialog.getShowStatEnum().BUFFER, 'NONE');
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
                // change the value below (i.e. 500KB/s) and comment out the computation line to simulate a bandwidth size
                //bandwidthSize = 500 * 1024;
                bandwidthSize = (5087765 * 1000) / duration;
                //dialog.showDebug('bandwidth check duration (ms): ' + duration + ', size (KB/s): ' + Math.ceil(bandwidthSize / 1024));
                dialog.showStat(dialog.getShowStatEnum().BANDWIDTH_SIZE, Math.ceil(bandwidthSize / 1024));
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

    function tweakDisplay()
    {
        try
        {
            var commands = new Array();

            /*
            low bandwidth = lower quality, quantity and mouse move sampling rate
            */

            var tweak = false;

            var bandwidthUsageRatio = bandwidthUsage != null && bandwidthSize != null && bandwidthSize > 0 ? Math.round((bandwidthUsage * 100) / bandwidthSize) : 0;
            if (bandwidthUsageRatio >= config.getImageTweakBandwidthHigherThreshold())
            {
                if (config.getImageQuality() != 10)
                {
                    config.setImageEncoding(config.getImageEncodingEnum().JPEG);
                    config.setImageQuality(10);
                    tweak = true;
                }

                if (config.getImageQuantity() != 25)
                {
                    config.setImageQuantity(25);
                    tweak = true;
                }

                if (config.getMouseMoveSamplingRate() != 25)
                {
                    config.setMouseMoveSamplingRate(25);
                }
            }
            else if (bandwidthUsageRatio >= config.getImageTweakBandwidthLowerThreshold() && bandwidthUsageRatio < config.getImageTweakBandwidthHigherThreshold())
            {
                if (config.getImageQuality() != 25)
                {
                    config.setImageEncoding(config.getImageEncodingEnum().JPEG);
                    config.setImageQuality(25);
                    tweak = true;
                }

                if (config.getImageQuantity() != 50)
                {
                    config.setImageQuantity(50);
                    tweak = true;
                }

                if (config.getMouseMoveSamplingRate() != 50)
                {
                    config.setMouseMoveSamplingRate(50);
                }
            }
            else
            {
                if (config.getImageQuality() != originalImageQuality)
                {
                    config.setImageEncoding(originalImageEncoding);
                    config.setImageQuality(originalImageQuality);
                    tweak = true;
                }

                if (config.getImageQuantity() != originalImageQuantity)
                {
                    config.setImageQuantity(originalImageQuantity);
                    tweak = true;
                }

                if (config.getMouseMoveSamplingRate() != originalMouseMoveSamplingRate)
                {
                    config.setMouseMoveSamplingRate(originalMouseMoveSamplingRate);
                }
            }

            if (tweak)
            {
                //dialog.showDebug('tweaking image quality: ' + config.getImageEncoding().text + ', ' + config.getImageQuality());
                commands.push(base.getCommandEnum().SET_IMAGE_ENCODING.text + config.getImageEncoding().value);
                commands.push(base.getCommandEnum().SET_IMAGE_QUALITY.text + config.getImageQuality());
                //dialog.showDebug('tweaking image quantity: ' + config.getImageQuantity());
                commands.push(base.getCommandEnum().SET_IMAGE_QUANTITY.text + config.getImageQuantity());
                commands.push(base.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text + 'tweak');
            }

            if (commands.length > 0)
            {
                doSend(commands.toString());
            }
        }
        catch (exc)
        {
            dialog.showDebug('network tweakDisplay error: ' + exc.message);
        }
    }

    this.processUserEvent = function(event, data)
    {
        try
        {
            // if using a buffer, bufferize the data
            if (config.getBufferEnabled())
            {
                //dialog.showDebug('buffering ' + event + ' event: ' + data);
                buffer.addItem(data);
            }
            // otherwise, send it over the network
		    else
            {
                //dialog.showDebug('sending ' + event + ' event: ' + data);
                doSend(data);
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
            //dialog.showDebug('sending data: ' + data + ', img: ' + display.getImgIdx());

            var now = new Date().getTime();
            if (config.getAdditionalLatency() > 0)
            {
                window.setTimeout(function() { if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET && config.getNetworkMode() != config.getNetworkModeEnum().EVENTSOURCE) { xmlhttp.send(data, now); } else { websocket.send(data, now); } }, Math.round(config.getAdditionalLatency() / 2));
            }
            else
            {
                if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET && config.getNetworkMode() != config.getNetworkModeEnum().EVENTSOURCE) { xmlhttp.send(data, now); } else { websocket.send(data, now); }
            }
        }
        catch (exc)
        {
            dialog.showDebug('network send error: ' + exc.message);
        }
    }
}