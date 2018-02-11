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
/*** Dialog                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Dialog(config)
{
    /*************************************************************************************************************************************************************************************************/
    /*** Stat                                                                                                                                                                                      ***/
    /*************************************************************************************************************************************************************************************************/

    var statDiv = null;
    
    var statLatency = 0;
    var statBuffer = 'NONE';
    
    var statBandwidthUsage = 0;
    var statBandwidthSize = 0;

    var statNetworkMode = config.getNetworkMode();

    var statDisplayMode = config.getDisplayMode();
    
    var statImageCount = 0;
    var statImageCountPerSec = 0;
    var statImageCountOk = config.getImageCountOk();
    var statImageCountMax = config.getImageCountMax();
    var statImageIndex = 0;
    var statImageFormat = config.getImageEncoding().text;
    var statImageQuality = config.getImageQuality();
    var statImageQuantity = config.getImageQuantity();
    var statImageMode = config.getImageMode();
    var statImageSize = 0;

    var showStatEnum =
    {
        LATENCY: { value: 0, text: 'LATENCY' },
        BUFFER: { value: 1, text: 'BUFFER' },
        BANDWIDTH_USAGE: { value: 2, text: 'BANDWIDTH_USAGE' },
        BANDWIDTH_SIZE: { value: 3, text: 'BANDWIDTH_SIZE' },
        NETWORK_MODE: { value: 4, text: 'NETWORK_MODE' },
        DISPLAY_MODE: { value: 5, text: 'DISPLAY_MODE' },
        IMAGE_COUNT: { value: 6, text: 'IMAGE_COUNT' },
        IMAGE_COUNT_PER_SEC: { value: 7, text: 'IMAGE_COUNT_PER_SEC' },
        IMAGE_COUNT_OK: { value: 8, text: 'IMAGE_COUNT_OK' },
        IMAGE_COUNT_MAX: { value: 9, text: 'IMAGE_COUNT_MAX' },
        IMAGE_INDEX: { value: 10, text: 'IMAGE_INDEX' },
        IMAGE_FORMAT: { value: 11, text: 'IMAGE_FORMAT' },
        IMAGE_QUALITY: { value: 12, text: 'IMAGE_QUALITY' },
        IMAGE_QUANTITY: { value: 13, text: 'IMAGE_QUANTITY' },
        IMAGE_MODE: { value: 14, text: 'IMAGE_MODE' },
        IMAGE_SIZE: { value: 15, text: 'IMAGE_SIZE' }
    };

    if (Object.freeze)
    {
        Object.freeze(showStatEnum);
    }

    this.getShowStatEnum = function() { return showStatEnum; };

    // display settings and connection info
    this.showStat = function(key, value)
    {
        try
        {
            if (!config.getStatEnabled())
                return;

            //this.showDebug('showStat, key: ' + key.text + ', value: ' + value);

            if (statDiv == null)
            {
                statDiv = document.getElementById('statDiv');
                if (statDiv == null)
                {
                    this.showDebug('statDiv is undefined');
                    return;
                }
                statDiv.style.display = 'block';
                statDiv.style.visibility = 'visible';
            }

            switch (key)
            {
                case showStatEnum.LATENCY:
                    statLatency = value;
                    break;

                case showStatEnum.BUFFER:
                    statBuffer = value;
                    break;

                case showStatEnum.BANDWIDTH_USAGE:
                    statBandwidthUsage = value;
                    break;

                case showStatEnum.BANDWIDTH_SIZE:
                    statBandwidthSize = value;
                    break;
                
                case showStatEnum.NETWORK_MODE:
                    statNetworkMode = value;
                    break;
                
                case showStatEnum.DISPLAY_MODE:
                    statDisplayMode = value;
                    break;
                
                case showStatEnum.IMAGE_COUNT:
                    statImageCount = value;
                    break;

                case showStatEnum.IMAGE_COUNT_PER_SEC:
                    statImageCountPerSec = value;
                    break;

                case showStatEnum.IMAGE_COUNT_OK:
                    statImageCountOk = value;
                    break;

                case showStatEnum.IMAGE_COUNT_MAX:
                    statImageCountMax = value;
                    break;

                case showStatEnum.IMAGE_INDEX:
                    statImageIndex = value;
                    break;

                case showStatEnum.IMAGE_FORMAT:
                    statImageFormat = value;
                    break;

                case showStatEnum.IMAGE_QUALITY:
                    statImageQuality = value;
                    break;

                case showStatEnum.IMAGE_QUANTITY:
                    statImageQuantity = value;
                    break;

                case showStatEnum.IMAGE_MODE:
                    statImageMode = value;
                    break;
                
                case showStatEnum.IMAGE_SIZE:
                    statImageSize = value;
                    break;
            }

	        statDiv.innerHTML =
                'LATENCY (ms): ' + statLatency + ', ' +
                'BUFFER (ms): ' + statBuffer + ', ' +
                'MOUSE SAMPLING (%): ' + (config.getMouseMoveSamplingRate() > 0 ? config.getMouseMoveSamplingRate() : 'NONE') + ', ' +
                'BANDWIDTH (KB/s): ' + statBandwidthUsage + '/' + statBandwidthSize + ' (' + (statBandwidthSize > 0 ? Math.round((statBandwidthUsage * 100) / statBandwidthSize) : 0) + '%), ' +
                'PERIODICAL FSU (s): ' + config.getPeriodicalFullscreenInterval() / 1000 + ', ' +
                'ADAPTIVE FSU (s): ' + config.getAdaptiveFullscreenTimeout() / 1000 + ', ' +
                'NETWORK: ' + statNetworkMode.text + (statNetworkMode == config.getNetworkModeEnum().LONGPOLLING ? ' (' + config.getLongPollingDuration() / 1000 + 's)' : '') + ', ' +
                'DISPLAY: ' + statDisplayMode.text + ', ' +
                'IMG COUNT: ' + statImageCount + ' (' + statImageCountPerSec + '/s), OK: ' + statImageCountOk + ', MAX: ' + statImageCountMax + ', ' +
                'INDEX: ' + statImageIndex + ', ' +
                'FORMAT: ' + statImageFormat.toUpperCase() + ', ' +
                'QUALITY (%): ' + statImageQuality + ', ' +
                'QUANTITY (%): ' + statImageQuantity + ', ' +
                'MODE: ' + statImageMode.text + ', ' +
                'SIZE (KB): ' + statImageSize;
        }
        catch (exc)
        {
            this.showDebug('dialog showStat error: ' + exc.message);
        }
    };

    /*************************************************************************************************************************************************************************************************/
    /*** Debug                                                                                                                                                                                     ***/
    /*************************************************************************************************************************************************************************************************/

    var debugDiv = null;
    var debugLines = 0;
    var debugText = '';

    // display debug info
    this.showDebug = function(message)
    {
        try
        {
            if (!config.getDebugEnabled() || message == '')
                return;

            if (config.getDebugConsole() && window.console && window.console.log)
            {
                console.log(message);
                return;
            }

            if (debugDiv == null)
            {
                debugDiv = document.getElementById('debugDiv');
                if (debugDiv == null)
                {
                    alert('debugDiv is undefined');
                    return;
                }
                debugDiv.style.display = 'block';
                debugDiv.style.visibility = 'visible';
            }

	        if (debugLines > config.getDebugLinesMax())
	        {
		        debugLines = 0;
		        debugText = '';
	        }

            debugLines++;
	        debugText += message + '<br/>';

	        debugDiv.innerHTML = debugText;
        }
        catch (exc)
        {
            alert('dialog showDebug error: ' + exc.message);
        }
    };

    /*************************************************************************************************************************************************************************************************/
    /*** Message                                                                                                                                                                                   ***/
    /*************************************************************************************************************************************************************************************************/

    var msgDiv = null;
    var msgDisplayed = false;
    var msgDivTimeout = null;

    // display message info
    this.showMessage = function(message, duration)
    {
        try
        {
            if (msgDisplayed || message == '')
                return;
    
            if (msgDiv == null)
            {
                msgDiv = document.getElementById('msgDiv');
                if (msgDiv == null)
                {
                    this.showDebug('msgDiv is undefined');
                    return;
                }
            }

            msgDiv.style.display = 'block';
            msgDiv.style.visibility = 'visible';
            msgDiv.innerHTML = message;
            msgDisplayed = true;

            if (duration > 0)
            {
	            if (msgDivTimeout != null)
	            {
                    window.clearTimeout(msgDivTimeout);
		            msgDivTimeout = null;
	            }
	            msgDivTimeout = window.setTimeout(function() { doHideMessage(); }, duration);
            }
        }
        catch (exc)
        {
            this.showDebug('dialog showMessage error: ' + exc.message);
        }
    };

    this.hideMessage = function()
    {
        doHideMessage();
    };

    function doHideMessage()
    {
        try
        {
            if (!msgDisplayed)
                return;
        
	        if (msgDiv != null)
	        {
	            msgDiv.style.display = 'none';
		        msgDiv.style.visibility = 'hidden';
		        msgDiv.innerHTML = '';
		        msgDisplayed = false;
	        }
        }
        catch (exc)
        {
            this.showDebug('dialog hideMessage error: ' + exc.message);
        }
    }

    /*************************************************************************************************************************************************************************************************/
    /*** Keyboard helper                                                                                                                                                                           ***/
    /*************************************************************************************************************************************************************************************************/

    var kbhDiv = null;
    var kbhText = '';
    var kbhTimeout = null;

    // display typed keyboard text (useful when latency is high, as the user can see the result of its action immediately and is able to evaluate the latency)
    this.showKeyboardHelper = function(text)
    {
        try
        {
            if (!config.getKeyboardHelperEnabled() || text == '')
                return;
    
            if (kbhDiv == null)
            {
                kbhDiv = document.getElementById('kbhDiv');
                if (kbhDiv == null)
                {
                    this.showDebug('kbhDiv is undefined');
                    return;
                }
            }

            kbhDiv.style.display = 'block';
            kbhDiv.style.visibility = 'visible';

            kbhText += text;

            if (kbhText.length > config.getKeyboardHelperSize())
            {
                doHideKeyboardHelper();
            }
            else
            {
	            kbhDiv.innerHTML = kbhText;

                if (kbhTimeout != null)
                {
                    window.clearTimeout(kbhTimeout);
                    kbhTimeout = null;
                }

                kbhTimeout = window.setTimeout(function() { doHideKeyboardHelper(); }, config.getKeyboardHelperTimeout());
            }
        }
        catch (exc)
        {
            this.showDebug('dialog showKeyboardHelper error: ' + exc.message);
        }
    };

    this.hideKeyboardHelper = function()
    {
        doHideKeyboardHelper();
    };

    function doHideKeyboardHelper()
    {
        try
        {
            if (!config.getKeyboardHelperEnabled() || kbhDiv == null)
                return;

            if (kbhTimeout != null)
            {
                window.clearTimeout(kbhTimeout);
                kbhTimeout = null;
            }

            kbhDiv.style.display = 'none';
            kbhDiv.style.visibility = 'hidden';

            kbhText = '';
        }
        catch (exc)
        {
            this.showDebug('dialog hideKeyboardHelper error: ' + exc.message);
        }
    }
}

