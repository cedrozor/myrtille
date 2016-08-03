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
/*** Config                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Config(
    httpServerUrl,                                      // myrtille web server url (rdp gateway)
    httpSessionId,                                      // web session id, bound to a remote session (rdp) server side, also used as a security token
    webSocketPort,                                      // standard websocket port; ensure it's opened and not blocked by a third party program (antivirus/firewall/antispam, etc...)
    webSocketPortSecured,                               // secured websocket port; ensure it's opened and not blocked by a third party program (antivirus/firewall/antispam, etc...)
    statEnabled,                                        // displays various stats above the remote session display
    debugEnabled,                                       // displays debug messages; more traces can be enabled by uncommenting them in js files
    compatibilityMode)                                  // old HTML4 browsers (no websocket, no canvas) or HTML5 otherwise
{
    // you may change any of the variables below on-the-fly (then F5 to refresh display); proceed with caution!!!

    // debug
    var debugConsole = false;                           // output the debug messages into the browser javascript console (or on screen otherwise)
    var debugLinesMax = 40;                             // max number of displayed debug lines (rollover) when the output is on screen
    var keyboardHelperEnabled = true;                   // display a yellow tooltip to show the user inputs on-the-fly
    var keyboardHelperSize = 75;                        // max number of characters to display into the keyboard helper
    var keyboardHelperDelay = 3000;                     // duration (ms) before removing the keyboard helper

    // display
    var canvasEnabled = true;                           // 2d canvas; requires an HTML5 browser (fallback to divs if disabled or not supported)
    var imageEncoding = 'JPEG';                         // image encoding format; possible values: PNG, JPEG, PNG_JPEG, WEBP (see comments about pros and cons of each format in display.js)
    var imageQuality = 50;                              // image quality (%) higher = better; not applicable for PNG (lossless); tweaked dynamically to fit the available bandwidth if using JPEG, PNG_JPEG or WEBP encoding. for best user experience, fullscreen updates are always done in higher quality (75%), regardless of this setting and bandwidth
    var imageCountOk = 500;                             // reasonable number of images to display at once; for HTML4 (divs), used to clean the DOM (by requesting a fullscreen update) as too many divs may slow down the browser; not applicable for HTML5 (canvas)
    var imageCountMax = 1000;                           // maximal number of images to display at once; for HTML4 (divs), used to clean the DOM (by reloading the page) as too many divs may slow down the browser; not applicable for HTML5 (canvas)
    var imageBase64Enabled = true;                      // base64 image data (direct injection into the DOM; fallback to server roundtrip if not supported); pros: avoid server roundtrips to retrieve images; cons: base64 encoding have an overhead of about 33% compared to the images raw size
    var imageDebugEnabled = false;                      // display a red border around images, for debug purpose
    var periodicalFullscreenIntervalDelay = 30000;      // periodical fullscreen update (ms); used to refresh the whole display
    var adaptiveFullscreenTimeoutDelay = 1500;          // adaptive fullscreen update (ms); requested after a given period of user inactivity (=no input). 0 to disable

    // network
    var additionalLatency = 0;                          // simulate a network latency which adds to the real latency (useful to test various network situations). 0 to disable
    var roundtripDurationMax = 5000;                    // roundtrip duration (ms) above which the connection is considered having issues
    var bandwidthCheckIntervalDelay = 300000;           // periodical bandwidth check; used to tweak down the images quality when the available bandwidth gets too low. it relies on a 5MB dummy file download, so shouldn't be set on a too short timer (or it will eat the bandwidth it's supposed to test...)
    var webSocketEnabled = true;                        // websocket; requires an HTML5 browser (fallback to xhr if disabled or not supported)
    var httpSessionKeepAliveIntervalDelay = 30000;      // periodical dummy xhr calls (ms) when using websocket, in order to keep the http session alive
    var xmlHttpTimeoutDelay = 3000;                     // xmlhttp requests (xhr) timeout (ms)
    var longPollingEnabled = true;                      // long-polling requests (disabled if using websocket); ensure they are not blocked by a third party program (antivirus/firewall/antispam, etc...)
    var longPollingDuration = 60000;                    // long-polling requests duration (ms)
    var bufferEnabled = true;                           // buffer for user inputs; adjusted dynamically to fit the latency
    var bufferDelayBase = 0;                            // minimal buffering duration (ms)
    var bufferDelayEmpty = 10;                          // buffering duration (ms) when sending empty buffer
    var bufferSize = 128;                               // max number of buffered items (not size in bytes)

    // user
    var mouseMoveSamplingRate = 10;                     // sampling the mouse moves (%) may help to reduce the server load in applications that trigger a lot of updates (i.e.: imaging applications); 0 to disable, possible values: 5, 10, 20, 25, 50 (lower = higher drop rate)

    /*************************************************************************************************************************************************************************************************/
    /*** Properties                                                                                                                                                                                ***/
    /*************************************************************************************************************************************************************************************************/

    // about properties, starting from IE9 it's possible to define getters and setters... but these scripts are intended to work from IE6...
    // so, going old school...

    // server
    this.getHttpServerUrl = function() { return httpServerUrl; };
    this.getHttpSessionId = function() { return httpSessionId; };
    
    // dialog
    this.getStatEnabled = function() { return statEnabled; };
    this.getDebugEnabled = function() { return debugEnabled; };
    this.getDebugConsole = function() { return debugConsole; };
    this.getDebugLinesMax = function() { return debugLinesMax; };
    this.getKeyboardHelperEnabled = function() { return keyboardHelperEnabled; };
    this.getKeyboardHelperSize = function() { return keyboardHelperSize; };
    this.getKeyboardHelperDelay = function() { return keyboardHelperDelay; };

    // display
    this.getCompatibilityMode = function() { return compatibilityMode; };
    this.getCanvasEnabled = function() { return canvasEnabled; };
    this.setCanvasEnabled = function(enabled) { canvasEnabled = enabled; };
    this.getImageEncoding = function() { return imageEncoding; };
    this.setImageEncoding = function(encoding) { imageEncoding = encoding; };
    this.getImageQuality = function() { return imageQuality; };
    this.getImageCountOk = function() { return imageCountOk; };
    this.getImageCountMax = function() { return imageCountMax; };
    this.getImageBase64Enabled = function() { return imageBase64Enabled; };
    this.setImageBase64Enabled = function(enabled) { imageBase64Enabled = enabled; };
    this.getImageDebugEnabled = function() { return imageDebugEnabled; };
    this.getPeriodicalFullscreenIntervalDelay = function() { return periodicalFullscreenIntervalDelay; };
    this.getAdaptiveFullscreenTimeoutDelay = function() { return adaptiveFullscreenTimeoutDelay; };

    // network
    this.getAdditionalLatency = function() { return additionalLatency; };
    this.getRoundtripDurationMax = function() { return roundtripDurationMax; };
    this.getBandwidthCheckIntervalDelay = function() { return bandwidthCheckIntervalDelay; };
 
    // websocket
    this.getWebSocketEnabled = function() { return webSocketEnabled; };
    this.setWebSocketEnabled = function(enabled) { webSocketEnabled = enabled; };    
    this.getWebSocketPort = function() { return webSocketPort; };
    this.getWebSocketPortSecured = function() { return webSocketPortSecured; };
    this.getHttpSessionKeepAliveIntervalDelay = function() { return httpSessionKeepAliveIntervalDelay; };

    // xmlhttp
    this.getXmlHttpTimeoutDelay = function() { return xmlHttpTimeoutDelay; };

    // long-polling
    this.getLongPollingEnabled = function() { return longPollingEnabled; };
    this.setLongPollingEnabled = function(enabled) { longPollingEnabled = enabled; };
    this.getLongPollingDuration = function() { return longPollingDuration; };

    // buffer
    this.getBufferEnabled = function() { return bufferEnabled; };
    this.setBufferEnabled = function(enabled) { bufferEnabled = enabled; };
    this.getBufferDelayBase = function() { return bufferDelayBase; };
    this.getBufferDelayEmpty = function() { return bufferDelayEmpty; };
    this.getBufferSize = function() { return bufferSize; };
    
    // mouse
    this.getMouseMoveSamplingRate = function() { return mouseMoveSamplingRate; };
}