// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Test.Execution.EngineCommands;
using Microsoft.Test.Execution.Logging;
using System.Collections.ObjectModel;

namespace Microsoft.Test.Execution
{
    internal class ExecutionComponents
    {
        internal DebuggingEngineCommand DebuggingEngine { get; set; }
        internal LoggingMediator LoggingMediator { get; set; }
    }

    /// <summary>
    /// ExecutionEngine is responsible for running a set of tests
    /// </summary>
    internal static class ExecutionEngine
    {
        #region Public Static Method

        /// <summary>
        /// Executes Tests
        /// </summary>
        public static void Execute(ExecutionSettings settings)
        {
            Stack<ICleanableCommand> cleanupCommands = new Stack<ICleanableCommand>();
            try
            {
                //Elevation Service is hitting Error #5 "Access Denied" in XP - We don't have run time dependencies on it right now, so disabling.
                //cleanupCommands.Push(ElevationServiceCommand.Apply(infraBinariesDirectory));

                cleanupCommands.Push(LogDirectoryCommand.Apply(settings.LogFilesPath, settings.SkipDxDiag));

                ExecutionComponents executionComponents = new ExecutionComponents();
                executionComponents.DebuggingEngine = DebuggingEngineCommand.Apply(settings.InfraBinariesDirectory, settings.JitDebuggerCommand);
                cleanupCommands.Push(executionComponents.DebuggingEngine);
                ExecutionEventLog.RecordStatus("Creating LoggingMediator.");
                executionComponents.LoggingMediator = new LoggingMediator(settings.DebugTests); //Consider using dispose pattern.
                executionComponents.LoggingMediator.StartService(executionComponents.DebuggingEngine, settings.Tests.TestCollection.Count(record => record.ExecutionEnabled));

                // TODO: Remove this.  It was used to deploy to the GAC and then cleanup.  There is no GAC in .NET Core.
                //cleanupCommands.Push(RunStateCommand.Apply(settings.TestBinariesDirectory));
                cleanupCommands.Push(ExecutionGroupLogCommand.Apply("InfraExecution", settings.LogFilesPath, executionComponents.LoggingMediator));
                if (settings.CodeCoverageEnabled)
                {
                    cleanupCommands.Push(MergeCodeCoverageDataCommand.Apply(settings.LogFilesPath));
                }
                cleanupCommands.Push(TemporaryDirectoryCommand.Apply(settings.ExecutionRootDirectory));
                cleanupCommands.Push(MoveWindowCommand.Apply());
                cleanupCommands.Push(ExecutionEventLog.Apply(settings.LogFilesPath, !settings.ContinueExecution));

                try
                {
                    ExecuteTestStateGroups(settings, executionComponents);

                }
                catch (Exception e)
                {
                    ExecutionEventLog.RecordException(e);
                }
                finally
                {
                    ExecutionEventLog.RecordStatus("Ending Test Sequence.");
                    ExecutionEventLog.RecordStatus("Shutting down test logging system.");
                    executionComponents.LoggingMediator.StopService();
                }
            }
            finally
            {
                Cleanup(cleanupCommands);
                Console.WriteLine("Test Execution has finished.\n");
            }
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Bucketize collection of tests by matching State management needs, and run through each group.
        /// Notion of Execution group + a tracking counter variable seem to be critical elements.
        /// This (and supportfiles one) can likely get generalized+factored well to the ExecutionGroup class with some thought.
        /// </summary>
        private static void ExecuteTestStateGroups(ExecutionSettings settings, ExecutionComponents executionComponents)
        {
            ExecutionEventLog.RecordStatus("Running Test Sequence.");

            List<TestRecord> enabledTests = new List<TestRecord>(settings.Tests.TestCollection.Where(test => (test.ExecutionEnabled == true)));

            int stateGroupIndex = 0;
            // NOTE: Hash method for Deployments is order sensitive.
            IEnumerable<List<TestRecord>> testGroups = ExecutionGrouper.Bucketize(enabledTests,
                ExecutionGroupingLevel.SharedStateManagement,
                x => x.TestInfo.Area + x.TestInfo.Deployments.ToCommaSeparatedList());

            foreach (List<TestRecord> stateManagementGroup in testGroups)
            {
                ExecutionEventLog.RecordStatus("Running State Group # " + stateGroupIndex + " of " + testGroups.Count());
                Stack<ICleanableCommand> stateCleanupCommands = new Stack<ICleanableCommand>();
                ExecutionGroupRecord stateGroupRecord = ExecutionGroupRecord.Begin(ExecutionGroupType.State, stateManagementGroup[0].TestInfo.Area);
                try
                {
                    settings.Tests.ExecutionGroupRecords.Add(stateGroupRecord);
                    DirectoryInfo stateLogPath = settings.DetermineTestLogDirectory(settings.DetermineGroupPath(stateGroupIndex));
                    ExecutionGroupLogCommand command = ExecutionGroupLogCommand.Apply("StateManagement", stateLogPath, executionComponents.LoggingMediator);
                    stateCleanupCommands.Push(command);

                    stateCleanupCommands.Push(ExecutionStateCommand.Apply(stateManagementGroup.First(), settings.TestBinariesDirectory));
                    ExecuteTestSupportFileGroups(settings, stateManagementGroup, stateGroupRecord, stateGroupIndex, executionComponents);
                }
                catch (Exception e)
                {
                    ExecutionEventLog.RecordException(e);
                }
                finally
                {
                    Cleanup(stateCleanupCommands);
                    stateGroupRecord.End();
                    stateGroupIndex++;
                }
            }
        }

        /// <summary>
        /// Bucketize and run through groups of matching support file needs.
        /// </summary>
        private static void ExecuteTestSupportFileGroups(ExecutionSettings settings, List<TestRecord> stateManagementGroup, ExecutionGroupRecord stateGroupRecord, int stateGroupIndex, ExecutionComponents components)
        {
            int supportFileGroupIndex = 0;

            //Bucketize
            // NOTE: Hash method for SupportFiles is order sensitive.
            IEnumerable<List<TestRecord>> testGroups = ExecutionGrouper.Bucketize(
                stateManagementGroup,
                ExecutionGroupingLevel.SharedSupportFiles,
                x => HashSupportFileGroup(x));

            foreach (List<TestRecord> supportFileGroup in testGroups)
            {
                Stack<ICleanableCommand> filecleanupCommands = new Stack<ICleanableCommand>();

                ExecutionGroupRecord supportFileGroupRecord = ExecutionGroupRecord.Begin(ExecutionGroupType.Files, stateGroupRecord.Area);
                stateGroupRecord.ExecutionGroupRecords.Add(supportFileGroupRecord);
                DirectoryInfo executionDirectory = settings.DetermineTestExecutionDirectory(settings.DetermineGroupPath(stateGroupIndex, supportFileGroupIndex));
                DirectoryInfo logDirectory = settings.DetermineTestLogDirectory(settings.DetermineGroupPath(stateGroupIndex, supportFileGroupIndex));
                PrepLogDirectory(logDirectory);//HACK: Ideally logging can guarantee this in final configuration.
                if (GetCachedExecutionResult(settings, supportFileGroup, logDirectory, components))
                {
                    ExecutionEventLog.RecordStatus("Successfully retrieved previously stored execution result. ");
                }
                else
                {
                    ExecutionEventLog.RecordStatus("Applying Support Files and Executing.");
                    ExecutionGroupLogCommand command = ExecutionGroupLogCommand.Apply("SupportFiles", logDirectory, components.LoggingMediator);
                    filecleanupCommands.Push(command);
                    filecleanupCommands.Push(DesktopSnapshotCommand.Apply(logDirectory));
                    // Create temporary directory, but if we are using a fixed test execution directory, don't delete it if it was pre-existing.
                    filecleanupCommands.Push(TemporaryDirectoryCommand.Apply(executionDirectory, settings.FixedTestExecutionDirectory != null));
                    filecleanupCommands.Push(SupportFileCommand.Apply(supportFileGroup, settings.TestBinariesDirectory, executionDirectory));
                    filecleanupCommands.Push(BackupRecordsCommand.Apply(supportFileGroup, logDirectory));
                    filecleanupCommands.Push(ProcessLogsCommand.Apply(supportFileGroup, components.LoggingMediator));
                    try
                    {
                        ExecuteUniformTestGroup(settings, supportFileGroup, supportFileGroupRecord, stateGroupIndex, supportFileGroupIndex, components);
                    }
                    catch (Exception e)
                    {
                        ExecutionEventLog.RecordException(e);
                    }
                    finally
                    {
                        Cleanup(filecleanupCommands);
                    }
                }
                supportFileGroupRecord.End();
                supportFileGroupIndex++;
            }
        }

        private static string HashSupportFileGroup(TestRecord test)
        {
            // If an explicit execution group name was specified, hash to that
            // name such that all tests in that execution group will stay together.
            if (!String.IsNullOrEmpty(test.TestInfo.ExecutionGroup))
            {
                return test.TestInfo.ExecutionGroup;
            }
            // If no explicit execution group name was specified, we'll hash based on the support files,
            // such that tests will be bucketed based on sharing the same set of support files.
            else
            {
                return test.TestInfo.SupportFiles.ToCommaSeparatedList();
            }
        }

        /// <summary>
        /// Retrieve previously generated execution results, if they are present.
        /// </summary>
        private static bool GetCachedExecutionResult(ExecutionSettings settings, List<TestRecord> executionGroup, DirectoryInfo executionLogPath, ExecutionComponents components)
        {
            List<TestRecord> previousResults = null;
            if (settings.ContinueExecution)
            {
                previousResults = ExecutionBackupStore.LoadIntermediateTestRecords(executionLogPath);
            }
            if (previousResults != null)
            {
                //Confirm we have a match for all  the tests
                if (executionGroup.Count != previousResults.Count)
                {
                    return false;
                }

                for (int i = 0; i < executionGroup.Count; i++)
                {
                    if (executionGroup[i].TestInfo.Name != previousResults[i].TestInfo.Name)
                    {
                        return false; //Note: We should probably delete previous log results
                    }
                }

                for (int i = 0; i < executionGroup.Count; i++)
                {
                    TestRecord test = executionGroup[i];
                    TestRecord previous = previousResults[i];

                    test.Log = previous.Log;
                    foreach (FileInfo file in previous.LoggedFiles)
                    {
                        test.LoggedFiles.Add(file);
                    }
                    test.Machine = previous.Machine;
                    foreach (VariationRecord record in previous.Variations)
                    {
                        test.Variations.Add(record);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void ExecuteUniformTestGroup(ExecutionSettings settings, List<TestRecord> uniformTestGroup, ExecutionGroupRecord fileGroupRecord, int stateGroupIndex, int supportFileGroupIndex, ExecutionComponents components)
        {
            string groupPath = settings.DetermineGroupPath(stateGroupIndex, supportFileGroupIndex);
            DirectoryInfo executionDirectory = settings.DetermineTestExecutionDirectory(groupPath);
            string executionLabel = "(" + stateGroupIndex + "," + supportFileGroupIndex + ")";

            IEnumerable<List<TestRecord>> testGroups;
            if (settings.CodeCoverageEnabled) // When using Code Coverage, we do not group test Appdomains together
            {
                testGroups = ExecutionGrouper.MakeGroupPerTest(uniformTestGroup);
            }
            else //Normal logic: Bucketize based on support for Shared App domains and driver
            {
                testGroups = ExecutionGrouper.Bucketize(
                    uniformTestGroup,
                    ExecutionGroupingLevel.SharedAppDomains,
                    x => x.TestInfo.Driver.Executable + x.TestInfo.DriverParameters["SecurityLevel"]);
            }

            int testCount = 0;
            foreach (List<TestRecord> tests in testGroups)
            {
                ExecutionGroupRecord appDomainRecord = ExecutionGroupRecord.Begin(ExecutionGroupType.AppDomain, fileGroupRecord.Area);
                fileGroupRecord.ExecutionGroupRecords.Add(appDomainRecord);

                TestInfo first = tests.First().TestInfo;

                //Tests which allow grouping & use STI get to be run as a group during normal runs.
                //All tests must run separately with Code coverage runs.
                bool runAsGroup = !settings.CodeCoverageEnabled &&
                                    first.Driver.Executable.Equals("Sti.exe", StringComparison.OrdinalIgnoreCase) && first.ExecutionGroupingLevel >= ExecutionGroupingLevel.SharedAppDomains;

                TimeSpan processDuration;
                if (runAsGroup)//STI gets special treatment
                {
                    ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "Starting Shared AppDomain Test Process #{0}-{1}", groupPath, testCount));

                    DirectoryInfo testLogDirectory = settings.DetermineTestLogDirectory(settings.DetermineGroupPath(stateGroupIndex, supportFileGroupIndex));
                    processDuration = ExecuteSti(settings, tests, components, executionDirectory, testLogDirectory);

                    ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "Finished Shared AppDomain Test Process #{0}-{1}", groupPath, testCount));
                }

                else
                {
                    ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "Starting Test Process #{0}-{4} Runtests.cmd /Area={1} /Name={3} /Subarea={2}", groupPath, first.Area, first.SubArea, first.Name, testCount));

                    if (tests.Count > 1)
                    {
                        throw new InvalidDataException("[Infra bug]Tests should be hashed into lists of individual tests.");
                    }
                    DirectoryInfo testLogDirectory = settings.DetermineTestLogDirectory(settings.DetermineGroupPath(stateGroupIndex, supportFileGroupIndex, testCount));
                    PrepLogDirectory(testLogDirectory);//HACK: Ideally logging can guarantee this in final configuration.
                    processDuration = ExecuteTest(settings, tests.First(), components, executionDirectory, testLogDirectory);

                    ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "Finished Test Process #{0}-{4} /Area={1} /Name={3} /Subarea={2}", groupPath, first.Area, first.SubArea, first.Name, testCount));
                }
                Reporting.ReportingUtilities.ApplyProcessCost(tests, processDuration);
                appDomainRecord.End();
                testCount++;
            }
        }

        /// <summary>
        /// Runs the specified list of tests in STI.
        /// </summary>
        private static TimeSpan ExecuteSti(ExecutionSettings settings, List<TestRecord> tests, ExecutionComponents components, DirectoryInfo executionDirectory, DirectoryInfo executionLogPath)
        {
            TimeSpan processDuration = TimeSpan.Zero;
            Stack<ICleanableCommand> cleanupCommands = new Stack<ICleanableCommand>();
            try
            {
                cleanupCommands.Push(ListenToTestsCommand.Apply(tests, components.LoggingMediator, executionLogPath, settings.DebugTests));

                //Note: Execution Group model should not be applied with Code Coverage runs. Semantics of a test coverage data get murky there when some parts fail.

                //Perform the following set of commands, and unwind at cleanup in reverse order, regardless of success/failure
                ExecutionEventLog.RecordStatus("Starting Test.");
                processDuration = DriverLauncher.Launch(settings, tests, executionDirectory, components.DebuggingEngine);

                if (settings.RerunFailures && ShouldRerun(tests))
                {
                    // Run cleanup to end any active logs...
                    // *************************************
                    // ATTENTION!
                    // This behavior (clear cleanup commands, then re-push them) used for re-run has only been tested to work with the CodeCoverage and ListenToTestsCommands.
                    // Since the stack is instantiated here and used here, this is a very contained assumption.
                    // If you need to add more types of cleanup commands, please ensure they work with RerunFailures.
                    // *************************************
                    Cleanup(cleanupCommands);
                    cleanupCommands.Clear();
                    cleanupCommands.Push(ListenToTestsCommand.Apply(tests, components.LoggingMediator, executionLogPath, settings.DebugTests));
                    ClearResult(tests);
                    ExecutionEventLog.RecordStatus("Not all test variations passed but no crashes recorded, attempting to re-run. (ExecuteSti Variation)");
                    processDuration = processDuration.Add(DriverLauncher.Launch(settings, tests, executionDirectory, components.DebuggingEngine));
                }
                ExecutionEventLog.RecordStatus("Test Execution sequence completed normally.");
            }
            catch (Exception e)
            {
                ExecutionEventLog.RecordException(e);
            }
            finally
            {
                Cleanup(cleanupCommands);
            }
            return processDuration;
        }

        /// <summary>
        /// Runs the specified test
        /// </summary>
        private static TimeSpan ExecuteTest(ExecutionSettings settings, TestRecord test, ExecutionComponents components, DirectoryInfo executionDirectory, DirectoryInfo executionLogPath)
        {
            TimeSpan processDuration = TimeSpan.Zero;
            Stack<ICleanableCommand> cleanupCommands = new Stack<ICleanableCommand>();
            try
            {
                List<TestRecord> tests = new List<TestRecord>();
                tests.Add(test);

                //Perform the following set of commands, and unwind at cleanup in reverse order, regardless of success/failure
                cleanupCommands.Push(ListenToTestsCommand.Apply(tests, components.LoggingMediator, executionLogPath, settings.DebugTests));
                if (settings.CodeCoverageEnabled)
                {
                    cleanupCommands.Push(GatherTestCodeCoverageCommand.Apply(test, executionLogPath));
                }

                ExecutionEventLog.RecordStatus("Starting Test.");
                processDuration = DriverLauncher.Launch(settings, tests, executionDirectory, components.DebuggingEngine);

                if (settings.RerunFailures && ShouldRerun(tests))
                {
                    // Run cleanup to end any active logs...
                    // *************************************
                    // ATTENTION!
                    // This behavior (clear cleanup commands, then re-push them) used for re-run has only been tested to work with the CodeCoverage and ListenToTestsCommands.
                    // Since the stack is instantiated here and used here, this is a very contained assumption.
                    // If you need to add more types of cleanup commands, please ensure they work with RerunFailures.
                    // *************************************
                    Cleanup(cleanupCommands);
                    cleanupCommands.Clear();
                    //Perform the following set of commands, and unwind at cleanup in reverse order, regardless of success/failure
                    cleanupCommands.Push(ListenToTestsCommand.Apply(tests, components.LoggingMediator, executionLogPath, settings.DebugTests));
                    if (settings.CodeCoverageEnabled)
                    {
                        cleanupCommands.Push(GatherTestCodeCoverageCommand.Apply(test, executionLogPath));
                    }
                    ClearResult(test);
                    // Then re-run the test...
                    ExecutionEventLog.RecordStatus("Not all test variations passed but no crashes recorded, attempting to re-run. (ExecuteTest Variation)");
                    processDuration = processDuration.Add(DriverLauncher.Launch(settings, tests, executionDirectory, components.DebuggingEngine));
                }
                ExecutionEventLog.RecordStatus("Test Execution sequence completed normally.");
            }
            catch (Exception e)
            {
                ExecutionEventLog.RecordException(e);
            }
            finally
            {
                Cleanup(cleanupCommands);
            }
            return processDuration;
        }

        private static void ClearResult(List<TestRecord> tests)
        {
            foreach (TestRecord testRecord in tests)
            {
                ClearResult(testRecord);
            }
        }

        private static void ClearResult(TestRecord test)
        {
            // Clear out existing TestRecord Collection
            foreach (FileInfo infoToDelete in test.LoggedFiles)
            {
                infoToDelete.Delete();
            }

            test.LoggedFiles.Clear();
            // Get rid of the existing log, and any recorded variations...
            test.Log = string.Empty;
            // Same thing with any LoggedFiles for the variation record.
            foreach (VariationRecord variationRecord in test.Variations)
            {
                foreach (FileInfo infoToDelete in variationRecord.LoggedFiles)
                {
                    infoToDelete.Delete();
                }
            }
            test.Variations.Clear();
        }

        private static bool ShouldRerun(List<TestRecord> tests)
        {
            // After the test run completes, check all variations for having set fail, or having successfully logged a .dmp file.
            bool allVariationsPassed = true;
            foreach (TestRecord test in tests)
            {
                foreach (VariationRecord thisRecord in test.Variations)
                {
                    foreach (FileInfo loggedFileInfo in thisRecord.LoggedFiles)
                    {
                       if (Path.GetExtension(loggedFileInfo.FullName).ToLowerInvariant().Equals(".dmp"))
                       {
                           ExecutionEventLog.RecordStatus("*** Saw a dump file as part of this failure. Not attempting to rerun test case");
                           return false;
                       }
                    }
                    if (thisRecord.Result == Result.Fail)
                    {
                        allVariationsPassed = false;
                    }

                }
                // Dump files could be in variations or the test object itself (i.e. if crash occurs outside variations), so check there too.
                for (int index = 0; index < test.LoggedFiles.Count(); index++)
                {
                    if (Path.GetExtension(test.LoggedFiles[index].FullName).ToLowerInvariant().Equals(".dmp"))
                    {
                        ExecutionEventLog.RecordStatus("*** Saw a dump file as part of this failure. Not attempting to rerun test case");
                        return false;
                    }
                }
            }
            return !allVariationsPassed;
        }

        /// <summary>
        /// Run Cleanup on a stack of cleanup command objects.
        ///
        /// Failures in one method don't spill over to the next.
        /// </summary>
        /// <param name="cleanupCommands"></param>
        private static void Cleanup(Stack<ICleanableCommand> cleanupCommands)
        {
            ExecutionEventLog.RecordStatus("Cleanup Execution State");
            while (cleanupCommands.Count != 0)
            {
                ICleanableCommand action = cleanupCommands.Pop();
                try
                {
                    action.Cleanup();
                }
                catch (Exception e)
                {
                    ExecutionEventLog.RecordException(e);
                }
            }
        }

        /// <summary>
        /// Ensure that Log directory is present, for screen captures and Execution backups, as Logging system does not guarantee this.
        /// HACK: This code should go. Far away.
        /// </summary>
        /// <param name="testLogDirectory"></param>
        private static void PrepLogDirectory(DirectoryInfo testLogDirectory)
        {
            if (!testLogDirectory.Exists)
            {
                testLogDirectory.Create();
            }
        }

        #endregion
    }
}
