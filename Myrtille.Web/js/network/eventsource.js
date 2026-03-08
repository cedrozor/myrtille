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
/*** Eventsource                                                                                                                                                                                   ***/
/*****************************************************************************************************************************************************************************************************/

function Eventsource(base, config, dialog, display, network)
{
    var sse = null;

    this.init = function()
    {
        try
        {
            //var sseUrl = config.getHttpServerUrl() + 'EventSource.aspx';
            var sseUrl = config.getHttpServerUrl() + 'handlers/EventSourceHandler.ashx';
            //dialog.showDebug('event source url: ' + sseUrl);
            sse = new EventSource(sseUrl);

            sse.onopen = function() { open(); };
            sse.onmessage = function (e) { message(e); };
            sse.onerror = function() { error(); };
        }
        catch (exc)
        {
            dialog.showDebug('event source init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            sse = null;
            return;
        }
    };

    function open()
    {
        dialog.showDebug('event source connection opened');
    }

    function message(e)
    {
        if (config.getAdditionalLatency() > 0)
        {
            window.setTimeout(function() { receive(e.data); }, Math.round(config.getAdditionalLatency() / 2));
        }
        else
        {
            receive(e.data);
        }
    }

    function error()
    {
        dialog.showDebug('event source connection error');
    }

    function receive(data)
    {
        try
        {
            //dialog.showDebug('received event source data: ' + data + ', length: ' + data.Length);

            if (data != null && data != '')
            {
                // event source data is text only (no binary support)
                var text = data;
                var message = true;

                if (config.getHostType() == config.getHostTypeEnum().RDP)
                {
                    try
                    {
                        // messages are serialized in JSON, unlike images; check a valid JSON string
                        JSON.parse(text);
                        //dialog.showDebug('message data: ' + text);
                    }
                    catch (exc)
                    {
                        message = false;
                        //dialog.showDebug('image data: ' + text);
                    }
                }
                else
                {
                    //dialog.showDebug('terminal data: ' + text);
                }

                // message
                if (message)
                {
                    processMessage(text);
                }
                // image
                else
                {
                    var imgInfo, idx, posX, posY, width, height, format, quality, fullscreen, imgData;

                    imgInfo = text.split(',');

                    idx = parseInt(imgInfo[0]);
                    posX = parseInt(imgInfo[1]);
                    posY = parseInt(imgInfo[2]);
                    width = parseInt(imgInfo[3]);
                    height = parseInt(imgInfo[4]);
                    format = imgInfo[5];
                    quality = parseInt(imgInfo[6]);
                    fullscreen = imgInfo[7] == 'true';
                    imgData = imgInfo[8];

                    processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('event source receive error: ' + exc.message);
        }
    }
}