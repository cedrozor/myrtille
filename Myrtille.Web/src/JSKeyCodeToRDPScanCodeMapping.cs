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

using System.Collections;

namespace Myrtille.Web
{
    public static class JsKeyCodeToRdpScanCodeMapping
    {
        public static Hashtable MapTable { get; private set; }

        static JsKeyCodeToRdpScanCodeMapping()
        {
            // this mapping may not be complete; fill it as needed
            // keys (javascript keycodes) and values (rdp scancodes) are decimals (scancodes are often expressed in hexadecimal). scancode "0" means there is no available translation (at least for now)
            // see http://protocolsofmatrix.blogspot.com/2007/09/javascript-keycode-reference-table-for.html or http://www.webonweboff.com/tips/js/event_key_codes.aspx for js keycodes
            // see http://www.vmware.com/support/ws55/doc/ws_devices_keymap_vscan.html or http://svn.zentific.com/deprecated/rdp/xrdp/rdp-scan-codes.txt for rdp scancodes

            MapTable = new Hashtable();

            MapTable.Add(8, 14);           // backspace
            MapTable.Add(9, 15);           // tab
            MapTable.Add(13, 28);          // enter
            MapTable.Add(16, 42);          // shift
            MapTable.Add(17, 29);          // ctrl
            MapTable.Add(18, 56);          // alt
            MapTable.Add(19, 256);         // pause/break
            MapTable.Add(20, 58);          // caps lock
            MapTable.Add(27, 1);           // escape
            MapTable.Add(32, 57);          // space
            MapTable.Add(33, 73);          // page up
            MapTable.Add(34, 81);          // page down
            MapTable.Add(35, 79);          // end
            MapTable.Add(36, 71);          // home
            MapTable.Add(37, 75);          // left arrow
            MapTable.Add(38, 72);          // up arrow
            MapTable.Add(39, 77);          // right arrow
            MapTable.Add(40, 80);          // down arrow
            MapTable.Add(45, 82);          // insert
            MapTable.Add(46, 83);          // delete
            MapTable.Add(48, 11);          // 0
            MapTable.Add(49, 2);           // 1
            MapTable.Add(50, 3);           // 2
            MapTable.Add(51, 4);           // 3
            MapTable.Add(52, 5);           // 4
            MapTable.Add(53, 6);           // 5
            MapTable.Add(54, 7);           // 6
            MapTable.Add(55, 8);           // 7
            MapTable.Add(56, 9);           // 8
            MapTable.Add(57, 10);          // 9
            MapTable.Add(65, 30);          // a qwerty
            //MapTable.Add(65, 16);        // a azerty
            MapTable.Add(66, 48);          // b
            MapTable.Add(67, 46);          // c
            MapTable.Add(68, 32);          // d
            MapTable.Add(69, 18);          // e
            MapTable.Add(70, 33);          // f
            MapTable.Add(71, 34);          // g
            MapTable.Add(72, 35);          // h
            MapTable.Add(73, 23);          // i
            MapTable.Add(74, 36);          // j
            MapTable.Add(75, 37);          // k
            MapTable.Add(76, 38);          // l
            MapTable.Add(77, 50);          // m qwerty
            //MapTable.Add(77, 39);        // m azerty
            MapTable.Add(78, 49);          // n
            MapTable.Add(79, 24);          // o
            MapTable.Add(80, 25);          // p
            MapTable.Add(81, 16);          // q qwerty
            //MapTable.Add(81, 30);        // q azerty
            MapTable.Add(82, 19);          // r
            MapTable.Add(83, 31);          // s
            MapTable.Add(84, 20);          // t
            MapTable.Add(85, 22);          // u
            MapTable.Add(86, 47);          // v
            MapTable.Add(87, 17);          // w qwerty
            //MapTable.Add(87, 44);        // w azerty
            MapTable.Add(88, 45);          // x
            MapTable.Add(89, 21);          // y
            MapTable.Add(90, 44);          // z qwerty
            //MapTable.Add(90, 17);        // z azerty
            MapTable.Add(91, 347);         // left window key
            MapTable.Add(92, 348);         // right window key
            MapTable.Add(93, 0);           // select key
            MapTable.Add(96, 82);          // numpad 0
            MapTable.Add(97, 79);          // numpad 1
            MapTable.Add(98, 80);          // numpad 2
            MapTable.Add(99, 81);          // numpad 3
            MapTable.Add(100, 75);         // numpad 4
            MapTable.Add(101, 76);         // numpad 5
            MapTable.Add(102, 77);         // numpad 6
            MapTable.Add(103, 71);         // numpad 7
            MapTable.Add(104, 72);         // numpad 8
            MapTable.Add(105, 73);         // numpad 9
            MapTable.Add(106, 55);         // multiply
            MapTable.Add(107, 78);         // add
            MapTable.Add(109, 74);         // subtract
            MapTable.Add(110, 52);         // decimal point
            MapTable.Add(111, 53);         // divide
            MapTable.Add(112, 59);         // f1
            MapTable.Add(113, 60);         // f2
            MapTable.Add(114, 61);         // f3
            MapTable.Add(115, 62);         // f4
            MapTable.Add(116, 63);         // f5
            MapTable.Add(117, 64);         // f6
            MapTable.Add(118, 65);         // f7
            MapTable.Add(119, 66);         // f8
            MapTable.Add(120, 67);         // f9
            MapTable.Add(121, 68);         // f10
            MapTable.Add(122, 87);         // f11
            MapTable.Add(123, 88);         // f12
            MapTable.Add(144, 69);         // num lock
            MapTable.Add(145, 70);         // scroll lock
            MapTable.Add(186, 39);         // semi-colon qwerty
            //MapTable.Add(186, 50);       // semi-colon azerty
            MapTable.Add(187, 13);         // equal sign
            MapTable.Add(188, 51);         // comma
            MapTable.Add(189, 12);         // dash
            MapTable.Add(190, 52);         // period
            MapTable.Add(191, 53);         // forward slash
            MapTable.Add(192, 41);         // grave accent qwerty
            //MapTable.Add(192, 40);       // grave accent azerty
            MapTable.Add(219, 26);         // open bracket
            MapTable.Add(220, 43);         // back slash
            MapTable.Add(221, 27);         // close braket
            MapTable.Add(222, 40);         // single quote qwerty
            //MapTable.Add(222, 41);       // single quote azerty
        }
    }
}