using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Myrtille.Web
{
    public class AdditionalControls
    {
        public bool LoginControlsIsVisible { get; set; }
        public bool ToolBarIsVisible { get; set; }
        public string ServerAddress { get; set; }
        public string StatValue { get; set; }
        public bool StatIsDisabled { get; set; }
        public string DebugValue { get; set; }
        public bool DebugIsDisabled { get; set; }
        public string BrowserValue { get; set; }
        public bool BrowserIsDisabled { get; set; }
        public string ScaleValue { get; set; }
        public bool ScaleIsDisabled { get; set; }

        public bool KeyboardIsDisabled { get; set; }
        public bool ClipboardIsDisabled { get; set; }
        public bool FilesIsDisabled { get; set; }
        public bool CadIsDisabled { get; set; }
        public bool DisconnectIsDisabled { get; set; }
        public bool MrcIsDisabled { get; set; }
    }
}