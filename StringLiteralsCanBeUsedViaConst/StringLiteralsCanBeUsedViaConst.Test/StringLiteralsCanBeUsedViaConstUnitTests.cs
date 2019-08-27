using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using StringLiteralsCanBeUsedViaConst;

namespace StringLiteralsCanBeUsedViaConst.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void DiagnosticsWasNotCreated()
        {
            var test = @"
                            using System;

                            namespace ConsoleApplication1
                            {
                                class TypeName
                                {
                                    public void RunWorker() 
                                    {
                                        const string worker0Arg = ""worker0"";
                                        
                                        RunWorkerInternal(worker0);
                                        RunWorkerInternal(""Worker1"");
                                        RunWorkerInternal(""Worker2"");
                                        RunWorkerInternal(""Worker3"");
                                        RunWorkerInternal(""Worker4"");
                                    }
                                    
                                    private void RunWorkerInternal(string workerName)
                                    {
                                        Console.WriteLine($""Worker {workerName} successfully started"");
                                    }
                                }
                            }"; 

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void DiagnosticsWasCreatedAndFixed()
        {
            var test = @"
                            using System;

                            namespace ConsoleApplication1
                            {
                                class TypeName
                                {
                                    public void RunWorker() 
                                    {
                                        const string worker0Arg = ""Worker0"";
                                        
                                        RunWorkerInternal(worker0);
                                        RunWorkerInternal(""Worker1"");
                                        RunWorkerInternal(""Worker2"");
                                        RunWorkerInternal(""Worker3"");
                                        RunWorkerInternal(""Worker1"");
                                    }
                                    
                                    private void RunWorkerInternal(string workerName)
                                    {
                                        Console.WriteLine($""Worker {workerName} successfully started"");
                                    }
                                }
                            }";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "StringLiteralsCanBeUsedViaConst",
                    Message = "String literals sequence can be replaced by const.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 13, 59),
                        }
                },
                new DiagnosticResult
                {
                    Id = "StringLiteralsCanBeUsedViaConst",
                    Message = "String literals sequence can be replaced by const.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations =
                        new[]
                        {
                            new DiagnosticResultLocation("Test0.cs", 16, 59),
                        }
                },
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
                            using System;

                            namespace ConsoleApplication1
                            {
                                class TypeName
                                {
        private const string _someArg = ""Worker1"";

        public void RunWorker() 
                                    {
                                        const string worker0Arg = ""Worker0"";
                                        
                                        RunWorkerInternal(worker0);
                                        RunWorkerInternal(_someArg);
                                        RunWorkerInternal(""Worker2"");
                                        RunWorkerInternal(""Worker3"");
                                        RunWorkerInternal(_someArg);
                                    }
                                    
                                    private void RunWorkerInternal(string workerName)
                                    {
                                        Console.WriteLine($""Worker {workerName} successfully started"");
                                    }
                                }
                            }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new StringLiteralsCanBeUsedViaConstCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StringLiteralsCanBeUsedViaConstAnalyzer();
        }
    }
}
