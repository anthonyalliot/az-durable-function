using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ProposalWorkflowFunctionApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProposalWorkflowFunctionApp
{
    public static class ProjectProposalFunction
    {
        [FunctionName("Run")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("process-proposal-orchestration");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("process-proposal-orchestration")]
        public static async Task<string> ProcessProposalOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext context, 
            ILogger log)
        {
            ProjectProposalModel proposal = await context.CallActivityAsync<ProjectProposalModel>("get-project-proposal", context.InstanceId);

            // Send proposal to the manager
            await context.CallActivityAsync("send-proposal", proposal);

            bool approved;
            try
            {
                // Wait for the manager's approval
                // Set a 30 minutes timeout
                approved = await context.WaitForExternalEvent<bool>("ApprovalEvent", new TimeSpan(0, 30, 0));
                log.LogInformation("Wait for manager's response for project '{proposalId}'", proposal.Id);
            }
            catch (TimeoutException)
            {
                // Timeout occured, it means the manager didn't approve/reject the proposal within the 30 minutes => reject the proposal
                approved = false;
            }

            if (!approved)
            {
                // Deny the proposal
                await context.CallActivityAsync("deny-proposal", proposal);
                return "Proposal rejected";
            }

            // Proposal approved
            // => notify the team + create the project in the company's system
            List<Task> parallelTasks = new List<Task>
            {
                context.CallActivityAsync("notify-team", proposal),
                context.CallSubOrchestratorAsync("process-project-orchestration", proposal)
            };
            await Task.WhenAll(parallelTasks);

            return "Proposal accepted and processed";
        }

        [FunctionName("process-project-orchestration")]
        public static Task ProcessProjectOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            ProjectProposalModel input = context.GetInput<ProjectProposalModel>();
            /*
             * Contain the workflow of the project creation (1 activity per action):
             * Create features + tasks
             * Compute planning
             * Assign tasks
             * Compute charts according to the features/tasks/estimated time...
             */
            return Task.CompletedTask;
        }

        [FunctionName("approve-proposal")]
        public static Task ApproveProposal(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "approve-proposal/{proposalId}")]
            HttpRequestMessage req,
            string proposalId,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            return client.RaiseEventAsync(proposalId, "ApprovalEvent", true);
        }

        [FunctionName("reject-proposal")]
        public static Task RejectProposal(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reject-proposal/{proposalId}")]
            HttpRequestMessage req,
            string proposalId,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            return client.RaiseEventAsync(proposalId, "ApprovalEvent", false);
        }

        [FunctionName("get-project-proposal")]
        public static Task<ProjectProposalModel> GetProjectProposal([ActivityTrigger] string proposalId,
            ILogger log)
        {
            ProjectProposalModel model = new ProjectProposalModel(proposalId)
            {
                Title = "Project title",
                Description = "Project description",
                Features = new List<FeatureModel>
                {
                    new FeatureModel
                    {
                        Name = "Feature 1",
                        Description = "Feature 1 description",
                        Tasks = new List<TaskModel>
                        {
                            new TaskModel
                            {
                                Name = "Task 1",
                                Description = "Task 1 description",
                                EstimatedTime = new TimeSpan(3, 0, 0)
                            },
                            new TaskModel
                            {
                                Name = "Task 2",
                                Description = "Task 2 description",
                                EstimatedTime = new TimeSpan(3, 0, 0)
                            },
                            new TaskModel
                            {
                                Name = "Task 3",
                                Description = "Task 3 description",
                                EstimatedTime = new TimeSpan(3, 0, 0)
                            }
                        }
                    },
                    new FeatureModel
                    {
                        Name = "Feature 2",
                        Description = "Feature 2 description",
                        Tasks = new List<TaskModel>
                        {
                            new TaskModel
                            {
                                Name = "Task 1",
                                Description = "Task 1 description",
                                EstimatedTime = new TimeSpan(3, 0, 0)
                            },
                            new TaskModel
                            {
                                Name = "Task 2",
                                Description = "Task 2 description",
                                EstimatedTime = new TimeSpan(3, 0, 0)
                            }
                        }
                    }
                },
                Team = new TeamModel
                {
                    Name = "Team name",
                    Members = new List<TeamMemberModel>
                    {
                        new TeamMemberModel
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "Jean",
                            LastName = "Dupond"
                        },
                        new TeamMemberModel
                        {
                            Id = Guid.NewGuid(),
                            FirstName = "Pierre",
                            LastName = "Jacques"
                        }
                    }
                }
            };
            return Task.FromResult(model);
        }

        [FunctionName("send-proposal")]
        public static Task SendProposalToManager([ActivityTrigger] ProjectProposalModel proposal,
            ILogger log)
        {
            /*
             * Send proposal to the manager
             */
            return Task.CompletedTask;
        }

        [FunctionName("build-planning-tasks")]
        public static Task BuildPlanningTasks([ActivityTrigger] FeatureModel feature,
            ILogger log)
        { 
            /*
             * Build planning tasks
             */
            return Task.CompletedTask;
        }

        [FunctionName("notify-team")]
        public static Task NotifyTeam([ActivityTrigger] ProjectProposalModel proposal,
            ILogger log)
        {
            /*
             * Notify team's members
             */
            return Task.CompletedTask;
        }

        [FunctionName("deny-proposal")]
        public static Task DenyProposal([ActivityTrigger] ProjectProposalModel proposal,
            ILogger log)
        {
            /*
             * Reject the project proposal
             * => notify the proposal requestor
             */
            return Task.CompletedTask;
        }
    }
}
