﻿using System.Windows;

namespace InjectedWorker.WPF
{
    class WPFControlsHandler : ControlsHandlerBase<DependencyObject>
    {
        public WPFControlsHandler()
        {
            RegisterAction(new GetTypeName());

            RegisterAction(new WPFGetChildren());
            RegisterAction(new WPFGetProperty());
            RegisterAction(new WPFGetRectangle());
            RegisterAction(new WPFGetHandle());
            RegisterAction(new WPFGetParent());
            RegisterAction(new WPFGetControlType());
            RegisterAction(new WPFGetFocusedElement());
            RegisterAction(new WPFSetFocus());
            RegisterAction(new WPFGetName());
        }

        // TODO RunOnUiThread method
    }
}
