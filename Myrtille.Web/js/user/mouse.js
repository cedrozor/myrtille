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
/*** Mouse                                                                                                                                                                                         ***/
/*****************************************************************************************************************************************************************************************************/

function Mouse(config, dialog, display, network, user)
{
    this.init = function()
    {
        try
        {
            user.addListener('mousemove', function(e) { mouseMove(e); });
            user.addListener('mousedown', function(e) { mouseClick(e, 1); });
            user.addListener('mouseup', function(e) { mouseClick(e, 0); });
            user.addListener(display.isFirefoxBrowser() ? 'DOMMouseScroll' : 'mousewheel', function(e) { mouseScroll(e); });
            user.addListener('selectstart', function() { return false; });
            user.addListener('contextmenu', function(e) { user.cancelEvent(e); return false; });
        }
        catch (exc)
        {
            dialog.showDebug('mouse init error: ' + exc.message);
            throw exc;
        }
    };

    function processEvent(e)
    {
        if (e == null)
            e = window.event;
        
        if (e == null)
            return false;

        if (!setMousePosition(e))
            return false;
        
        return true;
    }

    function setMousePosition(e)
    {
        var scrollLeft = (document.documentElement.scrollLeft ? document.documentElement.scrollLeft : document.body.scrollLeft);
        var scrollTop = (document.documentElement.scrollTop ? document.documentElement.scrollTop : document.body.scrollTop);

        //dialog.showDebug('browser width: ' + display.getBrowserWidth() + ', height: ' + display.getBrowserHeight());
        //dialog.showDebug('scroll left: ' + scrollLeft + ', top: ' + scrollTop);
        //dialog.showDebug('horizontal offset: ' + display.getHorizontalOffset() + ', vertical: ' + display.getVerticalOffset());

        mouseX = (e.pageX ? e.pageX : e.clientX + scrollLeft) - display.getHorizontalOffset();
        mouseY = (e.pageY ? e.pageY : e.clientY + scrollTop) - display.getVerticalOffset();

        //dialog.showDebug('mouse X: ' + mouseX + ', Y: ' + mouseY);

        if (mouseX < 0 || mouseY < 0 || mouseX > display.getBrowserWidth() + scrollLeft - display.getHorizontalOffset() || mouseY > display.getBrowserHeight() + scrollTop - display.getVerticalOffset())
        {
            //dialog.showDebug('mouse out of bounds, ignoring');
            return false;
        }

        //dialog.showDebug('*************************');

        return true;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Move                                                                                                                                                                                      ***/
	/*************************************************************************************************************************************************************************************************/

    // last mouse position
    var lastMouseX = null;
    var lastMouseY = null;
    
    // current mouse position
    var mouseX;
    var mouseY;

    // mouse move sampling
    var mouseMoveCount = 0;

    function mouseMove(e)
    {
        try
        {
            //dialog.showDebug('mouse move');

            if (!processEvent(e))
                return false;

            // the mouse move event can be fired repeatedly if there is an external application stealing the focus to the browser (i.e.: windows task manager, fiddler, etc. on a 1 sec interval)
            // when the browser gets the focus back, it fires a mouse move event...
            // so, if the mouse didn't moved since last call, exit
            if (lastMouseX != null && lastMouseY != null && lastMouseX == mouseX && lastMouseY == mouseY)
            {
                //dialog.showDebug('mouse move repeated, ignoring');
                return false;
            }

            // sampling
            mouseMoveCount++;
            var send = true;
            if (config.getMouseMoveSamplingRate() == 5 ||
                config.getMouseMoveSamplingRate() == 10 ||
                config.getMouseMoveSamplingRate() == 20 ||
                config.getMouseMoveSamplingRate() == 25 ||
                config.getMouseMoveSamplingRate() == 50)
            {
                send = mouseMoveCount % (100 / config.getMouseMoveSamplingRate()) == 0;
            }

            // sampling debug: display a dot at the current mouse move position (green: move sent, red: dropped) - only if canvas is enabled
            /*
            if (config.getDebugEnabled() && config.getDisplayMode() == config.getDisplayModeEnum().CANVAS)
            {
                display.getCanvas().getCanvasContext().fillStyle = send ? '#00FF00' : '#FF0000';
                display.getCanvas().getCanvasContext().fillRect(mouseX, mouseY, 1, 1);
            }
            */

            if (send)
            {
                if (config.getAdaptiveFullscreenTimeout() > 0)
                    user.triggerActivity();

                sendEvent(network.getCommandEnum().SEND_MOUSE_MOVE.text + mouseX + '-' + mouseY);
            }

            // update the last mouse position
            lastMouseX = mouseX;
            lastMouseY = mouseY;
        }
        catch (exc)
        {
            dialog.showDebug('mouse move error: ' + exc.message);
        }
        
        user.cancelEvent(e);
        return false;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Click                                                                                                                                                                                     ***/
	/*************************************************************************************************************************************************************************************************/

    // last mouse click position
    var lastMouseClickX = null;
    var lastMouseClickY = null;
    this.getLastMouseClickX = function () { return lastMouseClickX; };
    this.getLastMouseClickY = function () { return lastMouseClickY; };

    function mouseClick(e, down)
    {
        try
        {
            //dialog.showDebug('mouse click');

            if (!processEvent(e))
                return false;

            if (config.getAdaptiveFullscreenTimeout() > 0)
                user.triggerActivity();

            // IE
            if (display.isIEBrowser() && display.getIEVersion() < 9)
            {
                switch (e.button)
                {
                    case 1:
                        //dialog.showDebug('mouse left click ' + (down ? 'down' : 'up'));
                        if (user.getRightClickButton() != null && user.getRightClickButton().value == 'Right-Click ON')
                        {
                            //dialog.showDebug('emulating mouse right click ' + (down ? 'down' : 'up'));
                            sendEvent(network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                            if (!down)
                            {
                                user.toggleRightClick(user.getRightClickButton());
                            }
                        }
                        else
                        {
                            sendEvent(network.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + down + mouseX + '-' + mouseY);
                        }
                        break;
                    case 4:
                        //dialog.showDebug('mouse middle click ' + (down ? 'down' : 'up'));
                        sendEvent(network.getCommandEnum().SEND_MOUSE_MIDDLE_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                    case 2:
                        //dialog.showDebug('mouse right click ' + (down ? 'down' : 'up'));
                        sendEvent(network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                }
            }
            // others
            else
            {
                switch (e.button)
                {
                    case 0:
                        //dialog.showDebug('mouse left click ' + (down ? 'down' : 'up'));
                        if (user.getRightClickButton() != null && user.getRightClickButton().value == 'Right-Click ON')
                        {
                            //dialog.showDebug('emulating mouse right click ' + (down ? 'down' : 'up'));
                            sendEvent(network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                            if (!down)
                            {
                                user.toggleRightClick(user.getRightClickButton());
                            }
                        }
                        else
                        {
                            sendEvent(network.getCommandEnum().SEND_MOUSE_LEFT_BUTTON.text + down + mouseX + '-' + mouseY);
                        }
                        break;
                    case 1:
                        //dialog.showDebug('mouse middle click ' + (down ? 'down' : 'up'));
                        sendEvent(network.getCommandEnum().SEND_MOUSE_MIDDLE_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                    case 2:
                        //dialog.showDebug('mouse right click ' + (down ? 'down' : 'up'));
                        sendEvent(network.getCommandEnum().SEND_MOUSE_RIGHT_BUTTON.text + down + mouseX + '-' + mouseY);
                        break;
                }
            }

            // update the last mouse click position
            lastMouseClickX = mouseX;
            lastMouseClickY = mouseY;
        }
        catch (exc)
        {
            dialog.showDebug('mouse click ' + (down ? 'down' : 'up') + ' error: ' + exc.message);
        }

        // the canvas needs to be focused in order to capture keyboard events
        // so, if using it, the mouse click event must be propagated; it can be cancelled otherwise
        if (config.getDisplayMode() != config.getDisplayModeEnum().CANVAS)
        {
            user.cancelEvent(e);
            return false;
        }

        return true;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Scroll                                                                                                                                                                                    ***/
	/*************************************************************************************************************************************************************************************************/

    function mouseScroll(e)
    {
        try
        {
            //dialog.showDebug('mouse scroll');

            if (!processEvent(e))
                return false;
        
            if (config.getAdaptiveFullscreenTimeout() > 0)
                user.triggerActivity();

            var delta = 0;

            if (e.detail)
                delta = -e.detail / 3;          // firefox
            else if (e.wheelDelta)
                delta = e.wheelDelta / 120;     // others

            if (delta > 0)
            {
                //dialog.showDebug('mouse scroll up');
                sendEvent(network.getCommandEnum().SEND_MOUSE_WHEEL_UP.text + mouseX + '-' + mouseY);
            }
            else if (delta < 0)
            {
                //dialog.showDebug('mouse scroll down');
                sendEvent(network.getCommandEnum().SEND_MOUSE_WHEEL_DOWN.text + mouseX + '-' + mouseY);
            }
        }
        catch (exc)
        {
            dialog.showDebug('mouse scroll error: ' + exc.message);
        }
        
        user.cancelEvent(e);
        return false;
    }

	/*************************************************************************************************************************************************************************************************/
	/*** Network                                                                                                                                                                                   ***/
	/*************************************************************************************************************************************************************************************************/

    function sendEvent(mouseEvent)
    {
        if (mouseEvent != null)
        {
            // pass the event to the network
            network.processUserEvent('mouse', mouseEvent);
        }
    }
}