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
/*** Touchscreen                                                                                                                                                                                   ***/
/*****************************************************************************************************************************************************************************************************/

function Touchscreen(base, config, dialog, display, network, user)
{
    this.init = function()
    {
     
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

            // detect gestures (simple swipe for now, may evolve into more advanced gestures)
            var gesture = false;
            var xDiff;
            var yDiff;

            if (user.getVerticalSwipeEnabled())
            {
                xDiff = lastTouchX - touchX;
                yDiff = lastTouchY - touchY;

                // horizontal move is more significant than vertical
                if (Math.abs(xDiff) > Math.abs(yDiff))
                {
                    if (xDiff > 0)
                    {
                        //dialog.showDebug('left swipe');
                    }
                    else
                    {
                        //dialog.showDebug('right swipe');
                    }
                }
                else
                {
                    if (yDiff > 0)
                    {
                        //dialog.showDebug('up swipe');
                    }
                    else
                    {
                        //dialog.showDebug('down swipe');
                    }

                    // handle gestures
                    gesture = true;
                }
            }

            touchMoveCount++;
            var send = true;

            if (!gesture)
            {
                // sampling (same mechanism as mouse)
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
                    user.triggerActivity();
                    sendEvent(base.getCommandEnum().SEND_MOUSE_MOVE.text + touchX + '-' + touchY);  // same event as mouse move
                }
            }
            else
            {
                user.triggerActivity();

                // cancel the touch tap preceding the gesture
                // replace it by a touch move to set the gesture initial position
                if (touchTapTimeout != null)
                {
                    //dialog.showDebug('cancelling touch tap');
                    window.clearTimeout(touchTapTimeout);
                    touchTapTimeout = null;
                    touchTapCancelled = true;
                    sendEvent(base.getCommandEnum().SEND_MOUSE_MOVE.text + lastTouchTapX + '-' + lastTouchTapY);
                }
                else
                {
                    // sample touch gestures to avoid sending too many mouse wheel events and thus scrolling too fast
                    // some browsers may support a finer touch definition; this is the case of firefox compared to chrome (!)
                    send = touchMoveCount % (display.isFirefoxBrowser() ? 10 : 5) == 0;

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
                        if (yDiff > 0)
                        {
                            // scroll down
                            sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_DOWN.text + lastTouchTapX + '-' + lastTouchTapY);
                        }
                        else
                        {
                            // scroll up
                            sendEvent(base.getCommandEnum().SEND_MOUSE_WHEEL_UP.text + lastTouchTapX + '-' + lastTouchTapY);
                        }
                    }
                }
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

    // wait for a potential gesture following a touch tap
    // if there is a gesture, the touch tap is cancelled
    var touchTapTimeout = null;
    var touchTapCancelled = false;

    function touchTap(e, start)
    {
        try
        {
            //dialog.showDebug('touch tap');

            if (touchTapCancelled)
            {
                //dialog.showDebug('touch tap cancelled');
                touchTapCancelled = false;
                return false;
            }

            if (!processEvent(e))
                return false;

            touchTapTimeout = window.setTimeout(function()
            {
                user.triggerActivity();

                //dialog.showDebug('touch ' + (start ? 'start' : 'end'));
                if (user.getRightClickButton() != null && user.getRightClickButton().value == 'Right-Click ON')
                {
                    //dialog.showDebug('emulating mouse right click ' + (start ? 'down' : 'up'));
                    sendEvent(base.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + start + touchX + '-' + touchY);
                    if (!start)
                    {
                        user.toggleRightClick(user.getRightClickButton());
                    }
                }
                else
                {
                    sendEvent(base.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + start + touchX + '-' + touchY);   // same event as mouse left button
                }
            }, 250);

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