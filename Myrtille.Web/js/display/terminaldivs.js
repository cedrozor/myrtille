/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2008 Olive Innovations

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
/*** Terminal                                                                                                                                                                                        ***/
/*****************************************************************************************************************************************************************************************************/

function TerminalDivs(config, dialog, display) {
    var term = null;
    
    this.init = function () {
        try {
            var div = document.createElement('div');
            div.id = 'terminalDiv';
            div.style.width = config.getScaleDisplay() ? display.getBrowserWidth() - display.getHorizontalOffset() : config.getDisplayWidth() + 'px';
            div.style.height = config.getScaleDisplay() ? display.getBrowserHeight() - display.getVerticalOffset() : config.getDisplayHeight() + 'px';

            display.getDisplayDiv().appendChild(div);

            Terminal.applyAddon(fit);
            term = new Terminal();
            term.open(div);
            term.fit();

        }
        catch (exc) {
            dialog.showDebug('terminal div init error: ' + exc.message + ', falling back to divs');
            config.setDisplayMode(config.getDisplayModeEnum().DIV);
            return;
        }

       
    };

    this.writeTerminal = function (data) {
        try {
            term.write(data);
        }
        catch (exc) {
            dialog.showDebug('terminal div  write terminal error: ' + exc.message);
            throw exc;
        }
    };

    this.closeTerminal = function()
    {
        try{
            term.clear();
            term.destroy();

        }
        catch (exc) {
            dialog.showDebug('terminal div close error: ' + exc.message);
            throw exc;
        }
    }
}