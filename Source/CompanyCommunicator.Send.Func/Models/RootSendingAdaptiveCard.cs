using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Models
{
    public class Body
    {
        public string type { get; set; }
        public string size { get; set; }
        public string weight { get; set; }
        public string text { get; set; }
        public bool wrap { get; set; }
        public bool separator { get; set; }
        public string url { get; set; }
        public string altText { get; set; }
        public string spacing { get; set; }
        public bool? isVisible { get; set; }
    }

    public class Action
    {
        public string type { get; set; }
        public string url { get; set; }
        public string title { get; set; }
    }

    public class RootSendingAdaptiveCard
    {
        public string type { get; set; }
        public string version { get; set; }
        public List<Body> body { get; set; }
        public List<Action> actions { get; set; }
    }
}
