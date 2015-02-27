using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Seen
{
    public class Seen
    {
    }

    [Serializable]
    public abstract class SeenAction
    {
        public string Nick { get; set; }
        public DateTime Date { get; set; }
        public string Channel { get; set; }
    }

    [Serializable]
    public class JoinAction : SeenAction
    {
    }

    [Serializable]
    public class PartAction : SeenAction
    {

    }

    [Serializable]
    public class QuitAction : SeenAction
    {

    }

    [Serializable]
    public class NickAction : SeenAction
    {

    }

}
