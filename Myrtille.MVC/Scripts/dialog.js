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
/*** Dialog                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Dialog(config)
{
    /*************************************************************************************************************************************************************************************************/
    /*** Stat                                                                                                                                                                                      ***/
    /*************************************************************************************************************************************************************************************************/

    var statDiv = null;
    
    var statLatency = 0;
    var statKeyboardBuffer = 'NONE';
    var statMouseBuffer = 'NONE';
    
    var statBandwidthUsage = 0;
    var statBandwidthSize = 0;

    var statWebSocketEnabled = config.getWebSocketEnabled();
    var statLongpollingEnabled = config.getLongPollingEnabled();
    var statCanvasEnabled = config.getCanvasEnabled();
    
    var statImageCount = 0;
    var statImageCountPerSec = 0;
    var statImageCountOk = config.getImageCountOk();
    var statImageCountMax = config.getImageCountMax();
    var statImageIndex = 0;
    var statImageFormat = 'png';
    var statImageQuality = 100;
    var statImageBase64Enabled = config.getImageBase64Enabled();
    var statImageSize = 0;

    // display settings and connection info
    this.showStat = function(key, value)
    {
        try
        {
            if (!config.getStatEnabled())
                return;

            //this.showDebug('stat: ' + key);

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
                case 'latency':
                    statLatency = value;
                    break;

                case 'keyboard':
                    statKeyboardBuffer = value;
                    break;

                case 'mouse':
                    statMouseBuffer = value;
                    break;

                case 'buffer':
                    statKeyboardBuffer = value;
                    statMouseBuffer = value;
                    break;

                case 'bandwidthUsage':
                    statBandwidthUsage = value;
                    break;

                case 'bandwidthSize':
                    statBandwidthSize = value;
                    break;
                
                case 'websocket':
                    statWebSocketEnabled = value;
                    break;
                
                case 'longpolling':
                    statLongpollingEnabled = value;
                    break;

                case 'canvas':
                    statCanvasEnabled = value;
                    break;
                
                case 'imageCount':
                    statImageCount = value;
                    break;

                case 'imageCountPerSec':
                    statImageCountPerSec = value;
                    break;

                case 'imageCountOk':
                    statImageCountOk = value;
                    break;

                case 'imageCountMax':
                    statImageCountMax = value;
                    break;

                case 'imageIndex':
                    statImageIndex = value;
                    break;

                case 'imageFormat':
                    statImageFormat = value;
                    break;

                case 'imageQuality':
                    statImageQuality = value;
                    break;

                case 'imageBase64':
                    statImageBase64Enabled = value;
                    break;
                
                case 'imageSize':
                    statImageSize = value;
                    break;
            }

	        statDiv.innerHTML =
                'AVERAGE LATENCY (ms): ' + statLatency + ', ' +
                'KEYBOARD BUFFER (ms): ' + statKeyboardBuffer + ', ' +
                'MOUSE BUFFER (ms): ' + statMouseBuffer + ' (MOVES ' + (config.getMouseMoveSamplingRate() > 0 ? 'SAMPLED ' + config.getMouseMoveSamplingRate() + '%' : 'NOT SAMPLED') + '), ' +
                'BANDWIDTH (KB/s): ' + statBandwidthUsage + '/' + statBandwidthSize + ' (' + (statBandwidthSize > 0 ? Math.round((statBandwidthUsage * 100) / statBandwidthSize) : 0) + '%), ' +
                'PERIODICAL FSU (s): ' + config.getPeriodicalFullscreenIntervalDelay() / 1000 + ', ' +
                'ADAPTIVE FSU (s): ' + config.getAdaptiveFullscreenTimeoutDelay() / 1000 + ', ' +
                'WEBSOCKET: ' + (statWebSocketEnabled ? 'ON' : 'OFF') + ', ' +
                'LONG-POLLING: ' + (statLongpollingEnabled ? 'ON (' + config.getLongPollingDuration() / 1000 + 's)' : 'OFF') + ', ' +
                'CANVAS: ' + (statCanvasEnabled ? 'ON' : 'OFF') + ', ' +
                'COUNT: ' + statImageCount + ' (' + statImageCountPerSec + '/s), OK: ' + statImageCountOk + ', MAX: ' + statImageCountMax + ', ' +
                'INDEX: ' + statImageIndex + ', ' +
                'FORMAT: ' + statImageFormat.toUpperCase() + ', ' +
                'QUALITY: ' + statImageQuality + '%, ' +
                'BASE64: ' + (statImageBase64Enabled ? 'ON' : 'OFF') + ', ' +
                'SIZE (KB): ' + statImageSize;
        }
        catch (exc)
        {
            this.showDebug('dialog showStat error: ' + exc.Message);
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
            alert('dialog showDebug error: ' + exc.Message);
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
            this.showDebug('dialog showMessage error: ' + exc.Message);
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
            this.showDebug('dialog hideMessage error: ' + exc.Message);
        }
    }

    /*************************************************************************************************************************************************************************************************/
    /*** Keyboard helper                                                                                                                                                                           ***/
    /*************************************************************************************************************************************************************************************************/

    var kbhDiv = null;
    var kbhText = '';
    var kbhTimeout = null;

    // display typed keyboard text (useful when latency is high, the user can see the result of its action immediately and is able to evaluate the latency)
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

                kbhTimeout = window.setTimeout(function() { doHideKeyboardHelper(); }, config.getKeyboardHelperDelay());
            }
        }
        catch (exc)
        {
            this.showDebug('dialog showKeyboardHelper error: ' + exc.Message);
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
            this.showDebug('dialog hideKeyboardHelper error: ' + exc.Message);
        }
    }
}