using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AppsettingsDiff;

/// <summary>
/// Tests to verify that all IDiffReportWriter implementations properly redact sensitive values
/// when showSecrets=false, preventing secret leakage through any output format.
/// </summary>
public class DiffReportWriterSecretRedactionTests
{
    private readonly SensitiveKeyDetector _detector = new();
    private const string SensitiveKey = "ConnectionStrings:Default:Password";
    private const string SensitiveValue = "SuperSecret123!";
    private const string SafeKey = "AppSettings:Title";
    private const string SafeValue = "My Application";

    /// <summary>
    /// Creates a DiffResult with both sensitive and non-sensitive entries for testing.
    /// </summary>
    private DiffResult CreateTestDiffResult()
    {
        var result = new DiffResult
        {
            BasePath = "test-baseline.json",
            TargetPath = "test-target.json"
        };

        // Add sensitive entry (password key)
        result.Entries.Add(new DiffEntry
        {
            Kind = DiffKind.Changed,
            Key = SensitiveKey,
            OldValue = "OldPassword123",
            NewValue = SensitiveValue,
            IsSensitive = true // Detector will identify this as sensitive
        });

        // Add non-sensitive entry
        result.Entries.Add(new DiffEntry
        {
            Kind = DiffKind.Changed,
            Key = SafeKey,
            OldValue = "OldTitle",
            NewValue = SafeValue,
            IsSensitive = false
        });

        // Add sensitive entry that matches detector patterns
        result.Entries.Add(new DiffEntry
        {
            Kind = DiffKind.Added,
            Key = "ApiKey:SecretToken",
            NewValue = "sk_live_abc123xyz",
            IsSensitive = true // Will be detected as sensitive by the detector
        });

        return result;
    }

    /// <summary>
    /// Tests that ConsoleDiffReportWriter respects showSecrets=false
    /// </summary>
    [Fact]
    public void ConsoleDiffReportWriter_RedactsSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        var writer = new ConsoleDiffReportWriter(_detector, showSecrets: false);
        using var stringWriter = new StringWriter();

        // Redirect console output
        Console.SetOut(stringWriter);

        // Act
        writer.WriteConsole(result);

