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
/*** Touchscreen                                                                                                                                                                                   ***/
/*****************************************************************************************************************************************************************************************************/

function Touchscreen(config, dialog, display, network, user)
{
    this.init = function()
    {
        try
        {
            user.addListener('touchmove', function(e) { touchMove(e); });
            user.addListener('touchstart', function(e) { touchTap(e, 1); });
            user.addListener('touchend', function(e) { touchTap(e, 0); });
        }
        catch (exc)
        {
            dialog.showDebug('touchscreen init error: ' + exc.message);
            throw exc;
        }
    };

    function processEvent(e)
    {
        if (e == null)
            e = window.event;
        
        if (e == null)
            return false;

        if (!setTouchPosition(e))
            return false;
        
        return true;
    }

    function setTouchPosition(e)
    {
        var scrollLeft = (document.documentElement.scrollLeft ? document.documentElement.scrollLeft : document.body.scrollLeft);
        var scrollTop = (document.documentElement.scrollTop ? document.documentElement.scrollTop : document.body.scrollTop);

        //dialog.showDebug('browser width: ' + display.getBrowserWidth() + ', height: ' + display.getBrowserHeight());
        //dialog.showDebug('scroll left: ' + scrollLeft + ', top: ' + scrollTop);
        //dialog.showDebug('horizontal offset: ' + display.getHorizontalOffset() + ', vertical: ' + display.getVerticalOffset());

        if (e.touches[0])
        {
            touchX = Math.round(e.touches[0].pageX ? e.touches[0].pageX : e.touches[0].clientX + scrollLeft) - display.getHorizontalOffset();
            touchY = Math.round(e.touches[0].pageY ? e.touches[0].pageY : e.touches[0].clientY + scrollTop) - display.getVerticalOffset();
        }

        //dialog.showDebug('touch X: ' + touchX + ', Y: ' + touchY);

        if (touchX < 0 || touchY < 0 || touchX > display.getBrowserWidth() + scrollLeft - display.getHorizontalOffset() || touchY > display.getBrowserHeight() + scrollTop - display.getVerticalOffset())
        {
            //dialog.showDebug('touch out of bounds, ignoring');
            return false;
        }

        //dialog.showDebug('*************************');

        return true;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Move                                                                                                                                                                                      ***/
	/*************************************************************************************************************************************************************************************************/

    // last touch position
    var lastTouchX = null;
    var lastTouchY = null;
    
    // current touch position
    var touchX;
    var touchY;

    // touch move sampling (same mechanism as mouse)
    var touchMoveCount = 0;

    function touchMove(e)
    {
        try
        {
            //dialog.showDebug('touch move');

            if (!processEvent(e))
                return false;

            // the touch move event can be fired repeatedly if there is an external application stealing the focus to the browser (i.e.: windows task manager, fiddler, etc. on a 1 sec interval)
            // when the browser gets the focus back, it fires a touch move event...
            // so, if the touch didn't moved since last call, exit
            if (lastTouchX != null && lastTouchY != null && lastTouchX == touchX && lastTouchY == touchY)
            {
                //dialog.showDebug('touch move repeated, ignoring');
                return false;
            }

            // sampling (same mechanism as mouse)
            touchMoveCount++;
            var send = true;
            if (config.getMouseMoveSamplingRate() == 5 ||
                config.getMouseMoveSamplingRate() == 10 ||
                config.getMouseMoveSamplingRate() == 20 ||
                config.getMouseMoveSamplingRate() == 25 ||
                config.getMouseMoveSamplingRate() == 50)
            {
                send = touchMoveCount % (100 / config.getMouseMoveSamplingRate()) == 0;
            }

            // sampling debug: display a dot at the current touch move position (green: move sent, red: dropped) - only if canvas is enabled
            /*
            if (config.getDebugEnabled() && config.getDisplayMode() == config.getDisplayModeEnum().CANVAS)
            {
                display.getCanvas().getCanvasContext().fillStyle = send ? '#00FF00' : '#FF0000';
                display.getCanvas().getCanvasContext().fillRect(touchX, touchY, 1, 1);
            }
            */

            if (send)
            {
                if (config.getAdaptiveFullscreenTimeout() > 0)
                    user.triggerActivity();

                sendEvent(network.getCommandEnum().SEND_MOUSE_MOVE.text + touchX + '-' + touchY);  // same event as mouse move
            }

            // update the last touch position
            lastTouchX = touchX;
            lastTouchY = touchY;
        }
        catch (exc)
        {
            dialog.showDebug('touchscreen move error: ' + exc.message);
        }
        
        user.cancelEvent(e);
        return false;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Tap                                                                                                                                                                                       ***/
	/*************************************************************************************************************************************************************************************************/

    // last touch tap position
    var lastTouchTapX = null;
    var lastTouchTapY = null;
    this.getLastTouchTapX = function () { return lastTouchTapX; };
    this.getLastTouchTapY = function () { return lastTouchTapY; };

    function touchTap(e, start)
    {
        try
        {
            //dialog.showDebug('touch tap');

            if (!processEvent(e))
                return false;

            if (config.getAdaptiveFullscreenTimeout() > 0)
                user.triggerActivity();

            //dialog.showDebug('touch ' + (start ? 'start' : 'end'));
            if (user.getRightClickButton() != null && user.getRightClickButton().value == 'Right-Click ON')
            {
                //dialog.showDebug('emulating mouse right click ' + (start ? 'down' : 'up'));
                sendEvent(network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + start + touchX + '-' + touchY);
                if (!start)
                {
                    user.toggleRightClick(user.getRightClickButton());
                }
            }
            else
            {
                sendEvent(network.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + start + touchX + '-' + touchY);   // same event as mouse left button
            }

            // update the last touch tap position
            lastTouchTapX = touchX;
            lastTouchTapY = touchY;
        }
        catch (exc)
        {
            dialog.showDebug('touchscreen tap ' + (start ? 'start' : 'end') + ' error: ' + exc.message);
        }

        user.cancelEvent(e);
        return false;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Network                                                                                                                                                                                   ***/
	/*************************************************************************************************************************************************************************************************/

    function sendEvent(touchEvent)
    {
        if (touchEvent != null)
        {
            // pass the event to the network
            network.processUserEvent('mouse', touchEvent);  // same logic as mouse
        }
    }
}