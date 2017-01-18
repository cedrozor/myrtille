namespace Myrtille.Mvc.Models
{
    public class DefaultModel
    {
        public DefaultFormData DefaultFormData { get; set; }
        public StartMyrtilleParams StartMyrtilleParams { get; set; }
    }

    public class StartMyrtilleParams
    {
        public string HttpSessionId { get; set; }
        public bool RemoteSessionActive { get; set; }
        public int WebSocketPort { get; set; }
        public int WebSocketPortSecured { get; set; }
        public bool StatEnabled { get; set; }
        public bool DebugEnabled { get; set; }
    }

    public class DefaultFormData
    {
        public string Server { get; set; }
        public string Domain { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Stat { get; set; }
        public string Debug { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }
}
