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
/*** XmlHttp                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function XmlHttp(config, dialog, display, network)
{
    var xhr = null;
    var xhrStartTime = null;
    var xhrTimeout = null;
    var xhrTimeoutElapsed = false;

    var fullscreenPending = false;

    this.init = function()
    {
        try
        {
            createXhr();
            cleanup(false);
            //dialog.showDebug('xhr supported');
        }
        catch (exc)
        {
            dialog.showDebug('xmlhttp init error: ' + exc.Message);
            xhr = null;
            throw exc;
        }
    };

    function createXhr()
    {
        // IE
        if (window.ActiveXObject)
	    {
		    try
		    {
                xhr = new XMLHttpRequest();
		    }
		    catch (exc1)
		    {
                var MSXMLXMLHTTPPROGIDS = new Array('MSXML2.XMLHTTP.5.0', 'MSXML2.XMLHTTP.4.0', 'MSXML2.XMLHTTP.3.0', 'MSXML2.XMLHTTP', 'Microsoft.XMLHTTP');

                var ok = false;
                for (var i = 0; i < MSXMLXMLHTTPPROGIDS.length && !ok; i++)
                {
                    try
                    {
                        xhr = new ActiveXObject(MSXMLXMLHTTPPROGIDS[i]);
                        ok = true;
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
			    xhr = new XMLHttpRequest();
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
                // give priority to fullscreen update requests (by design, unbuffered commands for immediate consideration)
                if (data == null)
                {
                    //dialog.showDebug('xhr fullscreen priority');
                    cleanup(false);
                }
                else
                {
                    //dialog.showDebug('xhr is busy');
                    return;
                }
            }

            createXhr();

            if (xhr == null)
            {
                //dialog.showDebug('failed to create xhr');
                return;
            }

            xhrStartTime = startTime;

            xhr.open('GET', config.getHttpServerUrl() + 'SendInputs.aspx' +
                '?data=' + (data == null ? '' : data) +
                '&fsu=' + (data == null ? 1 : 0) +
                '&imgIdx=' + display.getImgIdx() +
                '&imgEncoding=' + config.getImageEncoding() +
                '&imgQuality=' + config.getImageQuality() +
                '&imgReturn=' + (!config.getWebSocketEnabled() && !config.getLongPollingEnabled() ? 1 : 0) +
                '&bandwidthRatio=' + (network.getBandwidthUsageKBSec() != null && network.getBandwidthSizeKBSec() != null && network.getBandwidthSizeKBSec() > 0 ? Math.round((network.getBandwidthUsageKBSec() * 100) / network.getBandwidthSizeKBSec()) : 0) +
                '&noCache=' + startTime);

            xhrTimeout = window.setTimeout(function()
            {
                //dialog.showDebug('xhr timeout');
                cleanup(true);
            },
            config.getXmlHttpTimeoutDelay());

            xhr.onreadystatechange = function() { callback(); };

            xhr.send(null);

            if (buffer != null)
            {
                buffer.setClearBuffer(true);
            }
        }
        catch (exc)
        {
            dialog.showDebug('xmlhttp send error: ' + exc.Message);
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
		    dialog.showDebug('xmlhttp callback error: ' + exc.Message);
            cleanup(false);
	    }
    }

    function processResponse(xhrResponseText)
    {
        try
        {
            // update the average "latency"
            network.updateLatency(xhrStartTime);

            //dialog.showDebug('xhr response:' + xhrResponseText);

            if (xhrResponseText != '')
            {
                // session disconnect
                if (xhrResponseText == 'disconnected')
                {
                    // the remote session is disconnected
                    window.location.href = window.location.href.replace('connect', 'disconnect');
                }
                // new image
                else
                {
                    var parts = xhrResponseText.split(',');
                        
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
                }
            }
            
            // release the xhr
            cleanup(false);

            // a previous xhr failed to return within a proper time fashion; now that one had returned successfully, lift the error status
            if (xhrTimeoutElapsed)
            {
                //dialog.showDebug('xhr ok');
                xhrTimeoutElapsed = false;
                dialog.hideMessage();
            }

            // if using divs and count reached a reasonable number, request a fullscreen update
            if (!config.getCanvasEnabled() && display.getImgCount() >= config.getImageCountOk() && !fullscreenPending)
            {
                //dialog.showDebug('reached a reasonable number of divs, requesting a fullscreen update');
                fullscreenPending = true;
                network.send(null);
            }
	    }
	    catch (exc)
	    {
		    dialog.showDebug('xmlhttp processResponse error: ' + exc.Message);
            cleanup(false);
	    }
    }

    function cleanup(timeoutElapsed)
    {
        try
        {
            if (timeoutElapsed)
            {
                dialog.showMessage('xhr timeout (' + config.getXmlHttpTimeoutDelay() + ' ms). Please check your network connection', 0);
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
		    dialog.showDebug('xmlhttp cleanup error: ' + exc.Message);
	    }

        xhr = null;
    }
}