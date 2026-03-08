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
/*** Dialog                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Dialog(config)
{
    /*************************************************************************************************************************************************************************************************/
    /*** Stat                                                                                                                                                                                      ***/
    /*************************************************************************************************************************************************************************************************/

    var statDiv = null;

    var statHostType = 'RDP';

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

    var statAudioFormat = config.getAudioFormat();
    var statAudioBitrate = config.getAudioBitrate();

    var statData = {};
    this.getStatDataJson = function() { return JSON.stringify(statData); }

    var showStatEnum =
    {
        HOST_TYPE: { value: 0, text: 'HOST_TYPE' },
        LATENCY: { value: 1, text: 'LATENCY' },
        BUFFER: { value: 2, text: 'BUFFER' },
        BANDWIDTH_USAGE: { value: 3, text: 'BANDWIDTH_USAGE' },
        BANDWIDTH_SIZE: { value: 4, text: 'BANDWIDTH_SIZE' },
        NETWORK_MODE: { value: 5, text: 'NETWORK_MODE' },
        DISPLAY_MODE: { value: 6, text: 'DISPLAY_MODE' },
        IMAGE_COUNT: { value: 7, text: 'IMAGE_COUNT' },
        IMAGE_COUNT_PER_SEC: { value: 8, text: 'IMAGE_COUNT_PER_SEC' },
        IMAGE_COUNT_OK: { value: 9, text: 'IMAGE_COUNT_OK' },
        IMAGE_COUNT_MAX: { value: 10, text: 'IMAGE_COUNT_MAX' },
        IMAGE_INDEX: { value: 11, text: 'IMAGE_INDEX' },
        IMAGE_FORMAT: { value: 12, text: 'IMAGE_FORMAT' },
        IMAGE_QUALITY: { value: 13, text: 'IMAGE_QUALITY' },
        IMAGE_QUANTITY: { value: 14, text: 'IMAGE_QUANTITY' },
        IMAGE_MODE: { value: 15, text: 'IMAGE_MODE' },
        IMAGE_SIZE: { value: 16, text: 'IMAGE_SIZE' },
        AUDIO_FORMAT: { value: 17, text: 'AUDIO_FORMAT' },
        AUDIO_BITRATE: { value: 18, text: 'AUDIO_BITRATE' }
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
            //this.showDebug('showStat, key: ' + key.text + ', value: ' + value);

            switch (key)
            {
                case showStatEnum.HOST_TYPE:
                    statHostType = value;
                    break;

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

                case showStatEnum.AUDIO_FORMAT:
                    statAudioFormat = value;
                    break;

                case showStatEnum.AUDIO_BITRATE:
                    statAudioBitrate = value;
                    break;
            }

            // have all stats into an object that will be serialized in JSON
            statData.HostType = statHostType.text;
            statData.Latency = statLatency;
            statData.Buffer = statBuffer;
            statData.MouseMoveSampling = config.getMouseMoveSamplingRate() > 0 ? config.getMouseMoveSamplingRate() : 'NONE';
            statData.Bandwidth = statBandwidthUsage + '/' + statBandwidthSize + ' (' + (statBandwidthSize > 0 ? Math.round((statBandwidthUsage * 100) / statBandwidthSize) : 0) + '%)';
            statData.PeriodicalFSU = config.getPeriodicalFullscreenInterval() / 1000;
            statData.AdaptiveFSU = config.getAdaptiveFullscreenTimeout() / 1000;
            statData.NetworkMode = statNetworkMode.text + (statNetworkMode == config.getNetworkModeEnum().LONGPOLLING ? ' (' + config.getLongPollingDuration() / 1000 + 's)' : (statNetworkMode == config.getNetworkModeEnum().WEBSOCKET ? ' (' + config.getWebsocketCount() + ')' : ''));
            statData.DisplayMode = statDisplayMode.text;
            statData.ImageCount = statImageCount + ' (' + statImageCountPerSec + '/s)';
            statData.ImageIndex = statImageIndex;
            statData.ImageFormat = statImageFormat.toUpperCase();
            statData.ImageQuality = statImageQuality;
            statData.ImageQuantity = statImageQuantity;
            statData.ImageMode = statImageMode.text;
            statData.ImageSize = statImageSize;
            statData.Audio = statAudioFormat.text + (statAudioFormat == config.getAudioFormatEnum().NONE ? '' : ' (' + statAudioBitrate + ' kbps)');

            if (config.getStatEnabled())
            {
                if (statDiv == null)
                {
                    statDiv = document.getElementById('statDiv');
                    if (statDiv == null)
                    {
                        this.showDebug('statDiv is undefined');
                        return;
                    }
                    statDiv.style.display = 'inline-block';
                    statDiv.style.visibility = 'visible';
                }

                statDiv.innerText =
                    'TYPE: ' + statData.HostType + ', ' +
                    'LATENCY (ms): ' + statData.Latency + ', ' +
                    'BUFFER (ms): ' + statData.Buffer + ', ' +
                    'MOUSE SAMPLING (%): ' + statData.MouseMoveSampling + ', ' +
                    'BANDWIDTH (KB/s): ' + statData.Bandwidth + ', ' +
                    'PERIODICAL FSU (s): ' + statData.PeriodicalFSU + ', ' +
                    'ADAPTIVE FSU (s): ' + statData.AdaptiveFSU + ', ' +
                    'NETWORK: ' + statData.NetworkMode + ', ' +
                    'DISPLAY: ' + statData.DisplayMode + ', ' +
                    'IMG COUNT: ' + statData.ImageCount + ', ' +
                    'INDEX: ' + statData.ImageIndex + ', ' +
                    'FORMAT: ' + statData.ImageFormat + ', ' +
                    'QUALITY (%): ' + statData.ImageQuality + ', ' +
                    'QUANTITY (%): ' + statData.ImageQuantity + ', ' +
                    'MODE: ' + statData.ImageMode + ', ' +
                    'SIZE (KB): ' + statData.ImageSize + ', ' +
                    'AUDIO: ' + statData.Audio;
            }
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
                debugDiv.style.display = 'inline-block';
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

function openPopup(id, src, fade)
{
    try
    {
        // if there is already an opened popup, close it
        if (popup != null)
        {
            closePopup();
        }

        if (fade == null || fade)
        {
            // lock background
            var bgfDiv = document.getElementById('bgfDiv');
            if (bgfDiv != null)
            {
                bgfDiv.style.visibility = 'visible';
                bgfDiv.style.display = 'block';
            }
        }

        // add popup
        popup = document.createElement('iframe');
        popup.id = id;
        popup.src = 'popups/' + src;

        // draggable
        var dragDiv = document.getElementById('dragDiv');
        dragDiv.appendChild(popup);
        dragDiv.style.visibility = 'visible';
        dragDiv.style.display = 'block';
    }
    catch (exc)
    {
        console.error('openPopup error: ' + exc.message);
    }
}

function closePopup()
{
    try
    {
        // remove popup
        if (popup != null)
        {
            var dragDiv = document.getElementById('dragDiv');
            dragDiv.removeChild(popup);
            dragDiv.style.visibility = 'hidden';
            dragDiv.style.display = 'none';
            popup = null;
        }

        // unlock background
        var bgfDiv = document.getElementById('bgfDiv');
        if (bgfDiv != null)
        {
            bgfDiv.style.visibility = 'hidden';
            bgfDiv.style.display = 'none';
        }

        // get focus back
        window.focus();
    }
    catch (exc)
    {
        console.error('closePopup error: ' + exc.message);
    }
}

var showDialogPopupDesc = null;
this.getShowDialogPopupDesc = function() { return showDialogPopupDesc; };

var showDialogPopupText = null;
this.getShowDialogPopupText = function() { return showDialogPopupText; };

var showDialogPopupSelectText = false;
this.getShowDialogPopupSelectText = function() { return showDialogPopupSelectText; };

this.showDialogPopup = function(id, desc, text, selectText)
{
    // properties
    showDialogPopupDesc = desc;
    showDialogPopupText = text;
    showDialogPopupSelectText = selectText;

    // popup
    openPopup(id, 'ShowDialog.aspx');
}
