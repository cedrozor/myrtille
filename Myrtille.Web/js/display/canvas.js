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
                // set canvas properties (same size as browser if scaling display or original (unscaled) size otherwise)
                canvasObject.width = config.getScaleDisplay() ? display.getBrowserWidth() - display.getHorizontalOffset() : config.getDisplayWidth();
                canvasObject.height = config.getScaleDisplay() ? display.getBrowserHeight() - display.getVerticalOffset() : config.getDisplayHeight();

                // set a tab index so the canvas can be focused
                canvasObject.setAttribute('tabindex', 0);

                // disable drag & drop
                canvasObject.setAttribute('draggable', false);

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
	        dialog.showDebug('canvas init error: ' + exc.message + ', falling back to divs');
		    config.setDisplayMode(config.getDisplayModeEnum().DIV);
		    return;
	    }

        // blob support check
        if (config.getImageBlobEnabled())
        {
            try
            {
                var blob = new Blob();
            }
	        catch (exc)
	        {
		        dialog.showDebug('blob support check failed (' + exc.message + '), using base64');
		        config.setImageBlobEnabled(false);
	        }
        }
    };

    this.addImage = function(idx, posX, posY, width, height, format, quality, fullscreen, data)
    {
        try
        {
            var img = new Image();
            var url = null;

            img.onload = function()
            {
                //dialog.showDebug('canvas image ' + idx + ' loaded');
                canvasContext.drawImage(this, parseInt(posX), parseInt(posY), parseInt(width), parseInt(height));

                if (config.getImageDebugEnabled())
                {
                    canvasContext.strokeRect(parseInt(posX), parseInt(posY), parseInt(width), parseInt(height));
                }

                if (config.getImageBlobEnabled() && url != null)
                {
                    //dialog.showDebug('revoking url: ' + url);
                    URL.revokeObjectURL(url);
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

            if (config.getImageMode() != config.getImageModeEnum().BINARY)
            {
                if (config.getImageMode() != config.getImageModeEnum().BASE64 || data == '')
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
                    img.src = 'data:image/' + format + ';base64,' + data;
                }
            }
            else
            {
                // it's possible to draw binary images directly on the canvas (using createImageData and putImageData)
                // however, it only works with raw RGBA data, not with already compressed PNG or JPEG images

                // after sending the image in raw binary, having to convert it to base64 to draw it on canvas feels quite weird
                // another option is to make a blob and create an url from it (cached locally)
                // base64 is more backward compatible while blob is a newer javascript construct
                // after some tests, both solutions behave similarly... but the blob url is cached on disk whereas the base64 data is cached in memory (thus blob might be slower)

                if (!config.getImageBlobEnabled())
                {
                    url = 'data:image/' + format + ';base64,' + display.bytesToBase64(data);
                }
                else
                {
                    url = URL.createObjectURL(new Blob([data], { type: 'image/' + format }));
                }

                //dialog.showDebug('image url: ' + url);
                img.src = url;
            }
        }
        catch (exc)
        {
            dialog.showDebug('canvas addImage error: ' + exc.message);
            throw exc;
        }
    };
}