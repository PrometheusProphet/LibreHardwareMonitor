// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public enum ExpectedFailureKind
{
    SensorInitialization,
    SensorRefresh,
    NotificationRegistration,
    NotificationDelivery,
    InventoryRead,
    DriverEvidenceRead
}

public enum ExpectedFailureDataState
{
    NoCurrentTemperatureReadings,
    NotificationsUnavailable,
    NoInventoryResult,
    PartialInventoryWithDriverEvidenceUnknown
}

public enum ExpectedFailureDisposition
{
    StopTemperatureMonitoring,
    ClearCurrentTemperatureReadings,
    MarkNotificationsUnavailable,
    ClearInventoryResult,
    RetainPartialInventoryWithDriverEvidenceUnknown
}

public sealed record ExpectedFailurePresentation(
    ExpectedFailureKind Kind,
    ExpectedFailureDataState DataState,
    ExpectedFailureDisposition Disposition,
    string Headline,
    string Detail);

public static class ExpectedFailurePresentationPolicy
{
    public static ExpectedFailurePresentation For(ExpectedFailureKind kind) => kind switch
    {
        ExpectedFailureKind.SensorInitialization => new(
            kind,
            ExpectedFailureDataState.NoCurrentTemperatureReadings,
            ExpectedFailureDisposition.StopTemperatureMonitoring,
            "Temperature observations unavailable.",
            "Current temperature observations are unavailable. No current CPU or GPU temperature reading is available."),
        ExpectedFailureKind.SensorRefresh => new(
            kind,
            ExpectedFailureDataState.NoCurrentTemperatureReadings,
            ExpectedFailureDisposition.ClearCurrentTemperatureReadings,
            "Temperature observations unavailable.",
            "Current temperature observations are unavailable. Previous values are not current. No current CPU or GPU temperature reading is available."),
        ExpectedFailureKind.NotificationRegistration or ExpectedFailureKind.NotificationDelivery => new(
            kind,
            ExpectedFailureDataState.NotificationsUnavailable,
            ExpectedFailureDisposition.MarkNotificationsUnavailable,
            "Local Windows notifications unavailable.",
            "Temperature observation may continue, but local Windows notification delivery is unavailable and unconfirmed for this session."),
        ExpectedFailureKind.InventoryRead => new(
            kind,
            ExpectedFailureDataState.NoInventoryResult,
            ExpectedFailureDisposition.ClearInventoryResult,
            "Local hardware inventory unavailable.",
            "No inventory result is available because the local read did not complete."),
        ExpectedFailureKind.DriverEvidenceRead => new(
            kind,
            ExpectedFailureDataState.PartialInventoryWithDriverEvidenceUnknown,
            ExpectedFailureDisposition.RetainPartialInventoryWithDriverEvidenceUnknown,
            "Installed driver evidence unavailable.",
            "The PnP inventory snapshot remains available, but installed driver fields and their evidence are unknown."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
