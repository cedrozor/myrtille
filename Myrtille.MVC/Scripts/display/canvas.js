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
/*** Canvas                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Canvas(config, dialog, display)
{
    // canvas object
    var canvasObject = null;
    this.getCanvasObject = function() { return canvasObject; };

    // canvas context
    var canvasContext = null;
    this.getCanvasContext = function() { return canvasContext; };

    this.init = function()
    {
        try
        {
            canvasObject = document.createElement('canvas');
            canvasContext = canvasObject == null ? null : canvasObject.getContext;
        
            if (canvasContext == null || !canvasContext)
            {
                //dialog.showDebug('canvas is not supported, using divs');
                canvasObject = null;
                config.setCanvasEnabled(false);
            }
            else
            {
                // set canvas properties (same size as browser and a tab index so it can be focused)
                canvasObject.width = display.getBrowserWidth();
                canvasObject.height = display.getBrowserHeight();
                canvasObject.setAttribute('tabindex', '0');

                display.getDisplayDiv().appendChild(canvasObject);

                //dialog.showDebug('using canvas, width: ' + canvasObject.width + ', height:' + canvasObject.height);

                // set canvas context properties
                canvasContext = canvasObject.getContext('2d');
                if (config.getImageDebugEnabled())
                {
                    canvasContext.lineWidth = 1;
                    canvasContext.strokeStyle = 'red';
                }
            }
        }
	    catch (exc)
	    {
		    dialog.showDebug('canvas init error: ' + exc.message);
            config.setCanvasEnabled(false);
	    }
    };

    this.addImage = function(idx, posX, posY, width, height, format, quality, base64Data, fullscreen)
    {
        try
        {
            var img = new Image();

            img.onload = function()
            {
                //dialog.showDebug('canvas image ' + idx + ' loaded');
                canvasContext.drawImage(img, parseInt(posX), parseInt(posY), parseInt(width), parseInt(height));
                if (config.getImageDebugEnabled())
                {
                    canvasContext.strokeRect(parseInt(posX), parseInt(posY), parseInt(width), parseInt(height));
                }
            };

            img.onabort = function()
            {
                dialog.showDebug('canvas image ' + idx + ' aborted');
            };

            img.onerror = function()
            {
                dialog.showDebug('canvas image ' + idx + ' error');
            };

            if (!config.getImageBase64Enabled() || base64Data == '')
            {
                if (config.getAdditionalLatency() > 0)
                {
                    window.setTimeout(function() { img.src = config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime(); }, Math.round(config.getAdditionalLatency() / 2));
                }
                else
                {
                    img.src = config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime();
                }
            }
            else
            {
                img.src = 'data:image/' + format + ';base64,' + base64Data;
            }
        }
        catch (exc)
        {
            dialog.showDebug('canvas addImage error: ' + exc.Message);
            throw exc;
        }
    };
}