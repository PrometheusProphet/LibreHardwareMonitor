// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class CpuTemperatureAccessDiagnostic
{
    public static string ExplainNoReadableSource(bool isAmdCpu, bool isPawnIoInstalled)
    {
        if (!isAmdCpu)
            return "No readable CPU temperature source is available. Required low-level access may be unavailable, or this processor path may be unsupported.";

        if (!isPawnIoInstalled)
        {
            return "PawnIO is not installed. The inherited AMD temperature path requires this low-level driver. " +
                   "Computer Vitals will not install a driver or request elevation automatically.";
        }

        return "PawnIO is installed, but no readable AMD CPU temperature source was returned. " +
               "The driver module may be unavailable, or this processor path may be unsupported.";
    }
}
