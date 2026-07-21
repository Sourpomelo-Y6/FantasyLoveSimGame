#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;

public class AllDataValidatorTests
{
    [Test]
    public void ValidateProjectAssets_CurrentProjectAssetsHaveNoWarnings()
    {
        AllDataValidationReport report = AllDataValidator.ValidateProjectAssets(false);

        Assert.That(report.ValidatorCount, Is.EqualTo(10));
        Assert.That(
            report.Items.Where(item => item.Name != "Save Data" && !item.IsValid)
                .Select(item => item.Name + ": " + item.WarningCount),
            Is.Empty,
            report.CreateDialogMessage());
    }
}
#endif
