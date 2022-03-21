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

using System.Collections;
using System.Collections.Generic;

namespace Myrtille.Web
{
    public class RdpScanCode
    {
        public int Value { get; private set; }
        public bool Extend { get; private set; }

        /// <summary>
        /// rdp scancode
        /// </summary>
        /// <remarks>
        /// rdp scancodes are directly mapped to a physical keyboard layout
        /// </remarks>
        /// <param name="value">decimal value</param>
        /// <param name="extend">non alphanumeric characters must be extended</param>
        public RdpScanCode(int value, bool extend)
        {
            Value = value;
            Extend = extend;
        }
    }

    public static class JsKeyCodeToRdpScanCodeMapping
    {
        public static Dictionary<string, Hashtable> MapTable { get; private set; }

        static JsKeyCodeToRdpScanCodeMapping()
        {
            // this mapping is not complete (latin characters only); fill it as needed (below values are those for qwerty (US) and azerty (FR) layouts)
            // keys (javascript keycodes) and values (rdp scancodes) are decimals (scancodes are often expressed in hexadecimal). scancode value 0 means there is no available translation (at least for now)
            // see http://protocolsofmatrix.blogspot.com/2007/09/javascript-keycode-reference-table-for.html or http://www.webonweboff.com/tips/js/event_key_codes.aspx for js keycodes
            // see http://www.vmware.com/support/ws55/doc/ws_devices_keymap_vscan.html or https://github.com/neutrinolabs/xrdp/blob/devel/xrdp/rdp-scan-codes.txt for rdp scancodes

            // see http://keycode.info/ for javascript keycodes and more, or https://w3c.github.io/uievents/tools/key-event-viewer.html
            // see http://kbdlayout.info/ for international keyboard layouts

            // the left and right location distinction for key modifiers is not currently done
            // while this is fine for most applications, some of them may need this distinction
            // also, some keys are a combination of key modifiers (i.e.: alt+gr = left ctrl + right alt)

            // TODO: make that distinction, by passing the key location from javascript (in addition to the key code),
            // then use the appropriate rdp scancodes (currently disabled below)

            MapTable = new Dictionary<string, Hashtable>();

            {
                // en-us layout
                var layout = new Hashtable();
                MapTable.Add("en-us", layout);
                layout.Add(8, new RdpScanCode(14, false));            // backspace
                layout.Add(9, new RdpScanCode(15, false));            // tab
                layout.Add(13, new RdpScanCode(28, false));           // enter
                layout.Add(16, new RdpScanCode(42, false));           // shift (left)
                                                                    //MapTable.Add(16, new RdpScanCode(54, false));         // shift (right)
                layout.Add(17, new RdpScanCode(29, false));           // ctrl (left)
                                                                    //MapTable.Add(17, new RdpScanCode(29, true));          // ctrl (right)
                                                                    //MapTable.Add(18, new RdpScanCode(56, false));         // alt (left) * disabled to prevent interfering with the browser alt+key menu *
                layout.Add(18, new RdpScanCode(56, true));            // alt (right)
                layout.Add(19, new RdpScanCode(256, false));          // pause/break
                layout.Add(20, new RdpScanCode(58, false));           // caps lock
                layout.Add(27, new RdpScanCode(1, false));            // escape
                layout.Add(32, new RdpScanCode(57, false));           // space
                layout.Add(33, new RdpScanCode(73, true));            // page up
                layout.Add(34, new RdpScanCode(81, true));            // page down
                layout.Add(35, new RdpScanCode(79, true));            // end
                layout.Add(36, new RdpScanCode(71, true));            // home
                layout.Add(37, new RdpScanCode(75, true));            // left arrow
                layout.Add(38, new RdpScanCode(72, true));            // up arrow
                layout.Add(39, new RdpScanCode(77, true));            // right arrow
                layout.Add(40, new RdpScanCode(80, true));            // down arrow
                layout.Add(45, new RdpScanCode(82, true));            // insert
                layout.Add(46, new RdpScanCode(83, true));            // delete
                layout.Add(48, new RdpScanCode(11, false));           // 0
                layout.Add(49, new RdpScanCode(2, false));            // 1
                layout.Add(50, new RdpScanCode(3, false));            // 2
                layout.Add(51, new RdpScanCode(4, false));            // 3
                layout.Add(52, new RdpScanCode(5, false));            // 4
                layout.Add(53, new RdpScanCode(6, false));            // 5
                layout.Add(54, new RdpScanCode(7, false));            // 6
                layout.Add(55, new RdpScanCode(8, false));            // 7
                layout.Add(56, new RdpScanCode(9, false));            // 8
                layout.Add(57, new RdpScanCode(10, false));           // 9
                layout.Add(65, new RdpScanCode(30, false));           // a qwerty
                                                                    //MapTable.Add(65, new RdpScanCode(16, false));         // a azerty
                layout.Add(66, new RdpScanCode(48, false));           // b
                layout.Add(67, new RdpScanCode(46, false));           // c
                layout.Add(68, new RdpScanCode(32, false));           // d
                layout.Add(69, new RdpScanCode(18, false));           // e
                layout.Add(70, new RdpScanCode(33, false));           // f
                layout.Add(71, new RdpScanCode(34, false));           // g
                layout.Add(72, new RdpScanCode(35, false));           // h
                layout.Add(73, new RdpScanCode(23, false));           // i
                layout.Add(74, new RdpScanCode(36, false));           // j
                layout.Add(75, new RdpScanCode(37, false));           // k
                layout.Add(76, new RdpScanCode(38, false));           // l
                layout.Add(77, new RdpScanCode(50, false));           // m qwerty
                                                                    //MapTable.Add(77, new RdpScanCode(39, false));         // m azerty
                layout.Add(78, new RdpScanCode(49, false));           // n
                layout.Add(79, new RdpScanCode(24, false));           // o
                layout.Add(80, new RdpScanCode(25, false));           // p
                layout.Add(81, new RdpScanCode(16, false));           // q qwerty
                                                                    //MapTable.Add(81, new RdpScanCode(30, false));         // q azerty
                layout.Add(82, new RdpScanCode(19, false));           // r
                layout.Add(83, new RdpScanCode(31, false));           // s
                layout.Add(84, new RdpScanCode(20, false));           // t
                layout.Add(85, new RdpScanCode(22, false));           // u
                layout.Add(86, new RdpScanCode(47, false));           // v
                layout.Add(87, new RdpScanCode(17, false));           // w qwerty
                                                                    //MapTable.Add(87, new RdpScanCode(44, false));         // w azerty
                layout.Add(88, new RdpScanCode(45, false));           // x
                layout.Add(89, new RdpScanCode(21, false));           // y
                layout.Add(90, new RdpScanCode(44, false));           // z qwerty
                                                                    //MapTable.Add(90, new RdpScanCode(17, false));         // z azerty
                layout.Add(91, new RdpScanCode(347, true));           // left window key
                layout.Add(92, new RdpScanCode(348, true));           // right window key
                layout.Add(93, new RdpScanCode(0, true));             // select key
                layout.Add(96, new RdpScanCode(82, false));           // numpad 0
                layout.Add(97, new RdpScanCode(79, false));           // numpad 1
                layout.Add(98, new RdpScanCode(80, false));           // numpad 2
                layout.Add(99, new RdpScanCode(81, false));           // numpad 3
                layout.Add(100, new RdpScanCode(75, false));          // numpad 4
                layout.Add(101, new RdpScanCode(76, false));          // numpad 5
                layout.Add(102, new RdpScanCode(77, false));          // numpad 6
                layout.Add(103, new RdpScanCode(71, false));          // numpad 7
                layout.Add(104, new RdpScanCode(72, false));          // numpad 8
                layout.Add(105, new RdpScanCode(73, false));          // numpad 9
                layout.Add(106, new RdpScanCode(55, false));          // multiply
                layout.Add(107, new RdpScanCode(78, false));          // add
                layout.Add(109, new RdpScanCode(74, false));          // subtract
                layout.Add(110, new RdpScanCode(52, false));          // decimal point
                layout.Add(111, new RdpScanCode(53, true));           // divide
                layout.Add(112, new RdpScanCode(59, false));          // f1
                layout.Add(113, new RdpScanCode(60, false));          // f2
                layout.Add(114, new RdpScanCode(61, false));          // f3
                layout.Add(115, new RdpScanCode(62, false));          // f4
                layout.Add(116, new RdpScanCode(63, false));          // f5
                layout.Add(117, new RdpScanCode(64, false));          // f6
                layout.Add(118, new RdpScanCode(65, false));          // f7
                layout.Add(119, new RdpScanCode(66, false));          // f8
                layout.Add(120, new RdpScanCode(67, false));          // f9
                layout.Add(121, new RdpScanCode(68, false));          // f10
                layout.Add(122, new RdpScanCode(87, false));          // f11
                layout.Add(123, new RdpScanCode(88, false));          // f12
                layout.Add(144, new RdpScanCode(69, false));          // num lock
                layout.Add(145, new RdpScanCode(70, false));          // scroll lock
                layout.Add(186, new RdpScanCode(39, false));          // semi-colon qwerty
                                                                    //MapTable.Add(186, new RdpScanCode(50, false));        // semi-colon azerty
                layout.Add(187, new RdpScanCode(13, false));          // equal sign
                layout.Add(188, new RdpScanCode(51, false));          // comma
                layout.Add(189, new RdpScanCode(12, false));          // dash
                layout.Add(190, new RdpScanCode(52, false));          // period
                layout.Add(191, new RdpScanCode(53, false));          // forward slash
                layout.Add(192, new RdpScanCode(41, false));          // grave accent qwerty
                                                                    //MapTable.Add(192, new RdpScanCode(40, false));        // grave accent azerty
                layout.Add(219, new RdpScanCode(26, false));          // open bracket
                layout.Add(220, new RdpScanCode(43, false));          // back slash
                layout.Add(221, new RdpScanCode(27, false));          // close bracket
                layout.Add(222, new RdpScanCode(40, false));          // single quote qwerty
                                                                    //MapTable.Add(222, new RdpScanCode(41, false));        // single quote azerty
                layout.Add(223, new RdpScanCode(0, false));           // ?
                layout.Add(226, new RdpScanCode(86, false));          // lesser than
            }

            {
                // de-de layout
                var layout = new Hashtable();
                MapTable.Add("de-de", layout);
                layout.Add(8, new RdpScanCode(14, false));            // backspace
                layout.Add(9, new RdpScanCode(15, false));            // tab
                layout.Add(13, new RdpScanCode(28, false));           // enter
                layout.Add(16, new RdpScanCode(42, false));           // shift (left)
                                                                    //MapTable.Add(16, new RdpScanCode(54, false));         // shift (right)
                layout.Add(17, new RdpScanCode(29, false));           // ctrl (left)
                                                                    //MapTable.Add(17, new RdpScanCode(29, true));          // ctrl (right)
                                                                    //MapTable.Add(18, new RdpScanCode(56, false));         // alt (left) * disabled to prevent interfering with the browser alt+key menu *
                layout.Add(18, new RdpScanCode(56, true));            // alt (right)
                layout.Add(19, new RdpScanCode(256, false));          // pause/break
                layout.Add(20, new RdpScanCode(58, false));           // caps lock
                layout.Add(27, new RdpScanCode(1, false));            // escape
                layout.Add(32, new RdpScanCode(57, false));           // space
                layout.Add(33, new RdpScanCode(73, true));            // page up
                layout.Add(34, new RdpScanCode(81, true));            // page down
                layout.Add(35, new RdpScanCode(79, true));            // end
                layout.Add(36, new RdpScanCode(71, true));            // home
                layout.Add(37, new RdpScanCode(75, true));            // left arrow
                layout.Add(38, new RdpScanCode(72, true));            // up arrow
                layout.Add(39, new RdpScanCode(77, true));            // right arrow
                layout.Add(40, new RdpScanCode(80, true));            // down arrow
                layout.Add(45, new RdpScanCode(82, true));            // insert
                layout.Add(46, new RdpScanCode(83, true));            // delete
                layout.Add(48, new RdpScanCode(11, false));           // 0
                layout.Add(49, new RdpScanCode(2, false));            // 1
                layout.Add(50, new RdpScanCode(3, false));            // 2
                layout.Add(51, new RdpScanCode(4, false));            // 3
                layout.Add(52, new RdpScanCode(5, false));            // 4
                layout.Add(53, new RdpScanCode(6, false));            // 5
                layout.Add(54, new RdpScanCode(7, false));            // 6
                layout.Add(55, new RdpScanCode(8, false));            // 7
                layout.Add(56, new RdpScanCode(9, false));            // 8
                layout.Add(57, new RdpScanCode(10, false));           // 9
                layout.Add(65, new RdpScanCode(30, false));           // a qwerty
                                                                    //MapTable.Add(65, new RdpScanCode(16, false));         // a azerty
                layout.Add(66, new RdpScanCode(48, false));           // b
                layout.Add(67, new RdpScanCode(46, false));           // c
                layout.Add(68, new RdpScanCode(32, false));           // d
                layout.Add(69, new RdpScanCode(18, false));           // e
                layout.Add(70, new RdpScanCode(33, false));           // f
                layout.Add(71, new RdpScanCode(34, false));           // g
                layout.Add(72, new RdpScanCode(35, false));           // h
                layout.Add(73, new RdpScanCode(23, false));           // i
                layout.Add(74, new RdpScanCode(36, false));           // j
                layout.Add(75, new RdpScanCode(37, false));           // k
                layout.Add(76, new RdpScanCode(38, false));           // l
                layout.Add(77, new RdpScanCode(50, false));           // m qwerty
                                                                    //MapTable.Add(77, new RdpScanCode(39, false));         // m azerty
                layout.Add(78, new RdpScanCode(49, false));           // n
                layout.Add(79, new RdpScanCode(24, false));           // o
                layout.Add(80, new RdpScanCode(25, false));           // p
                layout.Add(81, new RdpScanCode(16, false));           // q qwerty
                                                                    //MapTable.Add(81, new RdpScanCode(30, false));         // q azerty
                layout.Add(82, new RdpScanCode(19, false));           // r
                layout.Add(83, new RdpScanCode(31, false));           // s
                layout.Add(84, new RdpScanCode(20, false));           // t
                layout.Add(85, new RdpScanCode(22, false));           // u
                layout.Add(86, new RdpScanCode(47, false));           // v
                layout.Add(87, new RdpScanCode(17, false));           // w qwerty
                                                                    //MapTable.Add(87, new RdpScanCode(44, false));         // w azerty
                layout.Add(88, new RdpScanCode(45, false));           // x
                layout.Add(89, new RdpScanCode(44, false));           // y
                layout.Add(90, new RdpScanCode(21, false));           // z qwertz
                                                                    //MapTable.Add(90, new RdpScanCode(17, false));         // z azerty
                layout.Add(91, new RdpScanCode(347, true));           // left window key
                layout.Add(92, new RdpScanCode(348, true));           // right window key
                layout.Add(93, new RdpScanCode(0, true));             // select key
                layout.Add(96, new RdpScanCode(82, false));           // numpad 0
                layout.Add(97, new RdpScanCode(79, false));           // numpad 1
                layout.Add(98, new RdpScanCode(80, false));           // numpad 2
                layout.Add(99, new RdpScanCode(81, false));           // numpad 3
                layout.Add(100, new RdpScanCode(75, false));          // numpad 4
                layout.Add(101, new RdpScanCode(76, false));          // numpad 5
                layout.Add(102, new RdpScanCode(77, false));          // numpad 6
                layout.Add(103, new RdpScanCode(71, false));          // numpad 7
                layout.Add(104, new RdpScanCode(72, false));          // numpad 8
                layout.Add(105, new RdpScanCode(73, false));          // numpad 9
                layout.Add(106, new RdpScanCode(55, false));          // multiply
                layout.Add(107, new RdpScanCode(78, false));          // add
                layout.Add(109, new RdpScanCode(74, false));          // subtract
                layout.Add(110, new RdpScanCode(52, false));          // decimal point
                layout.Add(111, new RdpScanCode(53, true));           // divide
                layout.Add(112, new RdpScanCode(59, false));          // f1
                layout.Add(113, new RdpScanCode(60, false));          // f2
                layout.Add(114, new RdpScanCode(61, false));          // f3
                layout.Add(115, new RdpScanCode(62, false));          // f4
                layout.Add(116, new RdpScanCode(63, false));          // f5
                layout.Add(117, new RdpScanCode(64, false));          // f6
                layout.Add(118, new RdpScanCode(65, false));          // f7
                layout.Add(119, new RdpScanCode(66, false));          // f8
                layout.Add(120, new RdpScanCode(67, false));          // f9
                layout.Add(121, new RdpScanCode(68, false));          // f10
                layout.Add(122, new RdpScanCode(87, false));          // f11
                layout.Add(123, new RdpScanCode(88, false));          // f12
                layout.Add(144, new RdpScanCode(69, false));          // num lock
                layout.Add(145, new RdpScanCode(70, false));          // scroll lock
                layout.Add(186, new RdpScanCode(26, false));          // ü
                layout.Add(187, new RdpScanCode(27, false));          // +*~
                layout.Add(188, new RdpScanCode(51, false));          // comma
                layout.Add(189, new RdpScanCode(53, false));          // dash
                layout.Add(190, new RdpScanCode(52, false));          // period
                layout.Add(191, new RdpScanCode(43, false));          // # '
                layout.Add(192, new RdpScanCode(39, false));          // ö

                layout.Add(219, new RdpScanCode(12, false));          // open bracket
                layout.Add(220, new RdpScanCode(41, false));          // ^ °
                layout.Add(221, new RdpScanCode(13, false));          // ´`
                layout.Add(222, new RdpScanCode(40, false));          // single quote qwerty

                layout.Add(223, new RdpScanCode(0, false));           // ?
                layout.Add(226, new RdpScanCode(86, false));          // lesser than
            }
        }
    }
}