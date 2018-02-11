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
/*** Buffer                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function Buffer(config, dialog, network)
{
    var bufferDelay = config.getBufferDelayBase();
    this.getBufferDelay = function() { return bufferDelay; };
    this.setBufferDelay = function(delay) { bufferDelay = delay; };

    var bufferData = new Array();
    this.getBufferData = function() { return bufferData; };

    var sendEmptyBuffer = false;
    this.getSendEmptyBuffer = function() { return sendEmptyBuffer; };
    this.setSendEmptyBuffer = function(enabled) { sendEmptyBuffer = enabled; };

    var clearBuffer = true;
    this.setClearBuffer = function(enabled) { clearBuffer = enabled; };

    var bufferTimeout = null;
    
    this.init = function()
    {
        try
        {
            // if using xhr only, updates are returned within the user inputs xhr responses; thus, the display is not updated while the user does nothing (no keyboard or mouse event)
            // to work that out, the user inputs buffer should be sent *even if empty* in order to allow retrieving the latest updates
            // problem is, on a low latency network, this will generate alot of xhr trafic which may interfere with others xhr's such as periodical and adaptative fullscreen updates!
            // to avoid that, and because there is no need for a frantic polling, a small timing is added
            if (config.getNetworkMode() == config.getNetworkModeEnum().XHR)
            {
                bufferDelay = config.getBufferDelayEmpty();
                sendEmptyBuffer = true;
            }

            bufferTimeout = window.setTimeout(function() { doFlush(); }, bufferDelay);
        }
        catch (exc)
        {
            dialog.showDebug('buffer init error: ' + exc.message);
            config.setBufferEnabled(false);
        }
    };

    this.flush = function()
    {
        doFlush();
    };

    function doFlush()
    {
        try
        {
            if (!config.getBufferEnabled())
                return;

            if (bufferTimeout != null)
            {
                window.clearTimeout(bufferTimeout);
                bufferTimeout = null;
            }

            if (bufferData.length > 0 || sendEmptyBuffer)
            {
                //dialog.showDebug('flushing buffer: ' + bufferData.toString());

                // send the buffered data over the network
                network.send(bufferData.toString());

                // if the data was sent successfully, clear the buffer
                if (clearBuffer)
                {
                    //dialog.showDebug('clearing buffer');
                    bufferData = [];
                }
            }
            
            // set the next buffer flush tick
            //dialog.showDebug('buffer delay: ' + bufferDelay);
            bufferTimeout = window.setTimeout(function() { doFlush(); }, bufferDelay);
        }
        catch (exc)
        {
            dialog.showDebug('buffer flush error: ' + exc.message);
        }
    }
}