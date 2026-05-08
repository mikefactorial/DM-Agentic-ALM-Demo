using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PlatformPackage.Services
{
    public class WorkflowService
    {
        private readonly IOrganizationService _serviceClient;
        private readonly TraceLogger _traceLogger;

        private const string WorkflowEntityName = "workflow";

        public WorkflowService(IOrganizationService serviceClient, TraceLogger traceLogger)
        {
            _serviceClient = serviceClient;
            _traceLogger = traceLogger;
        }

        public List<KeyValuePair<Guid, bool>> GetWorkflowStatesFromSolutions(string pathToFolderWithSolutions)
        {
            IEnumerable<string> zipFiles = Directory.GetFiles(pathToFolderWithSolutions).Where(x => x.EndsWith(".zip"));
            //Log out each found solution zip
            foreach (var zipFile in zipFiles)
            {
                _traceLogger.Log($"Found solution zip: {zipFile}");
            }
            List<KeyValuePair<Guid, bool>> enabledFlows = new List<KeyValuePair<Guid, bool>>();
            zipFiles.ToList().ForEach(
                x =>
                {
                    using (ZipArchive zipArchive = new ZipArchive(new MemoryStream(File.ReadAllBytes(x))))
                    {
                        ZipArchiveEntry customizationsXml = zipArchive.Entries.FirstOrDefault(
                            y => y.FullName.Equals(Constants.CustomizationsXmlFile, StringComparison.OrdinalIgnoreCase));
                        if (customizationsXml == null || customizationsXml == default(ZipArchiveEntry))
                        {
                            // not a solution
                            return;
                        }

                        _traceLogger.Log($"\n ----- Workflows for solution: {x} ----- \n");
                        // Read the customizations.xml file and look for the workflow id and statecode
                        XDocument customizationsXmlDocument = XDocument.Load(customizationsXml.Open());

                        var workflows = customizationsXmlDocument.Descendants("Workflows").Elements("Workflow"); ;
                        foreach (var workflow in workflows)
                        {
                            // Extract the WorkflowId (removing curly braces if present)
                            string workflowIdStr = workflow.Attribute("WorkflowId")?.Value ?? string.Empty;
                            workflowIdStr = workflowIdStr.Trim('{', '}');
                            string workflowName = workflow.Attribute("Name")?.Value ?? string.Empty;
                            if (Guid.TryParse(workflowIdStr, out Guid workflowId))
                            {
                                // Extract StateCode and convert to bool (1 = active/true, 0 = inactive/false)
                                string stateCodeStr = workflow.Element("StateCode")?.Value ?? "0";
                                bool isActive = stateCodeStr == "1";

                                // Add to list
                                enabledFlows.Add(new KeyValuePair<Guid, bool>(workflowId, isActive));
                                _traceLogger.Log(
                                    $"Name: {workflowName} \n " +
                                    $"WorkflowId: {workflowId} \n " +
                                    $"StateCode: {stateCodeStr} \n ");
                            }
                        }
                    }
                });

            return enabledFlows;
        }


        /// <summary>
        /// Enable or disable Dataverse workflows (flows in solutions).
        /// </summary>
        /// <param name="connectionReferenceName">Connection Reference logical name to retrieve </param>
        /// <param name="workflowId">Provide id of workflow that you want to activate or deactivate</param>
        /// <param name="enabled">Enable workflows => true. Disable workflows => false.</param>
        public void SetWorkflowState(Guid workflowId, bool enabled = true)
        {
            SetStateRequest setStateRequest = new SetStateRequest
            {
                EntityMoniker = new EntityReference(WorkflowEntityName, workflowId),
                State = enabled ? new OptionSetValue(1) : new OptionSetValue(0),
                Status = enabled ? new OptionSetValue(2) : new OptionSetValue(1)
            };

            SetStateResponse setStateResponse = (SetStateResponse)_serviceClient.Execute(setStateRequest);

        }

        /// <summary>
        /// Process workflow state changes with batching and retry logic.
        /// Uses ExecuteMultipleRequest for efficient batch processing.
        /// Continues retrying until there are 0 successful responses or all workflows succeed.
        /// </summary>
        /// <param name="workflowStates">List of workflow IDs and their desired states (true = active, false = inactive)</param>
        public void ProcessWorkflowStates(List<KeyValuePair<Guid, bool>> workflowStates)
        {
            if (workflowStates == null || workflowStates.Count == 0)
            {
                _traceLogger.Log("No workflows to process.");
                return;
            }

            int attemptNumber = 0;
            var pendingWorkflows = new List<KeyValuePair<Guid, bool>>(workflowStates);

            while (pendingWorkflows.Count > 0)
            {
                attemptNumber++;
                _traceLogger.Log($"Processing {pendingWorkflows.Count} workflows - Attempt {attemptNumber}");

                // Create batch request
                var executeMultipleRequest = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = true,
                        ReturnResponses = true,
                    },
                    Requests = new OrganizationRequestCollection(),
                };

                // Add all pending workflow state change requests to the batch
                foreach (var workflowState in pendingWorkflows)
                {
                    var setStateRequest = new SetStateRequest
                    {
                        EntityMoniker = new EntityReference(WorkflowEntityName, workflowState.Key),
                        State = workflowState.Value ? new OptionSetValue(1) : new OptionSetValue(0),
                        Status = workflowState.Value ? new OptionSetValue(2) : new OptionSetValue(1)
                    };
                    executeMultipleRequest.Requests.Add(setStateRequest);
                }

                // Execute the batch request
                ExecuteMultipleResponse executeMultipleResponse = null;
                try
                {
                    executeMultipleResponse = (ExecuteMultipleResponse)_serviceClient.Execute(executeMultipleRequest);
                }
                catch (Exception ex)
                {
                    _traceLogger.Log($"ExecuteMultiple request failed completely: {ex.Message}");
                    // If the entire request fails, we can't continue
                    break;
                }

                // Process results and identify failed workflows
                var failedWorkflows = new List<KeyValuePair<Guid, bool>>();
                int successCount = 0;

                for (int i = 0; i < executeMultipleResponse.Responses.Count; i++)
                {
                    var response = executeMultipleResponse.Responses[i];
                    var workflow = pendingWorkflows[response.RequestIndex];

                    if (response.Fault != null)
                    {
                        _traceLogger.Log($"Failed to change workflow state for: {workflow.Key}. Error: {response.Fault.Message}");
                        failedWorkflows.Add(workflow);
                    }
                    else
                    {
                        _traceLogger.Log($"Successfully changed workflow state for: {workflow.Key} to {workflow.Value}");
                        successCount++;
                    }
                }

                _traceLogger.Log($"Batch completed - Success: {successCount}, Failed: {failedWorkflows.Count}");

                // Check if we should continue retrying
                if (successCount == 0 && failedWorkflows.Count > 0)
                {
                    _traceLogger.Log($"No successful responses in this attempt. Stopping retry logic.");
                    _traceLogger.Log($"Failed to process {failedWorkflows.Count} workflows after {attemptNumber} attempts.");
                    foreach (var workflow in failedWorkflows)
                    {
                        _traceLogger.Log($"Unprocessed workflow: {workflow.Key}, Target state: {workflow.Value}");
                    }
                    break;
                }

                // Update pending workflows for next iteration
                pendingWorkflows = failedWorkflows;

                if (pendingWorkflows.Count > 0)
                {
                    _traceLogger.Log($"Retrying {pendingWorkflows.Count} workflows in the next attempt...");
                }
            }

            if (pendingWorkflows.Count == 0)
            {
                _traceLogger.Log($"All workflows processed successfully after {attemptNumber} attempt(s).");
            }
        }


    }
}
