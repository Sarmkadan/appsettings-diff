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

    [Fact]
    public void Diff_ShouldCountUnchangedKeys_ExcludingChangedRemovedAndIgnored()
    {
        var detector = new SensitiveKeyDetector();
        var differ = new ConfigDiffer(detector);

        var baseline = new FlatConfig();
        baseline.Values["Same"] = "1";
        baseline.Values["Changed"] = "a";
        baseline.Values["Removed"] = "x";
        baseline.Values["Ignored"] = "old";

        var target = new FlatConfig();
        target.Values["Same"] = "1";
        target.Values["Changed"] = "b";
        target.Values["Ignored"] = "new";
        target.Values["Added"] = "y";

        var result = differ.Diff(baseline, target, ignoreKeys: ["Ignored"]);

        Assert.Equal(1, result.UnchangedCount);
        Assert.Equal(1, result.CountOf(DiffKind.Added));
        Assert.Equal(1, result.CountOf(DiffKind.Removed));
        Assert.Equal(1, result.CountOf(DiffKind.Changed));
    }
}
