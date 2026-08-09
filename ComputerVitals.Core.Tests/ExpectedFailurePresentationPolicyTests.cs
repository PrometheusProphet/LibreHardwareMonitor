// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Reflection;
using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public class ExpectedFailurePresentationPolicyTests
{
    [TestMethod]
    public void For_ReturnsTheRequiredTypedStateAndDisposition()
    {
        (ExpectedFailureKind Kind, ExpectedFailureDataState State, ExpectedFailureDisposition Disposition)[] expected =
        [
            (ExpectedFailureKind.SensorInitialization, ExpectedFailureDataState.NoCurrentTemperatureReadings, ExpectedFailureDisposition.StopTemperatureMonitoring),
            (ExpectedFailureKind.SensorRefresh, ExpectedFailureDataState.NoCurrentTemperatureReadings, ExpectedFailureDisposition.ClearCurrentTemperatureReadings),
            (ExpectedFailureKind.NotificationRegistration, ExpectedFailureDataState.NotificationsUnavailable, ExpectedFailureDisposition.MarkNotificationsUnavailable),
            (ExpectedFailureKind.NotificationDelivery, ExpectedFailureDataState.NotificationsUnavailable, ExpectedFailureDisposition.MarkNotificationsUnavailable),
            (ExpectedFailureKind.InventoryRead, ExpectedFailureDataState.NoInventoryResult, ExpectedFailureDisposition.ClearInventoryResult),
            (ExpectedFailureKind.DriverEvidenceRead, ExpectedFailureDataState.PartialInventoryWithDriverEvidenceUnknown, ExpectedFailureDisposition.RetainPartialInventoryWithDriverEvidenceUnknown)
        ];

        foreach ((ExpectedFailureKind kind, ExpectedFailureDataState expectedState, ExpectedFailureDisposition expectedDisposition) in expected)
        {
            ExpectedFailurePresentation presentation = ExpectedFailurePresentationPolicy.For(kind);

            Assert.AreEqual(kind, presentation.Kind);
            Assert.AreEqual(expectedState, presentation.DataState);
            Assert.AreEqual(expectedDisposition, presentation.Disposition);
            Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.Headline));
            Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.Detail));
        }
    }

    [TestMethod]
    public void SensorFailures_DeclareNoCurrentTemperatureReadings()
    {
        ExpectedFailurePresentation initialization = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.SensorInitialization);
        ExpectedFailurePresentation refresh = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.SensorRefresh);

        StringAssert.Contains(initialization.Detail, "No current CPU or GPU temperature reading is available.");
        StringAssert.Contains(refresh.Detail, "Previous values are not current.");
    }

    [TestMethod]
    public void NotificationFailures_RemainUnavailableAndUnconfirmed()
    {
        ExpectedFailurePresentation presentation = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.NotificationDelivery);

        StringAssert.Contains(presentation.Detail, "Temperature observation may continue");
        StringAssert.Contains(presentation.Detail, "unavailable and unconfirmed for this session");
        Assert.DoesNotContain("received", presentation.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("will receive", presentation.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void InventoryAndDriverFailures_RemainDistinct()
    {
        ExpectedFailurePresentation inventory = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.InventoryRead);
        ExpectedFailurePresentation driver = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.DriverEvidenceRead);

        StringAssert.Contains(inventory.Detail, "local read did not complete");
        StringAssert.Contains(driver.Detail, "PnP inventory snapshot remains available");
        StringAssert.Contains(driver.Detail, "driver fields and their evidence are unknown");
    }

    [TestMethod]
    public void Policy_AcceptsOnlyTypedFailureKindsAndProvidesNoRawFailureChannel()
    {
        MethodInfo method = typeof(ExpectedFailurePresentationPolicy).GetMethod(nameof(ExpectedFailurePresentationPolicy.For))!;
        string[] forbiddenTerms = ["exception", "retry", "log", "persistence", "dump", "report", "telemetry", "path", "account"];

        CollectionAssert.AreEqual(new[] { typeof(ExpectedFailureKind) }, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.IsFalse(typeof(ExpectedFailurePresentation).GetProperties().Any(property => property.PropertyType == typeof(Exception)));
        foreach (ExpectedFailureKind kind in Enum.GetValues<ExpectedFailureKind>())
        {
            ExpectedFailurePresentation presentation = ExpectedFailurePresentationPolicy.For(kind);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, presentation.Headline, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(forbiddenTerm, presentation.Detail, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
