using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Runtime.ExceptionServices;

namespace InjectedWorker
{
    interface IRequestHandler
    {
        string ProcessRequest(string jsonRequest);
    }

    class ControlsStorage<T>
        where T : class
    {
        private ObjectIDGenerator IdGenerator = new ObjectIDGenerator();
        private Dictionary<long, T> KnownControls = new Dictionary<long, T>();

        internal long RegisterControl(T control)
        {
            long id = IdGenerator.GetId(control, out bool firstTime);
            if (firstTime)
                KnownControls.Add(id, control);

            return id;
        }

        internal long GetId(T control)
        {
            return IdGenerator.GetId(control, out _);
        }

        internal T GetControl(long id)
        {
            if (KnownControls.ContainsKey(id))
                return KnownControls[id];
            return null;
        }
    }

    abstract class ControlsHandlerBase<T> : IRequestHandler
        where T : class
    {
        private Dictionary<string, ActionBase> RegisteredActions = new Dictionary<string, ActionBase>();
        private ControlsStorage<T> RegisteredControls = new ControlsStorage<T>();

        protected void RegisterAction(ActionBase a)
        {
            RegisteredActions.Add(a.Name, a);
        }

        [HandleProcessCorruptedStateExceptions]
        public string ProcessRequest(string jsonRequest)
        {
            Reply reply = null;

            try
            {
                try
                {
                    var requestParams = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonRequest);
                    if (requestParams.ContainsKey("action"))
                    {
                        string actionName = requestParams["action"];
                        if (RegisteredActions.ContainsKey(actionName))
                        {
                            reply = RegisteredActions[actionName].Run(RegisteredControls, requestParams);
                        }
                        else
                        {
                            throw new ErrorReplyException(ErrorCodes.UNSUPPORTED_ACTION, String.Format("no such action: {0}", actionName));
                        }
                    }
                    else
                    {
                        throw new ErrorReplyException(ErrorCodes.MISSING_PARAM, "param is required: action");
                    }

                }
                catch (JsonReaderException e)
                {
                    throw new ErrorReplyException(ErrorCodes.PARSE_ERROR, e.ToString());
                }
            }
            catch (ErrorReplyException e)
            {
                reply = e.Reply;
            }
            catch (Exception e)
            {
                reply = new ErrorReply(ErrorCodes.RUNTIME_ERROR, e.ToString());
            }

            string jsonString = JsonConvert.SerializeObject(reply.Fields);
            return jsonString;
        }
    } 

}
