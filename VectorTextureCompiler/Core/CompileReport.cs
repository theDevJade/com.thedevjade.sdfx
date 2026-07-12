using System;
using System.Collections.Generic;

namespace SDFX.VectorTextureCompiler.Core
{
    [Serializable]
    public sealed class CompileReport
    {
        public string generatedAtUtc = string.Empty;
        public string sourcePath = string.Empty;
        public string sourceType = string.Empty;
        public string optimizationProfile = string.Empty;
        public string parserStrictness = string.Empty;
        public string coordinateModel = string.Empty;
        public string rasterAlgorithm = string.Empty;
        public bool buildQuestVariant;

        public PrimitiveCountReport counts = new PrimitiveCountReport();
        public StageTimingReport timings = new StageTimingReport();
        public WarningReport warnings = new WarningReport();
        public List<ParseIssueReport> parseIssues = new List<ParseIssueReport>();
    }

    [Serializable]
    public sealed class PrimitiveCountReport
    {
        public int parsed;
        public int simplified;
        public int resolved;
        public int quantized;
        public int final;
    }

    [Serializable]
    public sealed class StageTimingReport
    {
        public long parseMs;
        public long simplifyMs;
        public long booleanMs;
        public long quantizeMs;
        public long questMs;
        public long gridMs;
        public long bakeMs;
        public long codegenMs;
        public long assetMs;
        public long totalMs;
    }

    [Serializable]
    public sealed class WarningReport
    {
        public int parseWarnings;
        public int parseErrors;
        public int droppedGridReferences;
        public int totalWarnings;
    }

    [Serializable]
    public sealed class ParseIssueReport
    {
        public string severity = string.Empty;
        public string code = string.Empty;
        public string elementName = string.Empty;
        public int lineNumber;
        public string message = string.Empty;
    }
}