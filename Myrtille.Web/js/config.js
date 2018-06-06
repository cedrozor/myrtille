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
/*** Config                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Config(
    httpServerUrl,                                      // myrtille web server url (rdp gateway)
    statEnabled,                                        // displays various stats above the remote session display
    debugEnabled,                                       // displays debug messages; more traces can be enabled by uncommenting them in js files
    compatibilityMode,                                  // old HTML4 browsers (no websocket, no canvas) or HTML5 otherwise
    scaleDisplay,                                       // scale the remote session display to the browser size (responsive design)
    displayWidth,                                       // remote session original (unscaled) display width
    displayHeight,                                      // remote session original (unscaled) display height
    hostType)                                           // type of remote session host RDP or SSH
{
    /*************************************************************************************************************************************************************************************************/
    /*** Enums                                                                                                                                                                                     ***/
    /*************************************************************************************************************************************************************************************************/

    // see comments into display.js
    var displayModeEnum =
    {
        AUTO: { value: 0, text: 'AUTO' },
        DIV: { value: 1, text: 'DIV' },
        CANVAS: { value: 2, text: 'CANVAS' }
    };

    // see comments into display.js
    var imageEncodingEnum =
    {
        AUTO: { value: 0, text: 'AUTO' },
        PNG: { value: 1, text: 'PNG' },
        JPEG: { value: 2, text: 'JPEG' },
        WEBP: { value: 3, text: 'WEBP' }
    };

    // see comments into network.js
    var imageModeEnum =
    {
        AUTO: { value: 0, text: 'AUTO' },
        ROUNDTRIP: { value: 1, text: 'ROUNDTRIP' },
        BASE64: { value: 2, text: 'BASE64' },
        BINARY: { value: 3, text: 'BINARY' }
    };

    // see comments into network.js
    var networkModeEnum =
    {
        AUTO: { value: 0, text: 'AUTO' },
        XHR: { value: 1, text: 'XHR' },
        LONGPOLLING: { value: 2, text: 'LONGPOLLING' },
        WEBSOCKET: { value: 3, text: 'WEBSOCKET' }
    };

    // starting from IE9, it's possible to use Object.freeze along with enums (to prevent them being modified, making them static objects)
    // see https://stijndewitt.com/2014/01/26/enums-in-javascript/

    if (Object.freeze)
    {
        Object.freeze(displayModeEnum);
        Object.freeze(imageEncodingEnum);
        Object.freeze(imageModeEnum);
        Object.freeze(networkModeEnum);
    }

    /*************************************************************************************************************************************************************************************************/
    /*** Settings                                                                                                                                                                                  ***/
    /*************************************************************************************************************************************************************************************************/

    // dialog
    var debugConsole = false;                           // output the debug messages into the browser javascript console (or on screen otherwise)
    var debugLinesMax = 40;                             // max number of displayed debug lines (rollover) when the output is on screen
    var keyboardHelperEnabled = false;                  // display a yellow tooltip to show the user inputs on-the-fly; useful when the latency is high as the user can see a direct result of its action
    var keyboardHelperSize = 75;                        // max number of characters to display into the keyboard helper
    var keyboardHelperTimeout = 3000;                   // duration (ms) before removing the keyboard helper

    // display
    var displayMode = displayModeEnum.AUTO;             // display mode
    var imageEncoding = imageEncodingEnum.PNG;          // image encoding
    var imageQuality = 100;                             // image quality (%) higher = better; not applicable for PNG (lossless); tweaked dynamically to fit the available bandwidth if using JPEG, PNG_JPEG or WEBP encoding. for best user experience, fullscreen updates are always done in higher quality (75%), regardless of this setting and bandwidth
    var imageQuantity = 100;                            // image quantity (%) less images = lower cpu and bandwidth usage / faster; more = smoother display (skipping images may result in some display inconsistencies). tweaked dynamically to fit the available bandwidth; possible values: 5, 10, 20, 25, 50, 100 (lower = higher drop rate)
    var imageTweakLowerThreshold = 50;                  // tweak the image quality & quantity depending on the available bandwidth: lower threshold
    var imageTweakHigherThreshold = 90;                 // tweak the image quality & quantity depending on the available bandwidth: higher threshold
    var imageCountOk = 500;                             // reasonable number of images to display at once; for HTML4 (divs), used to clean the DOM (by requesting a fullscreen update) as too many divs may slow down the browser; not applicable for HTML5 (canvas)
    var imageCountMax = 1000;                           // maximal number of images to display at once; for HTML4 (divs), used to clean the DOM (by reloading the page) as too many divs may slow down the browser; not applicable for HTML5 (canvas)
    var imageMode = imageModeEnum.AUTO;                 // image mode
    var imageBlobEnabled = false;                       // display images from local cached urls using blob objects (HTML5 only, binary mode)
    var imageDebugEnabled = false;                      // display a red border around images, for debug purpose
    var periodicalFullscreenInterval = 30000;           // periodical fullscreen update (ms); used to refresh the whole display
    var adaptiveFullscreenTimeout = 1500;               // adaptive fullscreen update (ms); requested after a given period of user inactivity (=no input). 0 to disable

    // network
    var additionalLatency = 0;                          // simulate a network latency (ms) which adds to the real latency (useful to test various network situations). 0 to disable
    var roundtripDurationMax = 5000;                    // roundtrip duration (ms) above which the connection is considered having issues
    var bandwidthCheckInterval = 300000;                // periodical bandwidth check; used to tweak down the images (quality & quantity) if the available bandwidth gets too low. it relies on a 5MB dummy file download, so this param shouldn't be set on a too short timer (or it will eat the bandwidth it's supposed to test...)
    var networkMode = networkModeEnum.AUTO;             // network mode
    var httpSessionKeepAliveInterval = 30000;           // periodical dummy xhr calls (ms) when using websocket, in order to keep the http session alive
    var xmlHttpTimeout = 3000;                          // xmlhttp requests (xhr) timeout (ms)
    var longPollingDuration = 60000;                    // long-polling requests duration (ms)
    var bufferEnabled = true;                           // buffer for user inputs; adjusted dynamically to fit the latency
    var bufferDelayBase = 0;                            // minimal buffering duration (ms)
    var bufferDelayEmpty = 10;                          // buffering duration (ms) when sending empty buffer
    var bufferSize = 128;                               // max number of buffered items (not size in bytes)

    // user
    var mouseMoveSamplingRate = 100;                    // sampling the mouse moves (%) may help to reduce the server load in applications that trigger a lot of updates (i.e.: imaging applications); possible values: 5, 10, 20, 25, 50, 100 (lower = higher drop rate)

    /*************************************************************************************************************************************************************************************************/
    /*** Properties                                                                                                                                                                                ***/
    /*************************************************************************************************************************************************************************************************/

    // about properties, starting from IE9 it's possible to define getters and setters... but these scripts are intended to work from IE6...
    // so, going old school...

    // server
    this.getHttpServerUrl = function() { return httpServerUrl; };
    
    // dialog
    this.getStatEnabled = function() { return statEnabled; };
    this.getDebugEnabled = function() { return debugEnabled; };
    this.getDebugConsole = function() { return debugConsole; };
    this.getDebugLinesMax = function() { return debugLinesMax; };
    this.getKeyboardHelperEnabled = function() { return keyboardHelperEnabled; };
    this.getKeyboardHelperSize = function() { return keyboardHelperSize; };
    this.getKeyboardHelperTimeout = function() { return keyboardHelperTimeout; };

    // display
    this.getCompatibilityMode = function() { return compatibilityMode; };
    this.getScaleDisplay = function() { return scaleDisplay; };
    this.getDisplayWidth = function() { return displayWidth; };
    this.getDisplayHeight = function() { return displayHeight; };
    this.getDisplayModeEnum = function() { return displayModeEnum; };
    this.getDisplayMode = function() { return displayMode; };
    this.setDisplayMode = function(mode) { displayMode = mode; };
    this.getImageEncodingEnum = function() { return imageEncodingEnum; };
    this.getImageEncoding = function() { return imageEncoding; };
    this.setImageEncoding = function(encoding) { imageEncoding = encoding; };
    this.getImageQuality = function() { return imageQuality; };
    this.setImageQuality = function(quality) { imageQuality = quality; };
    this.getImageQuantity = function() { return imageQuantity; };
    this.setImageQuantity = function(quantity) { imageQuantity = quantity; };
    this.getImageTweakLowerThreshold = function() { return imageTweakLowerThreshold; };
    this.getImageTweakHigherThreshold = function() { return imageTweakHigherThreshold; };
    this.getImageCountOk = function() { return imageCountOk; };
    this.getImageCountMax = function() { return imageCountMax; };
    this.getImageModeEnum = function() { return imageModeEnum; };
    this.getImageMode = function() { return imageMode; };
    this.setImageMode = function(mode) { imageMode = mode; };
    this.getImageBlobEnabled = function() { return imageBlobEnabled; };
    this.setImageBlobEnabled = function(enabled) { imageBlobEnabled = enabled; };
    this.getImageDebugEnabled = function() { return imageDebugEnabled; };
    this.getPeriodicalFullscreenInterval = function() { return periodicalFullscreenInterval; };
    this.getAdaptiveFullscreenTimeout = function() { return adaptiveFullscreenTimeout; };

    // network
    this.getAdditionalLatency = function() { return additionalLatency; };
    this.getRoundtripDurationMax = function() { return roundtripDurationMax; };
    this.getBandwidthCheckInterval = function() { return bandwidthCheckInterval; };
    this.getNetworkModeEnum = function() { return networkModeEnum; };
    this.getNetworkMode = function() { return networkMode; };
    this.setNetworkMode = function(mode) { networkMode = mode; };

    // websocket
    this.getHttpSessionKeepAliveInterval = function() { return httpSessionKeepAliveInterval; };
 
    // xmlhttp
    this.getXmlHttpTimeout = function() { return xmlHttpTimeout; };

    // long-polling
    this.getLongPollingDuration = function() { return longPollingDuration; };

    // buffer
    this.getBufferEnabled = function() { return bufferEnabled; };
    this.setBufferEnabled = function(enabled) { bufferEnabled = enabled; };
    this.getBufferDelayBase = function() { return bufferDelayBase; };
    this.getBufferDelayEmpty = function() { return bufferDelayEmpty; };
    this.getBufferSize = function() { return bufferSize; };
    
    // mouse
    this.getMouseMoveSamplingRate = function () { return mouseMoveSamplingRate; };

    // host type
    this.getHostType = function () { return hostType; }
}