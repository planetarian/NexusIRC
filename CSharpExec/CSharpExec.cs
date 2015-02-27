using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleTest;
using Nexus;
using Nexus.Messages;

namespace CSharpExec
{
    public class CSharpExec : NexusComponent
    {
        public static Dictionary<string, object> State = new Dictionary<string, object>();
        public static int CommandId;
        private static string methodName;
        private static string returnType;
        private static string parameters;
        private static bool defineMode;

        public CSharpExec()
        {
            RegisterListener<IRCCommandEvent>(Execute);
        }

        private void Reply(IRCMessageEvent ev, string message)
        {
            CallMethod("IRC.Reply", ev, message);
        }

        private void Execute(IRCCommandEvent ev)
        {
            if (ev.Command != "cs") return;

            if (ev.Sender.Substring(0, 4) != "piro") return;


            if (methodName == null)
                methodName = "m_" + CommandId;

            //Console.Write((defineMode ? methodName : "") + "> ");
            //string src = Console.ReadLine();
            string src = ev.Body;
            Debug.Assert(src != null, "src != null");

            if (Regex.IsMatch(src, Rx.Cancel, RegexOptions.Compiled))
            {
                DynamicCodeManager.CancelMethod();
                Reply(ev, "Cancelled method" + (defineMode ? " " + methodName : "") + ".");
                defineMode = false;
                returnType = null;
                parameters = null;
                methodName = null;
                return;
            }

            // Ready to add a new method
            if (DynamicCodeManager.Ready)
            {
                Match match = Regex.Match(src, Rx.Define, RegexOptions.Compiled);
                if (match.Success)
                {
                    string tempMethodName = match.Groups[Rx.MethodNameId].Value;
                    if (match.Groups[Rx.RemoveCommandId].Success)
                    {
                        if (DynamicCodeManager.HasMethod(tempMethodName))
                        {
                            DynamicCodeManager.RemoveMethod(tempMethodName);
                            Reply(ev, "Method " + tempMethodName + " removed.");
                        }
                        else
                        {
                            Reply(ev, "Method " + tempMethodName + " does not exist.");
                        }
                    }
                    else
                    {
                        if (DynamicCodeManager.HasMethod(tempMethodName))
                        {
                            Reply(ev, "Method " + tempMethodName + " already exists.");
                            methodName = null;
                        }
                        else
                        {
                            methodName = tempMethodName;
                            if (match.Groups[Rx.ParamsId].Success)
                                parameters = match.Groups[Rx.ParamsId].Value;
                            if (match.Groups[Rx.ReturnTypeId].Success)
                                returnType = match.Groups[Rx.ReturnTypeId].Value;
                            defineMode = true;
                        }
                    }
                    return;
                }
            }

            try
            {
                DynamicCodeManager.AddMethod(methodName, src, parameters, returnType);
            }
            catch (Exception ex)
            {
                Reply(ev, ex.Message);
                return;
            }

            if (!DynamicCodeManager.Ready) return;

            if (defineMode)
            {
                Reply(ev, "Method " + methodName + " defined.");
            }
            else
            {
                var returnVal = DynamicCodeManager.InvokeMethod(methodName);
                if (!defineMode)
                    DynamicCodeManager.RemoveMethod(methodName);

                Reply(ev, "Output: " + (returnVal ?? "null"));
            }

            CommandId++;
            methodName = null;
            returnType = null;
            parameters = null;
            defineMode = false;

        }
    }

    public struct Rx
    {
        public const string Cancel = @"^cancel;?$";

        public const string ReturnTypeId = "rettype";
        public const string ParamsId = "params";
        public const string MethodNameId = "name";
        public const string RemoveCommandId = "rem";
        public const string DefineCommandId = "def";

        public const string Identifier = @"[a-zA-Z_][\w_]*";
        public const string TypeFudgedInner = @"[a-zA-Z_0-9<>\[\],\.]+";

        public const string TypeFudged = // Can recurse generics, unreliably.
            Identifier + @"(\." + Identifier + @")*"
            + @"(<\s*" + TypeFudgedInner + @"(\s*,\s*" + TypeFudgedInner + @")*\s*>|\[,*\](\[,*\])*)?";
        public const string Type = // Cannot recurse generics.
            Identifier + @"(\." + Identifier + @")*"
            + @"(<\s*" + Identifier + @"(\s*,\s*" + Identifier + @")*\s*>|\[,*\](\[,*\])*)?";

        public const string Param = @"((ref|out|params)\s+)?" + TypeFudged + @"\s+" + Identifier;
        public const string Params = Param + @"(\s*,\s*" + Param + @")*";

        public const string Define =
            @"^\s*(define|create|method)\s+"
            + @"((?<" + ReturnTypeId + @">" + TypeFudged + @")\s+)?"
            + @"(?<" + MethodNameId + @">" + Identifier + @")"
            + @"(\s*\(\s*(?<" + ParamsId + @">" + Params + @")\s*\))?\s*$";
    }
}
