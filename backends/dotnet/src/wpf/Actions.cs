using System;
using System.Collections.Generic;

namespace InjectedWorker
{
    internal abstract class ActionBase
    {
        public abstract string Name { get; }

        public abstract Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args) 
            where T: class;


        protected void CheckParamExists(IDictionary<string, dynamic> args, string name)
        {
            if (!args.ContainsKey(name))
            {
                throw new ErrorReplyException(ErrorCodes.MISSING_PARAM, "missing param: " + name);
            }
        }
        protected void CheckValidControlId<T>(dynamic id, ControlsStorage<T> controls)
            where T: class
        {
            if (id < 0)
                throw new ErrorReplyException(ErrorCodes.INVALID_VALUE, "invalid element id (should be >= 0)");
            if (controls != null)
            {
                if (id < 1)
                    throw new ErrorReplyException(ErrorCodes.INVALID_VALUE, "invalid element id (should be >= 1)");

                if (controls.GetControl(id) == null)
                    throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "element not found with ID: " + id);
            }

        }
    }

    internal abstract class GetChildrenAction<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetChildren"; }
        }
    }

    internal abstract class GetParent<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetParent"; }
        }
    }

    internal abstract class GetRectangle<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetRectangle"; }
        }
    }

    internal abstract class GetProperty<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetProperty"; }
        }
    }

    internal abstract class SetProperty<T> : ActionBase
    {
        public override string Name
        {
            get { return "SetProperty"; }
        }

        public static dynamic ConvertType(object value, Type type)
        {
            Type targetType = type;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }
                targetType = Nullable.GetUnderlyingType(targetType);
            }

            return Convert.ChangeType(value, targetType);
        }
    }

    internal abstract class GetHandle<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetHandle"; }
        }
    }

    internal class GetTypeName : ActionBase
    {
        public override string Name
        {
            get { return "GetTypeName"; }
        }

        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            object c = controls.GetControl(args["element_id"]);
            DynamicValueReply reply = new DynamicValueReply(c.GetType().Name);
            return reply;
        }
    }

    internal abstract class GetControlType<T> : ActionBase
    {
        protected Dictionary<Type, string> KnownTypes = new Dictionary<Type, string>();
        protected string DefaultType = "Custom";

        public override string Name
        {
            get { return "GetControlType"; }
        }

        protected virtual string FindControlType(object obj)
        {
            // traverse inheritance hierarchy of the given object
            for (Type t = obj.GetType(); t != null; t = t.BaseType)
            {
                if (KnownTypes.ContainsKey(t))
                {
                    return KnownTypes[t];
                }
            }
            return DefaultType;
        }

    }

    internal abstract class GetFocusedElement<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetFocusedElement"; }
        }
    }

    internal abstract class SetFocus<T> : ActionBase
    {
        public override string Name
        {
            get { return "SetFocus"; }
        }
    }

    internal abstract class GetName<T> : ActionBase
    {
        public override string Name
        {
            get { return "GetName"; }
        }
    }

    internal abstract class InvokeMethod<T> : ActionBase
    {
        public override string Name
        {
            get { return "InvokeMethod"; }
        }
    }

    internal abstract class RaiseEvent<T> : ActionBase
    {
        public override string Name
        {
            get { return "RaiseEvent"; }
        }
    }

}


