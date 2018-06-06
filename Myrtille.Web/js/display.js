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
/*** Display                                                                                                                                                                                       ***/
/*****************************************************************************************************************************************************************************************************/

function Display(config, dialog)
{
    // display div
    var displayDiv = document.getElementById('displayDiv');
    this.getDisplayDiv = function() { return displayDiv; }
    this.getHorizontalOffset = function() { return displayDiv.offsetLeft; };
    this.getVerticalOffset = function() { return displayDiv.offsetTop; };

    // canvas (HTML5)
    var canvas = null;
    this.getCanvas = function() { return canvas; };

    // divs (HTML4)
    var divs = null;
    this.getDivs = function () { return divs; };

    this.init = function()
    {
        try
        {
            //dialog.showDebug('browser: ' + navigator.userAgent + ', width: ' + this.getBrowserWidth() + ', height: ' + this.getBrowserHeight());

            // use a div markup to render the display, instead of document body, for a better position handling
            if (displayDiv == null)
            {
                alert('missing displayDiv element! can\'t render display');
                return;
            }

            if (config.getHostType() == 'RDP')
            {
                /* display mode

                DIV
                HTML4 compatibility mode; images are loaded as divs background images

                CANVAS
                HTML5 mode; fallback to divs if not supported

                SVG
                not implemented but could be an option (see http://stackoverflow.com/questions/5882716/html5-canvas-vs-svg-vs-div)

                AUTO (default)
                use canvas if possible; divs otherwise

                */

                switch (config.getDisplayMode())
                {
                    case config.getDisplayModeEnum().DIV:
                        break;

                    case config.getDisplayModeEnum().CANVAS:
                        if (config.getCompatibilityMode())
                        {
                            config.setDisplayMode(config.getDisplayModeEnum().DIV);
                        }
                        break;
                   
                    default:
                        config.setDisplayMode(config.getCompatibilityMode() ? config.getDisplayModeEnum().DIV : config.getDisplayModeEnum().CANVAS);
                }

                // canvas support will be checked on init
                if (config.getDisplayMode() == config.getDisplayModeEnum().CANVAS)
                {
                    canvas = new Canvas(config, dialog, this);
                    canvas.init();
                }

                // if not using canvas, use divs
                if (config.getDisplayMode() == config.getDisplayModeEnum().DIV)
                {
                    divs = new Divs(config, dialog, this);
                    divs.init();
                }

                dialog.showStat(dialog.getShowStatEnum().DISPLAY_MODE, config.getDisplayMode());

                /* image encoding

                PNG
                lossless (best quality), less sized for text images but more sized for graphic ones. best suited for office applications
            
                JPEG
                lossy, allows automatic quality tweak depending on bandwidth availability, less sized for graphic images but more sized for text ones. best suited for imaging applications
            
                WEBP
                may reduce the overall images size but at the expense of speed and server CPU; it's also not supported by all browsers (google tech, so mostly supported by chrome). it's mostly an experimental feature; fallback to JPEG if not supported
            
                AUTO (default)
                will encode both PNG and JPEG and return the lowest sized format; pros: optimize quality and bandwidth usage, cons: slower and higher server CPU. best suited for mixed (text and imaging) applications

                */

                if (config.getImageEncoding() == config.getImageEncodingEnum().WEBP)
                {
                    checkWebpSupport();
                }

                // image count per second; currently just an information but could be used for throttling display, if needed
                window.setInterval(function()
                {
                    //dialog.showDebug('checking image count per second');
                    dialog.showStat(dialog.getShowStatEnum().IMAGE_COUNT_PER_SEC, imgCountPerSec);
                    imgCountPerSec = 0;
                },
                1000);

                // reasonable number of images to display when using divs
                dialog.showStat(dialog.getShowStatEnum().IMAGE_COUNT_OK, (config.getDisplayMode() == config.getDisplayModeEnum().CANVAS ? 'N/A' : config.getImageCountOk()));

                // maximal number of images to display when using divs
                dialog.showStat(dialog.getShowStatEnum().IMAGE_COUNT_MAX, (config.getDisplayMode() == config.getDisplayModeEnum().CANVAS ? 'N/A' : config.getImageCountMax()));
            }
            else if (config.getHostType() == 'SSH')
            {
                // ssh uses xtermjs within a div, load div regardless of of browser compatibility

                divs = new TerminalDivs(config, dialog, this);
                divs.init();
            }
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
        var ua = navigator.userAgent;

        // IE 10 or older
        var msie = ua.indexOf('MSIE ');
        if (msie > 0)
        {
            return true;
        }

        // IE 11
        var trident = ua.indexOf('Trident/');
        if (trident > 0)
        {
            return true;
        }

        // Edge (IE 12+)
        var edge = ua.indexOf('Edge/');
        if (edge > 0)
        {
            return true;
        }

        return false;
    };

    this.getIEVersion = function()
    {
        var ua = navigator.userAgent;

        // IE 10 or older
        var msie = ua.indexOf('MSIE ');
        if (msie > 0)
        {
            return parseInt(ua.substring(msie + 5, ua.indexOf('.', msie)), 10);
        }

        // IE 11
        var trident = ua.indexOf('Trident/');
        if (trident > 0)
        {
            var rv = ua.indexOf('rv:');
            return parseInt(ua.substring(rv + 3, ua.indexOf('.', rv)), 10);
        }

        // Edge (IE 12+)
        var edge = ua.indexOf('Edge/');
        if (edge > 0)
        {
            return parseInt(ua.substring(edge + 5, ua.indexOf('.', edge)), 10);
        }

        return 0;
    };

    this.isBase64Available = function()
    {
        return !this.isIEBrowser() || this.getIEVersion() >= 8;
    };

    function checkWebpSupport()
    {
        try
        {
            //dialog.showDebug('checking webp image support: ' + config.getHttpServerUrl() + 'webp/test.webp');
            var img = new Image();
            img.onload = function() { /*dialog.showDebug('webp image loaded');*/if (img.height <= 0 || img.width <= 0) config.setImageEncoding(config.getImageEncodingEnum().AUTO); };
            img.onerror = function() { /*dialog.showDebug('webp image error');*/config.setImageEncoding(config.getImageEncodingEnum().AUTO); };
            img.src = config.getHttpServerUrl() + 'img/test.webp';
        }
	    catch (exc)
	    {
		    dialog.showDebug('display checkWebpSupport error: ' + exc.message);
		    config.setImageEncoding(config.getImageEncodingEnum().AUTO);
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

            if (this.isIEBrowser())
            {
                //dialog.showDebug('IE browser detected, retrieving mouse cursor (.cur format, with hotspot)');

                if (config.getAdditionalLatency() > 0)
                {
                    window.setTimeout(function() { document.body.style.cursor = 'url(\'' + config.getHttpServerUrl() + 'GetCursor.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\'), auto'; }, Math.round(config.getAdditionalLatency() / 2));
                }
                else
                {
                    document.body.style.cursor = 'url(\'' + config.getHttpServerUrl() + 'GetCursor.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\'), auto';
                }
            }
            else
            {
                if (config.getImageMode() == config.getImageModeEnum().ROUNDTRIP || base64Data == '')
                {
                    if (config.getAdditionalLatency() > 0)
                    {
                        window.setTimeout(function() { document.body.style.cursor = 'url(\'' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\') ' + xHotSpot + ' ' + yHotSpot + ', auto'; }, Math.round(config.getAdditionalLatency() / 2));
                    }
                    else
                    {
                        document.body.style.cursor = 'url(\'' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\') ' + xHotSpot + ' ' + yHotSpot + ', auto';
                    }
                }
                else
                {
                    document.body.style.cursor = 'url(\'data:image/png;base64,' + base64Data + '\') ' + xHotSpot + ' ' + yHotSpot + ', auto';
                }
            }
        }
	    catch (exc)
	    {
	        dialog.showDebug('display setMouseCursor error: ' + exc.message);
	        throw exc;
	    }
    }

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

    this.addImage = function(idx, posX, posY, width, height, format, quality, fullscreen, data)
    {
        try
        {
            //dialog.showDebug('new image, idx: ' + idx + ', posX: ' + posX + ', posY: ' + posY + ', width: ' + width + ', height: ' + height + ', format: ' + format + ', quality: ' + quality + ', fullscreen: ' + fullscreen + ', data: ' + data);

            // mouse cursor image
            if (format == 'cur')
            {
                this.setMouseCursor(idx, config.getImageMode() != config.getImageModeEnum().BINARY ? data : this.bytesToBase64(data), posX, posY);
            }
            // region or fullscreen image
            else
            {
                // canvas
                if (config.getDisplayMode() == config.getDisplayModeEnum().CANVAS)
                {
                    canvas.addImage(idx, posX, posY, width, height, format, quality, fullscreen, data);
                }
                // divs
                else
                {
                    divs.addImage(idx, posX, posY, width, height, format, quality, fullscreen, config.getImageMode() != config.getImageModeEnum().BINARY ? data : this.bytesToBase64(data));
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

            dialog.showStat(dialog.getShowStatEnum().IMAGE_COUNT, imgCount);
            dialog.showStat(dialog.getShowStatEnum().IMAGE_INDEX, idx);
            dialog.showStat(dialog.getShowStatEnum().IMAGE_FORMAT, format);
            dialog.showStat(dialog.getShowStatEnum().IMAGE_QUALITY, quality);
            dialog.showStat(dialog.getShowStatEnum().IMAGE_QUANTITY, config.getImageQuantity());
            dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            dialog.showStat(dialog.getShowStatEnum().IMAGE_SIZE, (config.getImageMode() != config.getImageModeEnum().BINARY ? (config.getImageMode() != config.getImageModeEnum().BASE64 || data == '' ? 'N/A' : Math.ceil(data.length / 1024)) : Math.ceil(data.byteLength / 1024)));

            // update the last processed image
            imgIdx = idx;
	    }
	    catch (exc)
	    {
		    dialog.showDebug('display addImage error: ' + exc.message);
	    }
    };

    this.getFormatText = function(format)
    {
        switch (format)
        {
            case 0:
                return 'cur';
            case 1:
                return 'png';
            case 2:
                return 'jpeg';
            case 3:
                return 'webp';
        }
    };

    this.bytesToBase64 = function(bytes)
    {
        var str = '';
        var arr = new Uint8Array(bytes);

        for (var i = 0; i < bytes.byteLength; i++)
        {
            str += String.fromCharCode(arr[i]);
        }

        return window.btoa(str);
    };
}