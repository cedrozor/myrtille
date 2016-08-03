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
/*** User                                                                                                                                                                                          ***/
/*****************************************************************************************************************************************************************************************************/

function User(config, dialog, display, network)
{
    // adaptive fullscreen update
    var adaptiveFullscreenTimeout = null;

    // keyboard
    var keyboard = null;

    // mouse
    var mouse = null;

    // touchscreen
    var touchscreen = null;

    this.init = function()
    {
        try
        {
            keyboard = new Keyboard(config, dialog, display, network, this);
            keyboard.init();

            mouse = new Mouse(config, dialog, display, network, this);
            mouse.init();

            // even if possible to detect if the device has touchscreen capabilities, it would only be an assumption; so, implementing it by default, alongside with mouse...
            // http://www.stucox.com/blog/you-cant-detect-a-touchscreen/
            touchscreen = new Touchscreen(config, dialog, display, network, this);
            touchscreen.init();
        }
        catch (exc)
        {
            dialog.showDebug('user init error: ' + exc.message);
            throw exc;
        }
    };

    this.triggerActivity = function()
    {
        try
        {
            //dialog.showDebug('user activity detected, sliding adaptive fullscreen update');

            if (adaptiveFullscreenTimeout != null)
            {
                window.clearTimeout(adaptiveFullscreenTimeout);
                adaptiveFullscreenTimeout = null;
            }

            adaptiveFullscreenTimeout = window.setTimeout(function()
            {
                //dialog.showDebug('adaptive fullscreen update');
                network.send(null);
            },
            config.getAdaptiveFullscreenTimeoutDelay());
        }
        catch (exc)
        {
            dialog.showDebug('user triggerActivity error: ' + exc.message);
        }
    };

    this.cancelEvent = function(e)
    {
        // prevent default action
        if (e.preventDefault) e.preventDefault();   // DOM Level 2
        else e.returnValue = false;                 // IE

        // stop event propagation
        if (e.stopPropagation) e.stopPropagation(); // DOM Level 2
        else e.cancelBubble = true;                 // IE
    };
}