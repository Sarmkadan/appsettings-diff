using Xunit;
using System.Collections.Generic;
using AppsettingsDiff;

namespace AppsettingsDiff;

public class ConfigDifferTests
{
    [Fact]
    public void Diff_ShouldNotReportDifferences_WhenBooleansAreCaseDifferently()
    {
        var detector = new SensitiveKeyDetector();
        var differ = new ConfigDiffer(detector);

        var baseline = new FlatConfig();
        baseline.Values["key"] = "True";

        var target = new FlatConfig();
        target.Values["key"] = "true";

        var result = differ.Diff(baseline, target);

        Assert.False(result.HasDifferences, "Should not report differences for 'True' vs 'true'");
    }

    [Fact]
    public void Diff_ShouldNotReportDifferences_WhenBooleansAreCaseDifferently_AndCaseSensitiveKeysIsTrue()
    {
        var detector = new SensitiveKeyDetector();
        var differ = new ConfigDiffer(detector, caseSensitiveKeys: true);

        var baseline = new FlatConfig();
        baseline.Values["key"] = "True";

        var target = new FlatConfig();
        target.Values["key"] = "true";

        var result = differ.Diff(baseline, target, options: new ConfigDiffOptions { CaseSensitiveKeys = true });

        Assert.False(result.HasDifferences, "Should not report differences for 'True' vs 'true' even with CaseSensitiveKeys=true");
    }
}
