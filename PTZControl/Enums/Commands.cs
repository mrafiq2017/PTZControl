using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTZControl.Enums
{
    public enum Commands
    {
        TiltUp,
        TiltDown,
        PanLeft,
        PanRight,
        Stop,
        IRDigitalZoom,
        IRManualZoom,
        IRZoomIn,
        IRZoomOut,
        IRNarrowFOV,
        IRWideFOV,
        DTVManualZoom,
        DTVZoomIn,
        DTVZoomOut,
        DTVWideFOV,
        DTVNarrowFOV,
        IRFocus,
        IRAutoFocus,
        DTVFocus,
        DTVAutoFocus,
        DTVZoomPos,
        IRZoomPos,
        EnableFilters,
        DisableFilters,
        DefaultFilters
    }
}
