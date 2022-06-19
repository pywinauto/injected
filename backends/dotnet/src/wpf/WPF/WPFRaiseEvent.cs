using System;
using System.Collections.Generic;
using System.Windows;

namespace InjectedWorker.WPF { 
    class WPFRaiseEvent : RaiseEvent
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
            CheckParamExists(args, "name");

            object c = controls.GetControl(args["element_id"]);

            if (!(c is UIElement))
            {
                throw new ErrorReplyException(ErrorCodes.UNSUPPORTED_ACTION, "This element is not UIElement");
            }

            bool isFound = false;
            for (Type t = c.GetType(); t != null; t = t.BaseType)
            {
                RoutedEvent[] events = EventManager.GetRoutedEventsForOwner(t);
                if (events != null)
                {
                    RoutedEvent e = Array.Find(events, x => x.Name == args["name"]);
                    if (e != null) 
                    {
                        isFound = true;
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            (c as UIElement).RaiseEvent(new RoutedEventArgs(e));
                        });
                    }
                }
            }
            if (!isFound)
                throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "can not find such event");
            return new Reply(ErrorCodes.OK);
        }

    }
}
