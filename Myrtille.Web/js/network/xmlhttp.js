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
/*** XmlHttp                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function XmlHttp(base, config, dialog, display, network)
{
    var xhr = null;
    var xhrStartTime = null;
    var xhrTimeout = null;
    var xhrTimeoutElapsed = false;

    this.init = function()
    {
        try
        {
            xhr = this.createXhr();
            cleanup(false);
            //dialog.showDebug('xhr supported');
        }
        catch (exc)
        {
            alert('xmlhttp init error: ' + exc.message + ', please ensure it\'s enabled into the browser options');
            xhr = null;
            throw exc;
        }
    };

    this.createXhr = function()
    {
        // IE
        if (window.ActiveXObject)
	    {
		    try
		    {
                return new XMLHttpRequest();
		    }
		    catch (exc1)
		    {
                var MSXMLXMLHTTPPROGIDS = new Array('MSXML2.XMLHTTP.5.0', 'MSXML2.XMLHTTP.4.0', 'MSXML2.XMLHTTP.3.0', 'MSXML2.XMLHTTP', 'Microsoft.XMLHTTP');

                var ok = false;
                for (var i = 0; i < MSXMLXMLHTTPPROGIDS.length && !ok; i++)
                {
                    try
                    {
                        return new ActiveXObject(MSXMLXMLHTTPPROGIDS[i]);
                    }
                    catch (exc2)
                    {}
                }
                
                if (!ok)
                {
                    dialog.showDebug('xmlhttp create error: XMLHttpRequest ActiveX not supported');
                    throw exc1;
                }
            }
	    }
        // others
	    else if (window.XMLHttpRequest)
	    {
		    try
		    {
		        return new XMLHttpRequest();
		    }
            catch (exc3)
		    {
		        dialog.showDebug('xmlhttp create error: ' + exc3.Message);
		        throw exc3;
		    }
	    }
    }

    this.send = function(data, startTime)
    {
        try
        {
            // in case of xhr issue, the event data is not sent
            // if that occurs and a buffer is used (strongly advised when using xhr!), the buffer must not be cleared... the event data can still be sent on its next flush tick!
            // this can't be done if no buffer is used (the unsent event data is lost...)
            var buffer = network.getBuffer();
            if (buffer != null)
            {
                buffer.setClearBuffer(false);
            }

            if (xhr != null)
            {
                // give priority to fullscreen update and browser pulse requests (by design, unbuffered command for immediate consideration)
                // also give priority to mouse clicks because the remote session may become unstable if it does not receive a mouse button release following a mouse button click
                if (data != null && (data.indexOf(base.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text) != -1 ||
                    data.indexOf(base.getCommandEnum().SEND_BROWSER_PULSE.text) != -1 ||
                    data.indexOf(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text) != -1 ||
                    data.indexOf(base.getCommandEnum().SEND_MOUSE_MIDDLE_BUTTON.text) != -1 ||
                    data.indexOf(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text) != -1 ||
                    data.indexOf(base.getCommandEnum().CLOSE_CLIENT.text) != -1))
                {
                    //dialog.showDebug('xhr priority');
                    cleanup(false);
                }
                else
                {
                    //dialog.showDebug('xhr is busy');
                    return;
                }
            }

            xhr = this.createXhr();

            if (xhr == null)
            {
                //dialog.showDebug('failed to create xhr');
                return;
            }

            xhrStartTime = startTime;

            xhr.open('GET', config.getHttpServerUrl() + 'SendInputs.aspx' +
                '?data=' + (data == null ? '' : encodeURIComponent(data)) +
                '&imgIdx=' + display.getImgIdx() +
                '&latency=' + network.getRoundtripDurationAvg() +
                '&imgReturn=' + (config.getNetworkMode() == config.getNetworkModeEnum().XHR ? 1 : 0) +
                '&noCache=' + startTime);

            xhrTimeout = window.setTimeout(function()
            {
                //dialog.showDebug('xhr timeout');
                cleanup(true);
            },
            config.getXmlHttpTimeout());

            xhr.onreadystatechange = function() { callback(); };

            xhr.send(null);

            if (buffer != null)
            {
                buffer.setClearBuffer(true);
            }
        }
        catch (exc)
        {
            dialog.showDebug('xmlhttp send error: ' + exc.message);
            cleanup(false);
        }
    };

    function callback()
    {
	    try
	    {
		    if (xhr.readyState == 4)
		    {
			    if (xhrTimeout != null)
			    {
			        window.clearTimeout(xhrTimeout);
				    xhrTimeout = null;
			    }

                if (xhr.status === 200 || xhr.statusKeys == 'OK')
                {
                    xhr.onreadystatechange = function() {};
                    
                    //dialog.showDebug('xmlhttp callback success');

                    if (config.getAdditionalLatency() > 0)
			        {
			            window.setTimeout(function() { processResponse(xhr.responseText); }, Math.round(config.getAdditionalLatency() / 2));
			        }
			        else
			        {
                        processResponse(xhr.responseText);
			        }
                }
			    else
			    {
                    //dialog.showDebug('xmlhttp callback error, status: ' + xhr.status + ', statusKeys: ' + xhr.statusKeys);
                    cleanup(false);
			    }
		    }
	    }
	    catch (exc)
	    {
		    dialog.showDebug('xmlhttp callback error: ' + exc.message);
            cleanup(false);
	    }
    }

    function processResponse(text)
    {
        try
        {
            //dialog.showDebug('xhr response:' + text);

            // update the average "latency"
            network.updateLatency(xhrStartTime);

            // release the xhr
            cleanup(false);

            // a previous xhr failed to return within a proper time fashion; now that one had returned successfully, lift the error status
            if (xhrTimeoutElapsed)
            {
                //dialog.showDebug('xhr ok');
                xhrTimeoutElapsed = false;
                //dialog.hideMessage();
            }

            if (text != null && text != '')
            {
                var message = true;

                if (config.getHostType() == config.getHostTypeEnum().RDP)
                {
                    try
                    {
                        // messages are serialized in JSON, unlike images; check a valid JSON string
                        JSON.parse(text);
                        //dialog.showDebug('message data: ' + text);
                    }
                    catch (exc)
                    {
                        message = false;
                        //dialog.showDebug('image data: ' + text);
                    }
                }
                else
                {
                    //dialog.showDebug('terminal data: ' + text);
                }

                // message
                if (message)
                {
                    processMessage(text);
                }
                // image
                else
                {
                    var imgInfo, idx, posX, posY, width, height, format, quality, fullscreen, imgData;

                    imgInfo = text.split(',');

                    idx = parseInt(imgInfo[0]);
                    posX = parseInt(imgInfo[1]);
                    posY = parseInt(imgInfo[2]);
                    width = parseInt(imgInfo[3]);
                    height = parseInt(imgInfo[4]);
                    format = imgInfo[5];
                    quality = parseInt(imgInfo[6]);
                    fullscreen = imgInfo[7] == 'true';
                    imgData = imgInfo[8];

                    processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);
                }
            }
	    }
	    catch (exc)
	    {
		    dialog.showDebug('xmlhttp processResponse error: ' + exc.message);
            cleanup(false);
	    }
    }

    function cleanup(timeoutElapsed)
    {
        try
        {
            if (timeoutElapsed)
            {
                //dialog.showMessage('xhr timeout (' + config.getXmlHttpTimeout() + ' ms). Please check your network connection', 0);
                xhrTimeoutElapsed = true;
            }

		    if (xhrTimeout != null)
		    {
			    window.clearTimeout(xhrTimeout);
			    xhrTimeout = null;
		    }

            if (xhr != null)
            {
                if (xhr.readyState == 1 || xhr.readyState == 2 || xhr.readyState == 3)
                {
                    //dialog.showDebug('aborting xhr');
                    xhr.abort();
                }
            }
            
            delete(xhr);
        }
	    catch (exc)
	    {
		    dialog.showDebug('xmlhttp cleanup error: ' + exc.message);
	    }

        xhr = null;
    }
}