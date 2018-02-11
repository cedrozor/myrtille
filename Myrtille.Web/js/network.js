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

    // buffer
    var buffer = null;
    this.getBuffer = function() { return buffer; };

    // periodical fullscreen update
    var periodicalFullscreenInterval = null;

    // average roundtrip duration
    var roundtripDurationAvg = null;

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
    var originalImageQuality = config.getImageQuality();
    var originalImageQuantity = config.getImageQuantity();

    /*
    prefixes (3 chars) are used to serialize commands with strings instead of numbers
    they make it easier to read log traces to find out which commands are issued
    they must match the prefixes used server side
    */
    var commandEnum =
    {
        // connection
        SEND_SERVER_ADDRESS: { value: 0, text: 'SRV' },
        SEND_USER_DOMAIN: { value: 1, text: 'DOM' },
        SEND_USER_NAME: { value: 2, text: 'USR' },
        SEND_USER_PASSWORD: { value: 3, text: 'PWD' },
        SEND_START_PROGRAM: { value: 4, text: 'PRG' },
        CONNECT_RDP_CLIENT: { value: 5, text: 'CON' },

        // browser
        SEND_BROWSER_RESIZE: { value: 6, text: 'RSZ' },

        // keyboard
        SEND_KEY_UNICODE: { value: 7, text: 'KUC' },
        SEND_KEY_SCANCODE: { value: 8, text: 'KSC' },

        // mouse
        SEND_MOUSE_MOVE: { value: 9, text: 'MMO' },
        SEND_MOUSE_LEFT_BUTTON: { value: 10, text: 'MLB' },
        SEND_MOUSE_MIDDLE_BUTTON: { value: 11, text: 'MMB' },
        SEND_MOUSE_RIGHT_BUTTON: { value: 12, text: 'MRB' },
        SEND_MOUSE_WHEEL_UP: { value: 13, text: 'MWU' },
        SEND_MOUSE_WHEEL_DOWN: { value: 14, text: 'MWD' },

        // control
        SET_STAT_MODE: { value: 15, text: 'STA' },
        SET_DEBUG_MODE: { value: 16, text: 'DBG' },
        SET_COMPATIBILITY_MODE: { value: 17, text: 'CMP' },
        SET_SCALE_DISPLAY: { value: 18, text: 'SCA' },
        SET_IMAGE_ENCODING: { value: 19, text: 'ECD' },
        SET_IMAGE_QUALITY: { value: 20, text: 'QLT' },
        SET_IMAGE_QUANTITY: { value: 21, text: 'QNT' },
        REQUEST_FULLSCREEN_UPDATE: { value: 22, text: 'FSU' },
        REQUEST_REMOTE_CLIPBOARD: { value: 23, text: 'CLP' },
        CLOSE_RDP_CLIENT: { value: 24, text: 'CLO' }
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
            /* image mode

            ROUNDTRIP
            display images from raw data
            the simplest mode. each image is retrieved using a server call
            pros: reliable (works in all browsers); cons: slower in case of high latency connection (due to the roundrip time)

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
            var binaryAvailable = (window.WebSocket || window.MozWebSocket) && !config.getCompatibilityMode();

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

            /* network mode

            XHR
            XmlHttpRequest is the basic requirement mode. user inputs and display updates are sent/received through the same request/response
            pros: reliable; cons: slower in case of high latency connection (due to the roundrip time and many requests)

            LONGPOLLING
            long-polling is a combination of xhr (to send user inputs) and long lived connection (to receive display updates)
            pros: faster than xhr because doesn't rely on roundtrip (and even brings a parallelized processing); cons: some proxies will timeout the connection passed a certain time

            WEBSOCKET
            websocket is the nowadays preferred communication method
            pros: fast and stateful duplex communication; cons: requires HTML5 (the above 2 methods work with HTML4 browsers) and some proxies may filter/block the (still evolving) websocket protocol
            
            AUTO (default)
            automatic detection of the best available mode (in order: XHR < LP < WS)

            */

            var wsAvailable = (window.WebSocket || window.MozWebSocket) && !config.getCompatibilityMode();

            switch (config.getNetworkMode())
            {
                case config.getNetworkModeEnum().XHR:
                case config.getNetworkModeEnum().LONGPOLLING:
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
            xmlhttp = new XmlHttp(config, dialog, display, this);
            xmlhttp.init();

            // use websocket if enabled
            if (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET)
            {
                websocket = new Websocket(config, dialog, display, this);
                websocket.init();
            }

            // only websocket supports the binary image mode
            if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET && config.getImageMode() == config.getImageModeEnum().BINARY)
            {
                config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            }
            
            // if not using websocket, use xhr and long-polling (or xhr only if XHR mode is specified)
            if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET)
            {
                // if long-polling is enabled, updates are streamed into a zero sized iframe (with automatic (re)load)
                // otherwise (xhr only), they are returned within the xhr response
                if (config.getNetworkMode() == config.getNetworkModeEnum().LONGPOLLING)
                {
                    longPolling = new LongPolling(config, dialog, display, this);
                    longPolling.init();
                }

                // send settings and request a fullscreen update
                this.initClient();
            }

            dialog.showStat(dialog.getShowStatEnum().NETWORK_MODE, config.getNetworkMode());

            // if using xhr only, force enable the user inputs buffer in order to allow polling update(s) even if the user does nothing ("send empty" feature, see comments in buffer.js)
            // even if using websocket or long-polling, using a buffer is recommended
            config.setBufferEnabled(config.getBufferEnabled() || config.getNetworkMode() == config.getNetworkModeEnum().XHR);
            if (config.getBufferEnabled())
            {
                buffer = new Buffer(config, dialog, this);
                buffer.init();
            }

            // periodical fullscreen update; to fix potential display issues and clean the browser DOM when divs are used
            if (periodicalFullscreenInterval != null)
            {
                window.clearInterval(periodicalFullscreenInterval);
                periodicalFullscreenInterval = null;
            }

            periodicalFullscreenInterval = window.setInterval(function()
            {
                //dialog.showDebug('periodical fullscreen update');
                doSend(commandEnum.REQUEST_FULLSCREEN_UPDATE.text);
            },
            config.getPeriodicalFullscreenInterval());

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

                // throttle the image quality & quantity depending on the bandwidth usage
                tweakDisplay();

                // reset bandwidth usage
                bandwidthUsage = 0;
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

    this.initClient = function()
    {
        try
        {
            var commands = new Array();

            //dialog.showDebug('sending rendering config');
            commands.push(commandEnum.SET_IMAGE_ENCODING.text + config.getImageEncoding().value);
            commands.push(commandEnum.SET_IMAGE_QUALITY.text + config.getImageQuality());
            commands.push(commandEnum.SET_IMAGE_QUANTITY.text + config.getImageQuantity());

            //dialog.showDebug('initial fullscreen update');
            commands.push(commandEnum.REQUEST_FULLSCREEN_UPDATE.text);

            doSend(commands.toString());
        }
        catch (exc)
        {
            dialog.showDebug('network initClient error: ' + exc.message);
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
                    dialog.showStat(dialog.getShowStatEnum().BUFFER, buffer.getBufferDelay());
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
            var tweak = false;

            var bandwidthRatio = bandwidthUsage != null && bandwidthSize != null && bandwidthSize > 0 ? Math.round((bandwidthUsage * 100) / bandwidthSize) : 0;
            if (bandwidthRatio >= config.getImageTweakHigherThreshold())
            {
                config.setImageQuality(10);
                config.setImageQuantity(25);
                tweak = true;
            }
            else if (bandwidthRatio >= config.getImageTweakLowerThreshold() && bandwidthRatio < config.getImageTweakHigherThreshold())
            {
                config.setImageQuality(25);
                config.setImageQuantity(50);
                tweak = true;
            }
            else if (config.getImageQuality() != originalImageQuality || config.getImageQuantity() != originalImageQuantity)
            {
                config.setImageQuality(originalImageQuality);
                config.setImageQuantity(originalImageQuantity);
                tweak = true;
            }

            if (tweak)
            {
                var commands = new Array();

                dialog.showDebug('tweaking display, image quality: ' + config.getImageQuality() + ', quantity: ' + config.getImageQuantity());
                commands.push(commandEnum.SET_IMAGE_QUALITY.text + config.getImageQuality());
                commands.push(commandEnum.SET_IMAGE_QUANTITY.text + config.getImageQuantity());

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
                if (buffer.getBufferData().length >= config.getBufferSize())
                {
                    //dialog.showDebug('buffer is full, flushing');
                    buffer.flush();
                }
                buffer.getBufferData().push(data);
            }
            // otherwise, send it over the network
		    else
            {
                dialog.showStat(dialog.getShowStatEnum().BUFFER, 'NONE');

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
                window.setTimeout(function() { if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET) { xmlhttp.send(data, now); } else { websocket.send(data, now); } }, Math.round(config.getAdditionalLatency() / 2));
            }
            else
            {
                if (config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET) { xmlhttp.send(data, now); } else { websocket.send(data, now); }
            }
        }
        catch (exc)
        {
            dialog.showDebug('network send error: ' + exc.message);
        }
    }
}