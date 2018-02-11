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
/*** Divs                                                                                                                                                                                          ***/
/*****************************************************************************************************************************************************************************************************/

function Divs(config, dialog, display)
{
    // images/divs mapping
    var imgDivs = null;

    this.init = function()
    {
        try
        {
            imgDivs = new Array();
        }
        catch (exc)
        {
            dialog.showDebug('divs init error: ' + exc.message);
            throw exc;
        }
    };

    this.addImage = function(idx, posX, posY, width, height, format, quality, fullscreen, base64Data)
    {
        try
        {
            var div = document.createElement('div');
	        div.id = 'img' + idx;
            div.className = 'imageDiv';
	        div.style.left = parseInt(posX) + 'px';
	        div.style.top = parseInt(posY) + 'px';
	        div.style.width = width + 'px';
	        div.style.height = height + 'px';

            if (config.getImageDebugEnabled())
            {
    	        div.style.border = 'red 1px solid';
    	    }

            // disable drag & drop
            div.setAttribute('draggable', false);

            // IE8 supports base64 data up to 32KB; above, retrieve the update through a standard roundtrip
            if (config.getImageMode() == config.getImageModeEnum().ROUNDTRIP || base64Data == '' || (display.isIEBrowser() && display.getIEVersion() == 8 && base64Data.length >= 32768))
            {
                if (config.getAdditionalLatency() > 0)
                {
                    window.setTimeout(function() { div.style.backgroundImage = 'url(\'' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\')'; }, Math.round(config.getAdditionalLatency() / 2));
                }
                else
                {
                    div.style.backgroundImage = 'url(\'' + config.getHttpServerUrl() + 'GetUpdate.aspx?imgIdx=' + idx + '&noCache=' + new Date().getTime() + '\')';
                }
            }
            else
            {
                div.style.backgroundImage = 'url(\'data:image/' + format + ';base64,' + base64Data + '\')';
            }

            display.getDisplayDiv().appendChild(div);

            // a fullscreen update covers all existing images, which can safely be removed
            if (fullscreen)
            {
                var imgDivsToRemove = imgDivs.slice();

                // to avoid having a blank "flash" by removing all divs at once, the cleanup is done with a slight delay so that the fullscreen update have time to be displayed
                window.setTimeout(function()
                {
                    for (var i = 0; i < imgDivsToRemove.length; i++)
                    {
                        removeImage(imgDivsToRemove[i]);
                    }
                },
                2000);

                imgDivs = [];
            }
            // if the maximal number of images is reached, refresh the screen (force clean the DOM)
            else if (imgDivs.length >= config.getImageCountMax())
            {
                // TODO? a cleaner way to proceed would be to search (using a sweep line or range tree algorithm, for example) for fully overlapped images and remove them dynamically (for each new image) or on a timed basis
                // problem is, after some tests, it proven too much cpu intensive for the browser (ram issue also, using a screen buffer) and introduces some lag which adds to the lag induced by the high divs number...
                // a server side implementation would be better but slow down the gateway (which is meant to have the less processing time possible) and be necessary only for HTML4 browsers, as canvas doesn't have this issue of being overloaded...
                // also, a fullscreen update (which allows to remove all existing divs safely as it covers them all) is already requested when a reasonable number of divs is reached... so, let's keep it simple...
                window.location.href = window.location.href;
            }

            imgDivs.push(idx);
	    }
	    catch (exc)
	    {
	        dialog.showDebug('divs addImage error: ' + exc.message);
	        throw exc;
	    }
    };

    function removeImage(idx)
    {
        try
        {
            var div = document.getElementById('img' + idx);
            if (div != null)
            {
                display.getDisplayDiv().removeChild(div);
                div = null;
                //dialog.showDebug('removed image ' + idx);
            }
        }
	    catch (exc)
	    {
		    dialog.showDebug('divs removeImage error: ' + exc.message);
	    }
    }
}