/*****************************************************************************************************************************************************************************************************/
/*** External Calls                                                                                                                                                                                ***/
/*****************************************************************************************************************************************************************************************************/

var popup = null;

function openPopup(id, src)
{
    // lock background
    var bgfDiv = document.getElementById('bgfDiv');
    if (bgfDiv != null)
    {
        bgfDiv.style.visibility = 'visible';
        bgfDiv.style.display = 'block';
    }

    // add popup
    popup = document.createElement('iframe');
    popup.id = id;
    popup.src = src;
    popup.className = 'modalPopup';

    document.body.appendChild(popup);
}

function closePopup()
{
    // remove popup
    if (popup != null)
    {
        document.body.removeChild(popup);
    }

    // unlock background
    var bgfDiv = document.getElementById('bgfDiv');
    if (bgfDiv != null)
    {
        bgfDiv.style.visibility = 'hidden';
        bgfDiv.style.display = 'none';
    }
}

var showDialogPopupDesc = null;
this.getShowDialogPopupDesc = function() { return showDialogPopupDesc; };

var showDialogPopupText = null;
this.getShowDialogPopupText = function() { return showDialogPopupText; };

var showDialogPopupSelectText = false;
this.getShowDialogPopupSelectText = function() { return showDialogPopupSelectText; };

this.showDialogPopup = function(id, src, desc, text, selectText)
{
    // properties
    showDialogPopupDesc = desc;
    showDialogPopupText = text;
    showDialogPopupSelectText = selectText;

    // popup
    openPopup(id, src);
}