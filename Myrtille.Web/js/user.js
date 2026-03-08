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
/*** User                                                                                                                                                                                          ***/
/*****************************************************************************************************************************************************************************************************/

function User(base, config, dialog, display, network)
{
    // adaptive fullscreen update
    var adaptiveFullscreenTimeout = null;

    // event handling
    var eventListener = function() {};
    this.addListener = function(eventType, listener, useCapture) { return eventListener(eventType, listener, useCapture); };

    // passive event listeners
    var passiveEventListeners = false;
    this.getPassiveEventListeners = function() { return passiveEventListeners; };

    // keyboard
    var keyboard = null;
    this.getKeyboard = function() { return keyboard; };

    // mouse
    var mouse = null;
    this.getMouse = function() { return mouse; };

    // touchscreen
    var touchscreen = null;
    this.getTouchscreen = function() { return touchscreen; };

    // send a right click on the next touch or left-click action
    var rightClickButton = null;
    this.getRightClickButton = function() { return rightClickButton; };

    // swipe up/down gesture management for touchscreen devices
    var verticalSwipeEnabled = true;
    this.getVerticalSwipeEnabled = function() { return verticalSwipeEnabled; };

    this.init = function()
    {
        try
        {
            // W3C standard
            if (window.addEventListener)
            {
                //dialog.showDebug('event handling: using window.addEventListener');
                eventListener = window.addEventListener;
                checkPassiveEventListenersSupport();
            }
            // IE < 9
            else if (window.attachEvent && document.attachEvent)
            {
                //dialog.showDebug('event handling: using window.attachEvent and document.attachEvent');
                eventListener = function(eventType, listener, useCapture)
                {
                    // attachEvent wants 'oneventType' instead of 'eventType'
                    if (eventType == 'resize')
                    {
                        window.attachEvent('on' + eventType, listener, useCapture);
                    }
                    else
                    {
                        document.attachEvent('on' + eventType, listener, useCapture);
                    }
                };
            }

            // prompts the user to confirm or not to close the window/tab

            // unfortunately, this method isn't reliable (the browser may decide or not to prompt the user, this is not guaranteed: see https://dev.to/chromiumdev/sure-you-want-to-leavebrowser-beforeunload-event-4eg5)
            // the message is not customizable (the browser shows a generic and confusing message, advising to save data; this doesn't apply to a remote session which isn't even disconnected when the page is closed)
            // also, the browser doesn't differentiate a page reload and close (such a confirm dialog should only be displayed on close); this is a problem because myrtille reloads the page on several occasions
            // this can even be considered as a bad practice to disturb the user with such a confirm dialog (...)
            // anyway, if the browser or page is closed by mistake: cross icon, ctrl + w or alt + f4, etc. (these key combinations can't be captured by javascript because the browser or OS are intercepting them first!), the user can still use its browser history > "pages or tabs closed recently" to resume its session

            //eventListener('beforeunload', function(e) { e.preventDefault(); e.returnValue = 'Are you sure you want to close this page?'; });

            if (config.getHostType() == config.getHostTypeEnum().RDP)
            {
                // responsive display
                eventListener('resize', function() { browserResize(); });

                // default action on browser resize
                if (config.getBrowserResize() == null)
                {
                    var reconnect = document.getElementById('reconnect');
                    if (reconnect != null)
                    {
                        reconnect.value = window.myrtilleI18n
                            ? window.myrtilleI18n.formatToggleLabel('reconnectLabel', config.getDefaultResize() == config.getBrowserResizeEnum().RECONNECT)
                            : (config.getDefaultResize() == config.getBrowserResizeEnum().RECONNECT ? 'Reconnect: On' : 'Reconnect: Off');
                    }
                }

                keyboard = new Keyboard(base, config, dialog, display, network, this);
                keyboard.init();

                mouse = new Mouse(base, config, dialog, display, network, this);
                mouse.init();

                // even if possible to detect if the device has touchscreen capabilities, it would only be an assumption; so, implementing it by default, alongside with mouse...
                // that's anyway the right thing to do, as a device can have both mouse and touchscreen
                // http://www.stucox.com/blog/you-cant-detect-a-touchscreen/
                touchscreen = new Touchscreen(base, config, dialog, display, network, this);
                touchscreen.init();

                // clipboard synchronization on window/tab focus
                eventListener('focus', function() { base.readClipboard(false); });
            }
            else
            {
                // use xterm input handlers for SSH
                display.getTerminalDiv().init(network, this);
            }
        }
        catch (exc)
        {
            dialog.showDebug('user init error: ' + exc.message);
            throw exc;
        }
    };

    // check support of passive event listeners for better scroll performance
    // https://github.com/WICG/EventListenerOptions/blob/gh-pages/explainer.md
    function checkPassiveEventListenersSupport()
    {
        try
        {
            // test via a getter in the options object to see if the passive property is accessed
            var opts = Object.defineProperty({}, 'passive', { get: function() { passiveEventListeners = true; }});
            window.addEventListener('testPassive', null, opts);
            window.removeEventListener('testPassive', null, opts);
        }
        catch (exc)
        {
            dialog.showDebug('user checkPassiveEventListenersSupport error: ' + exc.message);
        }

        //dialog.showDebug('passive event listeners: ' + passiveEventListeners);
    }

    this.toggleRightClick = function(button)
    {
        try
        {
            rightClickButton = button;
            var isOff = rightClickButton.value.indexOf('Off') !== -1 || rightClickButton.value.indexOf('关闭') !== -1;
            rightClickButton.value = window.myrtilleI18n
                ? window.myrtilleI18n.formatToggleLabel('mrcLabel', isOff)
                : (isOff ? 'Right Click: On' : 'Right Click: Off');
            //dialog.showDebug('toggling ' + rightClickButton.value);
        }
        catch (exc)
        {
            dialog.showDebug('user toggleRightClick error: ' + exc.message);
        }
    };

    this.toggleVerticalSwipe = function(button)
    {
        try
        {
            if (display.isIEBrowser())
            {
                alert(window.myrtilleI18n ? window.myrtilleI18n.getTranslation('vswipeDisabledOnIe') : 'This experimental feature is disabled on IE/Edge');
                return;
            }

            verticalSwipeEnabled = !verticalSwipeEnabled;
            button.value = window.myrtilleI18n
                ? window.myrtilleI18n.formatToggleLabel('vswipeLabel', verticalSwipeEnabled)
                : (verticalSwipeEnabled ? 'Swipe: On' : 'Swipe: Off');
            //dialog.showDebug('toggling ' + button.value);
        }
        catch (exc)
        {
            dialog.showDebug('user toggleVerticalSwipe error: ' + exc.message);
        }
    };

    function browserResize()
    {
        if (config.getBrowserResize() == config.getBrowserResizeEnum().NONE)
            return;

        try
        {
            disableToolbar();

            var width = display.getBrowserWidth();
            var height = display.getBrowserHeight();

            // if using myrtille within an iframe, have the parent window to hide the reconnection process (logon sequence)
            if (config.getBrowserResize() == config.getBrowserResizeEnum().RECONNECT && config.getDisplayMode() == config.getDisplayModeEnum().CANVAS && parent != null && window.name != '')
            {
                parent.hideIframeContent(window.name, width, height);
            }

            // send the new browser resolution
            network.send(base.getCommandEnum().SEND_BROWSER_RESIZE.text + (config.getKeepAspectRatio() ? '1' : '0') + '|' + width + 'x' + height);
        }
        catch (exc)
        {
            dialog.showDebug('user browserResize error: ' + exc.message);
        }
    }

    this.triggerActivity = function()
    {
        if (config.getAdaptiveFullscreenTimeout() == 0)
            return;

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
                network.send(base.getCommandEnum().REQUEST_FULLSCREEN_UPDATE.text + 'adaptive');
            },
            config.getAdaptiveFullscreenTimeout());
        }
        catch (exc)
        {
            dialog.showDebug('user triggerActivity error: ' + exc.message);
        }
    };

    this.cancelEvent = function(e)
    {
        //dialog.showDebug('event type: ' + e.type);

        // don't call prevent default when using passive event listeners
        // event capture and propagation are also superseded by such a configuration
        if (passiveEventListeners && (e.type == 'mousewheel' || e.type == 'touchmove' || e.type == 'touchstart' || e.type == 'touchend'))
        {
            //dialog.showDebug('prevent default not applicable');
            return;
        }

        // prevent default action
        if (e.preventDefault) e.preventDefault();   // DOM Level 2
        else e.returnValue = false;                 // IE

        // stop event propagation
        if (e.stopPropagation) e.stopPropagation(); // DOM Level 2
        else e.cancelBubble = true;                 // IE
    };
}
