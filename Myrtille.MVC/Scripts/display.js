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
/*** Display                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function Display(config, dialog)
{
    // display div
    var displayDiv = null;
    this.getDisplayDiv = function() { return displayDiv; }
    this.getHorizontalOffset = function() { return displayDiv.offsetLeft; };
    this.getVerticalOffset = function() { return displayDiv.offsetTop; };

    // canvas (HTML5)
    var canvas = null;
    this.getCanvas = function() { return canvas; };

    // divs (HTML4)
    var divs = null;

    this.init = function()
    {
        try
        {
            //dialog.showDebug('browser: ' + navigator.userAgent + ', width: ' + this.getBrowserWidth() + ', height: ' + this.getBrowserHeight());

            // use a div markup to render the display, instead of document body, for a better position handling
            displayDiv = document.getElementById('displayDiv');
            if (displayDiv == null)
            {
                alert('missing displayDiv element! can\'t render display');
                return;
            }

            // use canvas if enabled; support will be checked on init
            config.setCanvasEnabled(config.getCanvasEnabled() && !config.getCompatibilityMode());
            if (config.getCanvasEnabled())
            {
                canvas = new Canvas(config, dialog, this);
                canvas.init();
            }

            dialog.showStat('canvas', config.getCanvasEnabled());

            // if not using canvas, use divs
            if (!config.getCanvasEnabled())
            {
                divs = new Divs(config, dialog, this);
                divs.init();
            }

            // image encoding
            // PNG is lossless (best quality), less sized for text images but more sized for graphic ones. best suited for office applications
            // JPEG is lossy, allows automatic quality tweak depending on bandwidth availability, less sized for graphic images but more sized for text ones. best suited for imaging applications
            // PNG_JPEG will encode both PNG and JPEG and return the lowest sized format; pros: optimize quality and bandwidth usage, cons: slower and higher server CPU. best suited for mixed (text and imaging) applications
            // WEBP format may reduce the overall images size but at the expense of speed and server CPU; it's also not supported by all browsers (google tech, so mostly supported by chrome). it's mostly an experimental feature; fallback to JPEG if not supported
            if (config.getImageEncoding() == 'WEBP')
            {
                checkWebpSupport();
            }

            // base64
            // IE6/7: not supported
            // IE8: supported up to 32KB
            // IE9: supported in native mode; not supported in compatibility mode (use IE7 engine)
            // IE10+: supported
            // please note that, even if base64 data is disabled or not supported by the client, the server will always send them in order to display images size and compute bandwidth usage, and thus be able to tweak the images quality if the bandwidth gets too low
            // it also workaround a weird glitch in IE7 that prevents script execution if code length is too low (when script code is injected into the DOM through long-polling)
            config.setImageBase64Enabled(config.getImageBase64Enabled() && (!this.isIEBrowser() || this.getIEVersion() >= 8));
            dialog.showStat('imageBase64', config.getImageBase64Enabled());
           
            // image count per second; currently just an information but could be used for throttling display, if needed
            window.setInterval(function()
            {
                //dialog.showDebug('checking image count per second');
                dialog.showStat('imageCountPerSec', imgCountPerSec);
                imgCountPerSec = 0;
            },
            1000);

            // reasonable number of images to display when using divs
            dialog.showStat('imageCountOk', (config.getCanvasEnabled() ? 'N/A' : config.getImageCountOk()));

            // maximal number of images to display when using divs
            dialog.showStat('imageCountMax', (config.getCanvasEnabled() ? 'N/A' : config.getImageCountMax()));
        }
        catch (exc)
        {
            dialog.showDebug('display init error: ' + exc.message);
            throw exc;
        }
    };

    /*************************************************************************************************************************************************************************************************/
    /*** Browser                                                                                                                                                                                   ***/
    /*************************************************************************************************************************************************************************************************/

	this.getBrowserWidth = function()
	{
		if (self.innerWidth)
		{
		    return self.innerWidth;
		}
		else if (document.documentElement && document.documentElement.clientWidth)
		{
		    return document.documentElement.clientWidth;
		}
		else if (document.body)
		{
		    return document.body.clientWidth;
		}

		return 1024;
	};

	this.getBrowserHeight = function()
	{
        if (self.innerHeight)
        {
            return self.innerHeight;
        }
        else if (document.documentElement && document.documentElement.clientHeight)
        {
            return document.documentElement.clientHeight;
        }
        else if (document.body)
        {
            return document.body.clientHeight;
        }

        return 768;
    };

    this.isFirefoxBrowser = function()
    {
        return /Firefox/.test(navigator.userAgent);
    };

    this.isIEBrowser = function()
    {
        return /MSIE/.test(navigator.userAgent);
    };

    this.getIEVersion = function()
    {
        var version = -1;

        if (this.isIEBrowser())
        {
            var regExp = new RegExp("MSIE ([0-9]{1,}[\.0-9]{0,})");
            if (regExp.exec(navigator.userAgent) != null)
            {
                version = parseFloat(RegExp.$1);
            }
	        //dialog.showDebug('IE version: ' + version);
        }
        else
        {
            //dialog.showDebug('browser is not IE');
        }

        return version;
    };

    function checkWebpSupport()
    {
        try
        {
            //dialog.showDebug('checking webp image support: ' + config.getHttpServerUrl() + 'webp/test.webp');
            var img = new Image();
            img.onload = function() { /*dialog.showDebug('webp image loaded');*/if (img.height <= 0 || img.width <= 0) config.setImageEncoding('JPEG'); };
            img.onerror = function() { /*dialog.showDebug('webp image error');*/config.setImageEncoding('JPEG'); };
            img.src = config.getHttpServerUrl() + 'img/test.webp';
        }
	    catch (exc)
	    {
		    dialog.showDebug('display checkWebpSupport error: ' + exc.message);
		    config.setImageEncoding('JPEG');
	    }
    }

    /*************************************************************************************************************************************************************************************************/
    /*** Cursor                                                                                                                                                                                    ***/
    /*************************************************************************************************************************************************************************************************/

    this.setMouseCursor = function(idx, base64Data, xHotSpot, yHotSpot)
    {
        try
        {
            //dialog.showDebug('updating mouse cursor, xHotSpot: ' + xHotSpot + ', yHotSpot: ' + yHotSpot);

            // IE have issues with mouse cursors:
            // - it doesn't supports PNG (neither base64 or binary data), only .cur, .ico or .ani
            // - it's not possible to specify an hotspot using CSS (but the .cur format handles it...)
            // - the cursor blinks when it changes, and stays invisible as long as the user doesn't move the mouse (!)
            if (this.isIEBrowser())
                return;

            if (!config.getImageBase64Enabled() || base64Data == '')
            {
                if (config.getAdditionalLatency() > 0)
                {
                    window.setTimeout(function() { document.body.style.cursor = 'url(' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + ') ' + xHotSpot + ' ' + yHotSpot + ', auto'; }, Math.round(config.getAdditionalLatency() / 2));
                }
                else
                {
                    document.body.style.cursor = 'url(' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + ') ' + xHotSpot + ' ' + yHotSpot + ', auto';
                }
            }
            else
            {
                document.body.style.cursor = 'url(\'data:image/png;base64,' + base64Data + '\') ' + xHotSpot + ' ' + yHotSpot + ', auto';
            }
        }
	    catch (exc)
	    {
	        dialog.showDebug('display setMouseCursor error: ' + exc.message);
	        throw exc;
	    }
    };

    /*************************************************************************************************************************************************************************************************/
    /*** Images                                                                                                                                                                                    ***/
    /*************************************************************************************************************************************************************************************************/

    // number of displayed images
    var imgCount = 0;
    this.getImgCount = function() { return imgCount; };

    // number of displayed images per second
    var imgCountPerSec = 0;

    // last displayed image
    var imgIdx = 0;
    this.getImgIdx = function() { return imgIdx; };

    this.addImage = function(idx, posX, posY, width, height, format, quality, base64Data, fullscreen)
    {
        try
        {
            //dialog.showDebug('new image, idx: ' + idx + ', posX: ' + posX + ', posY: ' + posY + ', width: ' + width + ', height: ' + height + ', format: ' + format + ', quality: ' + quality + ', base64Data: ' + base64Data + ', fullscreen: ' + fullscreen);

            // mouse cursor image
            if (format == 'cur')
            {
                this.setMouseCursor(idx, base64Data, posX, posY);
            }
            // region or fullscreen image
            else
            {
                // canvas
                if (config.getCanvasEnabled())
                {
                    canvas.addImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen);
                }
                // divs
                else
                {
                    divs.addImage(idx, posX, posY, width, height, format, quality, base64Data, fullscreen);
                }

                // reset or increment image counter
                if (fullscreen)
                {
                    //dialog.showDebug('fullscreen');
                    imgCount = 1;
                }
                else
                {
                    //dialog.showDebug('region');
                    imgCount++;
                }

                imgCountPerSec++;
            }

            dialog.showStat('imageCount', imgCount);
            dialog.showStat('imageIndex', idx);
            dialog.showStat('imageFormat', format);
            dialog.showStat('imageQuality', quality);
            dialog.showStat('imageBase64', config.getImageBase64Enabled() && base64Data != '');
            dialog.showStat('imageSize', (base64Data != '' ? Math.ceil(((base64Data.length * 3) / 4) / 1024) : 'N/A'));

            // update the last processed image index
            imgIdx = idx;
	    }
	    catch (exc)
	    {
		    dialog.showDebug('display addImage error: ' + exc.Message);
	    }
    };
}