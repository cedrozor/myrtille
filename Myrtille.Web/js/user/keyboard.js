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
/*** Keyboard                                                                                                                                                                                      ***/
/*****************************************************************************************************************************************************************************************************/

function Keyboard(base, config, dialog, display, network, user)
{
    this.init = function()
    {
        try
        {
            user.addListener('keydown', function(e) { keyDown(e); });
            user.addListener('keypress', function(e) { keyPress(e); });
            user.addListener('keyup', function(e) { keyUp(e); });
        }
        catch (exc)
        {
            dialog.showDebug('keyboard init error: ' + exc.message);
            throw exc;
        }
    };

    function processEvent(e)
    {
        if (e == null)
            e = window.event;
        
        if (e == null)
            return false;

        user.triggerActivity();

        return true;
    }

    function KeyInfo(keyCode, keyLocation)
    {
        this.KeyCode = keyCode;
        this.KeyLocation = keyLocation;
    }

    function processKeyState(e, keyState, modKeyActive)
    {
	    //dialog.showDebug(keyState);

        if (!processEvent(e))
            return false;

        var keyCode = null;
        var keyLocation = null;

        if (window.event)
        {
            keyCode = window.event.keyCode;
            keyLocation = window.event.location;
        }
        else
        {
            if (e.which) keyCode = e.which;
            if (e.location) keyLocation = e.location;
        }

        if (keyCode == null)
            return false;

        //dialog.showDebug(keyState + ' key: ' + e.key + ', code: ' + e.code + ', KeyCode: ' + keyCode + ', location: ' + keyLocation + (keyLocation == 1 ? ' (left)' : (keyLocation == 2 ? ' (right)' : '')));

        if (keyCode == 144)     // numlock
            return false;

		// keyboard modifiers
        if (keyCode == 16)
			modShift = modKeyActive;
		else if (keyCode == 17)
			modCtrl = modKeyActive;
		else if (keyCode == 18)
			modAlt = modKeyActive;

        return new KeyInfo(keyCode, keyLocation);
    }

    this.setKeyCombination = function()
    {
        //dialog.showDebug('waiting key combination');
        keyCombination = true;
        keyCombinationTimeout = window.setTimeout(function()
        {
            //dialog.showDebug('cancelling key combination');
            keyCombination = false;
            keyCombinationTimeout = null;
        }, 1500);
    };

	/*************************************************************************************************************************************************************************************************/
	/*** Keydown                                                                                                                                                                                   ***/
	/*************************************************************************************************************************************************************************************************/

    // keyboard modifiers
	var modShift = false;
	var modCtrl = false;
	var modAlt = false;

    // key combination
    var keyCombination = false;
    var keyCombinationTimeout = null;

    // latest keydown code
    var keydownCode = null;

    // keycode to charcode mapping
    var keyCodeToCharCode = new Array();

	function keyDown(e)
    {
	    try
	    {
	        var keyInfo = processKeyState(e, 'keydown', true);
	        
            if (!keyInfo)
                return false;

            // the alt key may interfere with the browser alt + key menu
            // fact is, the remote session receives the alt key down but NOT the alt key up
            // this is due to the page (or iframe) losing the focus (on alt key down) and thus is no longer able to listen for input events (and capture alt key up)
            // this is nasty because the remote session remains stuck, waiting for an alt key up which will never arrive! the result is, the user can no longer type any text...
            // if it happens, the only way to unblock the session is to send a ctrl+alt+del sequence
            if (keyInfo.KeyCode == 18 && keyInfo.KeyLocation == 1)
            {
                user.cancelEvent(e);
                return false;
            }

            // non character keys are sent as scancodes on keydown and keyup
	        // character keys are sent as unicode on keypress and keyup, using the keypress character code
            var keyIsChar =
              !((keyInfo.KeyCode == 8) ||       // backspace
                (keyInfo.KeyCode == 9) ||       // tab
                (keyInfo.KeyCode == 13) ||      // enter
                (keyInfo.KeyCode == 16) ||      // shift
                (keyInfo.KeyCode == 17) ||      // ctrl
                (keyInfo.KeyCode == 18) ||      // alt
                (keyInfo.KeyCode == 27) ||      // esc
                (keyInfo.KeyCode == 35) ||      // end
                (keyInfo.KeyCode == 36) ||      // home
                (keyInfo.KeyCode == 33) ||      // page up
                (keyInfo.KeyCode == 34) ||      // page down
                (keyInfo.KeyCode == 37) ||      // keypad left arrow
                (keyInfo.KeyCode == 38) ||      // keypad up arrow
                (keyInfo.KeyCode == 39) ||      // keypad right arrow
                (keyInfo.KeyCode == 40) ||      // keypad down arrow
                (keyInfo.KeyCode == 45) ||      // insert
                (keyInfo.KeyCode == 46) ||      // delete
                (keyInfo.KeyCode == 112) ||     // F1
                (keyInfo.KeyCode == 113) ||     // F2
                (keyInfo.KeyCode == 114) ||     // F3
                (keyInfo.KeyCode == 115) ||     // F4
                (keyInfo.KeyCode == 116) ||     // F5
                (keyInfo.KeyCode == 117) ||     // F6
                (keyInfo.KeyCode == 118) ||     // F7
                (keyInfo.KeyCode == 119) ||     // F8
                (keyInfo.KeyCode == 120) ||     // F9
                (keyInfo.KeyCode == 121) ||     // F10
                (keyInfo.KeyCode == 122) ||     // F11
                (keyInfo.KeyCode == 123));      // F12

            // RDP over VM bus (Hyper-V): send keys as scancodes when enhanced mode is disabled
            if (!keyIsChar || modCtrl || modAlt || config.getVMNotEnhanced())
            {
                // if running myrtille into an iframe, provide a key combination (shift + tab) to switch iframe(s) focus
                if (parent != null && window.name != '')
                {
                    if (!keyCombination)
                    {
                        // shift
                        if (keyInfo.KeyCode == 16)
                        {
                            this.setKeyCombination();
                            user.cancelEvent(e);
                            return false;
                        }
                    }
                    else
                    {
                        // tab
                        if (keyInfo.KeyCode == 9)
                        {
                            keyCombination = false;
                            if (keyCombinationTimeout != null)
                            {
                                //dialog.showDebug('key combination complete');
                                window.clearTimeout(keyCombinationTimeout);
                                keyCombinationTimeout = null;
                            }
                            parent.switchIframe(window.name);
                            user.cancelEvent(e);
                            return false;
                        }
                    }
                }
                sendEvent(keyInfo.KeyCode, true, false);
                user.cancelEvent(e);
                return false;
            }

            keydownCode = keyInfo.KeyCode;
        }
        catch (exc)
        {
            dialog.showDebug('key down error: ' + exc.message);
        }

	    return true;
	}

	/*************************************************************************************************************************************************************************************************/
	/*** Keypress                                                                                                                                                                                  ***/
	/*************************************************************************************************************************************************************************************************/

    function keyPress(e)
    {
        try
        {
            //dialog.showDebug('keypress');

            if (!processEvent(e))
                return false;

		    var charCode;
		    if (window.event) charCode = (window.event.charCode ? window.event.charCode : window.event.keyCode);
            else if (e.charCode) charCode = e.charCode;
		    else if (e.which) charCode = e.which;

            if (charCode == null)
                return false;

            //dialog.showDebug('keypress code: ' + charCode);

            // bind keydown code to keypress code
            keyCodeToCharCode[keydownCode] = charCode;
            
            sendEvent(charCode, true, true);
        }
        catch (exc)
        {
            dialog.showDebug('key press error: ' + exc.message);
        }

        user.cancelEvent(e);
        return false;
	}

	/*************************************************************************************************************************************************************************************************/
	/*** Keyup                                                                                                                                                                                     ***/
	/*************************************************************************************************************************************************************************************************/

	function keyUp(e)
    {
	    try
	    {
	        var keyInfo = processKeyState(e, 'keyup', false);
	        
            if (!keyInfo)
                return false;

            // alt
            if (keyInfo.KeyCode == 18 && keyInfo.KeyLocation == 1)
            {
                user.cancelEvent(e);
                return false;
            }

            if (keyCombination)
                return false;

            // non character key
            if (keyCodeToCharCode[keyInfo.KeyCode] == null)
            {
                sendEvent(keyInfo.KeyCode, false, false);
            }
            // character key
	        else
	        {
                sendEvent(keyCodeToCharCode[keyInfo.KeyCode], false, true);
                keyCodeToCharCode[keyInfo.KeyCode] = null;
	        }

            //dialog.showDebug('*************************');
        }
        catch (exc)
        {
            dialog.showDebug('key up error: ' + exc.message);
        }

        user.cancelEvent(e);
		return false;
	}

	/*************************************************************************************************************************************************************************************************/
	/*** Network                                                                                                                                                                                   ***/
	/*************************************************************************************************************************************************************************************************/

	function sendEvent(keyCode, keyPressed, keyIsChar)
    {
        if (keyCode != null)
        {
            //dialog.showDebug('key' + (keyPressed ? 'down' : 'up') + ' is ' + (keyIsChar ? '' : 'not ') + 'a character, sending code: ' + keyCode);

            // if enabled, display the typed text in an helper div (helpful to debug keyboard issues or in case of high network latency)
            if (config.getKeyboardHelperEnabled() && keyPressed && keyIsChar)
                dialog.showKeyboardHelper(String.fromCharCode(keyCode));

            // a key event is composed of 2 parts: key code and state
            // the key code is prefixed to indicate the server to process it as unicode (character key) or scancode (non character key)
            // the key state is either 1 (down) or 0 (up)
            var keyEvent = (keyIsChar ? base.getCommandEnum().SEND_KEY_UNICODE.text : base.getCommandEnum().SEND_KEY_SCANCODE.text) + keyCode + '_' + (keyPressed ? '1' : '0') + '_' + config.getVmKeyboardLayout();

            // pass the event to the network
            network.processUserEvent('keyboard', keyEvent);
        }
	}
}