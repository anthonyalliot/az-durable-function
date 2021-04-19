using System;
using System.Collections.Generic;

namespace ProposalWorkflowFunctionApp.Models
{
    public class ProjectProposalModel
    {
        public string Id { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<FeatureModel> Features { get; set; }
        public TeamModel Team { get; set; }

        public ProjectProposalModel(string id)
        {
            Id = id;
        }
    }

    public class FeatureModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TaskModel> Tasks { get; set; } 
    }

    public class TaskModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TimeSpan EstimatedTime { get; set; }
    }

    public class TeamModel
    {
        public string Name { get; set; }
        public List<TeamMemberModel> Members { get; set; }
    }

    public class TeamMemberModel
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
