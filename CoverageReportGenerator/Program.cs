using System;
using System.IO;
using TodoApi.Tests;

namespace CoverageReportGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🔍 Generating Test Coverage Report...");
            Console.WriteLine();

            try
            {
                // Generate the detailed report
                var report = TestCoverageReport.GenerateDetailedReport();
                
                // Display the report
                Console.WriteLine(report);
                
                // Save to file
                var outputFile = "test-coverage-report.txt";
                File.WriteAllText(outputFile, report);
                Console.WriteLine($"📄 Report saved to: {outputFile}");
                
                // Generate summary
                var summary = TestCoverageReport.GenerateCoverageReport();
                Console.WriteLine();
                Console.WriteLine("📊 QUICK SUMMARY:");
                Console.WriteLine($"   Total Tests: {summary.TotalTests}");
                Console.WriteLine($"   ✅ Passed: {summary.TotalPassed} ({summary.OverallPassRate:F1}%)");
                Console.WriteLine($"   ⏭️ Skipped: {summary.TotalSkipped} ({summary.OverallSkipRate:F1}%)");
                Console.WriteLine($"   ❌ Failed: {summary.TotalFailed} ({summary.OverallFailRate:F1}%)");
                
                // Determine status
                var status = summary.OverallPassRate >= 80 ? "🟢 EXCELLENT" : 
                           summary.OverallPassRate >= 50 ? "🟡 GOOD" : "🔴 NEEDS IMPROVEMENT";
                Console.WriteLine($"   Status: {status}");
                
                Console.WriteLine();
                Console.WriteLine("✅ Coverage report generated successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating coverage report: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
} 