        // Assert
        var output = stringWriter.ToString();
        Assert.DoesNotContain(SensitiveValue, output);
        Assert.DoesNotContain("sk_live_abc123xyz", output);
        Assert.Contains("[REDACTED]", output);
    }

    /// <summary>
    /// Tests that ConsoleDiffReportWriter shows secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void ConsoleDiffReportWriter_ShowsSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        var writer = new ConsoleDiffReportWriter(_detector, showSecrets: true);
        using var stringWriter = new StringWriter();

        // Redirect console output
        Console.SetOut(stringWriter);

        // Act
        writer.WriteConsole(result);

        // Assert
        var output = stringWriter.ToString();
        Assert.Contains(SensitiveValue, output);
        Assert.Contains("sk_live_abc123xyz", output);
    }

    /// <summary>
    /// Tests that MarkdownDiffReportWriter respects showSecrets=false
    /// </summary>
    [Fact]
    public void MarkdownDiffReportWriter_RedactsSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new MarkdownDiffReportWriter(_detector, showSecrets: false);

        // Act
        writer.WriteMarkdown(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.DoesNotContain(SensitiveValue, output);
        Assert.DoesNotContain("sk_live_abc123xyz", output);
        Assert.Contains("[REDACTED]", output);
    }

    /// <summary>
    /// Tests that MarkdownDiffReportWriter shows secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void MarkdownDiffReportWriter_ShowsSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new MarkdownDiffReportWriter(_detector, showSecrets: true);

        // Act
        writer.WriteMarkdown(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.Contains(SensitiveValue, output);
        Assert.Contains("sk_live_abc123xyz", output);
    }

    /// <summary>
    /// Tests that HtmlDiffReportWriter respects showSecrets=false
    /// </summary>
    [Fact]
    public void HtmlDiffReportWriter_RedactsSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new HtmlDiffReportWriter(_detector, showSecrets: false);

        // Act
        writer.WriteHtml(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.DoesNotContain(SensitiveValue, output);
        Assert.DoesNotContain("sk_live_abc123xyz", output);
        Assert.Contains("[REDACTED]", output);
    }

    /// <summary>
    /// Tests that HtmlDiffReportWriter shows secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void HtmlDiffReportWriter_ShowsSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new HtmlDiffReportWriter(_detector, showSecrets: true);

        // Act
        writer.WriteHtml(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.Contains(SensitiveValue, output);
        Assert.Contains("sk_live_abc123xyz", output);
    }

    /// <summary>
    /// Tests that JsonPatchDiffReportWriter respects showSecrets=false
    /// This is the critical test - JSON Patch operations contain raw "value" fields
    /// that could leak secrets if not properly redacted.
    /// </summary>
    [Fact]
    public void JsonPatchDiffReportWriter_RedactsSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new JsonPatchDiffReportWriter(_detector, showSecrets: false);

        // Act
        writer.WriteJsonPatch(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.DoesNotContain(SensitiveValue, output);
        Assert.DoesNotContain("sk_live_abc123xyz", output);
        Assert.Contains("[REDACTED]", output);
    }

    /// <summary>
    /// Tests that JsonPatchDiffReportWriter shows secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void JsonPatchDiffReportWriter_ShowsSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new JsonPatchDiffReportWriter(_detector, showSecrets: true);

        // Act
        writer.WriteJsonPatch(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.Contains(SensitiveValue, output);
        Assert.Contains("sk_live_abc123xyz", output);
    }

    /// <summary>
    /// Tests that SummaryJsonDiffReportWriter respects showSecrets=false
    /// </summary>
    [Fact]
    public void SummaryJsonDiffReportWriter_RedactsSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new SummaryJsonDiffReportWriter(_detector, showSecrets: false);

        // Act
        writer.WriteJson(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.DoesNotContain(SensitiveValue, output);
        Assert.DoesNotContain("sk_live_abc123xyz", output);
        Assert.Contains("[REDACTED]", output);
    }

    /// <summary>
    /// Tests that SummaryJsonDiffReportWriter shows secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void SummaryJsonDiffReportWriter_ShowsSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        using var stringWriter = new StringWriter();
        var writer = new SummaryJsonDiffReportWriter(_detector, showSecrets: true);

        // Act
        writer.WriteJson(result, stringWriter);

        // Assert
        var output = stringWriter.ToString();
        Assert.Contains(SensitiveValue, output);
        Assert.Contains("sk_live_abc123xyz", output);
    }

    /// <summary>
    /// Cross-writer test: All writers receive the same DiffResult and all must redact secrets
    /// when showSecrets=false. This ensures consistency across all output formats.
    /// </summary>
    [Fact]
    public void AllWriters_RedactSecrets_WhenShowSecretsIsFalse()
    {
        // Arrange
        var result = CreateTestDiffResult();
        var secretPatterns = new[] { "*secret*", "*password*", "*token*", "*key*", "*api*" };
        var detector = new SensitiveKeyDetector(secretPatterns);

        // Test all writers with showSecrets=false
        var writers = new IDiffReportWriter[]
        {
            new ConsoleDiffReportWriter(detector, showSecrets: false),
            new MarkdownDiffReportWriter(detector, showSecrets: false),
            new HtmlDiffReportWriter(detector, showSecrets: false),
            new JsonPatchDiffReportWriter(detector, showSecrets: false),
            new SummaryJsonDiffReportWriter(detector, showSecrets: false)
        };

        var sensitiveValues = new[] { SensitiveValue, "sk_live_abc123xyz" };

        // Act & Assert for each writer
        foreach (var writer in writers)
        {
            using var stringWriter = new StringWriter();

            // Use appropriate method based on writer type
            switch (writer)
            {
                case ConsoleDiffReportWriter consoleWriter:
                    Console.SetOut(stringWriter);
                    consoleWriter.WriteConsole(result);
                    break;
                case MarkdownDiffReportWriter markdownWriter:
                    markdownWriter.WriteMarkdown(result, stringWriter);
                    break;
                case HtmlDiffReportWriter htmlWriter:
                    htmlWriter.WriteHtml(result, stringWriter);
                    break;
                case JsonPatchDiffReportWriter jsonPatchWriter:
                    jsonPatchWriter.WriteJsonPatch(result, stringWriter);
                    break;
                case SummaryJsonDiffReportWriter summaryJsonWriter:
                    summaryJsonWriter.WriteJson(result, stringWriter);
                    break;
                // All JSON writers are tested directly above
            }

            var output = stringWriter.ToString();

            // Verify no sensitive values leaked
            foreach (var sensitiveValue in sensitiveValues)
            {
                Assert.DoesNotContain(sensitiveValue, output);
            }

            // Verify redaction tokens are present
            Assert.Contains("[REDACTED]", output);
        }
    }

    /// <summary>
    /// Cross-writer test: All writers show secrets when showSecrets=true
    /// </summary>
    [Fact]
    public void AllWriters_ShowSecrets_WhenShowSecretsIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        var secretPatterns = new[] { "*secret*", "*password*", "*token*", "*key*", "*api*" };
        var detector = new SensitiveKeyDetector(secretPatterns);

        // Test all writers with showSecrets=true
        var writers = new IDiffReportWriter[]
        {
            new ConsoleDiffReportWriter(detector, showSecrets: true),
            new MarkdownDiffReportWriter(detector, showSecrets: true),
            new HtmlDiffReportWriter(detector, showSecrets: true),
            new JsonPatchDiffReportWriter(detector, showSecrets: true),
            new SummaryJsonDiffReportWriter(detector, showSecrets: true),
        };

        var sensitiveValues = new[] { SensitiveValue, "sk_live_abc123xyz" };

        // Act & Assert for each writer
        foreach (var writer in writers)
        {
            using var stringWriter = new StringWriter();

            // Use appropriate method based on writer type
            switch (writer)
            {
                case ConsoleDiffReportWriter consoleWriter:
                    Console.SetOut(stringWriter);
                    consoleWriter.WriteConsole(result);
                    break;
                case MarkdownDiffReportWriter markdownWriter:
                    markdownWriter.WriteMarkdown(result, stringWriter);
                    break;
                case HtmlDiffReportWriter htmlWriter:
                    htmlWriter.WriteHtml(result, stringWriter);
                    break;
                case JsonPatchDiffReportWriter jsonPatchWriter:
                    jsonPatchWriter.WriteJsonPatch(result, stringWriter);
                    break;
                case SummaryJsonDiffReportWriter summaryJsonWriter:
                    summaryJsonWriter.WriteJson(result, stringWriter);
                    break;
                // All JSON writers are tested directly above
            }

            var output = stringWriter.ToString();

            // Verify sensitive values are present
            foreach (var sensitiveValue in sensitiveValues)
            {
                Assert.Contains(sensitiveValue, output);
            }
        }
    }

    /// <summary>
    /// Tests that masked redaction works correctly (shows *** instead of [REDACTED])
    /// </summary>
    [Fact]
    public void AllWriters_UseMaskedTokens_WhenMaskSensitiveIsTrue()
    {
        // Arrange
        var result = CreateTestDiffResult();
        var secretPatterns = new[] { "*secret*", "*password*", "*token*", "*key*", "*api*" };
        var detector = new SensitiveKeyDetector(secretPatterns);

        // Test with maskSensitive=true
        var writers = new IDiffReportWriter[]
        {
            new ConsoleDiffReportWriter(detector, showSecrets: false, maskSensitive: true),
            new MarkdownDiffReportWriter(detector, showSecrets: false, maskSensitive: true),
            new HtmlDiffReportWriter(detector, showSecrets: false, maskSensitive: true),
            new JsonPatchDiffReportWriter(detector, showSecrets: false, maskSensitive: true),
            new SummaryJsonDiffReportWriter(detector, showSecrets: false, maskSensitive: true)
        };

        // Act & Assert for each writer
        foreach (var writer in writers)
        {
            using var stringWriter = new StringWriter();

            switch (writer)
            {
                case ConsoleDiffReportWriter consoleWriter:
                    Console.SetOut(stringWriter);
                    consoleWriter.WriteConsole(result);
                    break;
                case MarkdownDiffReportWriter markdownWriter:
                    markdownWriter.WriteMarkdown(result, stringWriter);
                    break;
                case HtmlDiffReportWriter htmlWriter:
                    htmlWriter.WriteHtml(result, stringWriter);
                    break;
                case JsonPatchDiffReportWriter jsonPatchWriter:
                    jsonPatchWriter.WriteJsonPatch(result, stringWriter);
                    break;
                case SummaryJsonDiffReportWriter summaryJsonWriter:
                    summaryJsonWriter.WriteJson(result, stringWriter);
                    break;
            }

            var output = stringWriter.ToString();

            // Verify no sensitive values leaked
            Assert.DoesNotContain(SensitiveValue, output);
            Assert.DoesNotContain("sk_live_abc123xyz", output);

            // Verify masked tokens are present (not [REDACTED])
            Assert.Contains("***", output);
            Assert.DoesNotContain("[REDACTED]", output);
        }
    }
}