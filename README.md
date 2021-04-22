# az-durable-function
This repo contains a concrete example of how to use Azure Durable Functions for building complex workflows

## Scenario
The workflow is pretty simple :
- A project proposal is made by a team manager and then sent to a business manager for approval
- The business manager can either accept or reject the proposal - the decision has to be made within 30 minutes
- In case it's accepted, the team assigned to the project is notified and the project is created in the company's system
- Otherwise, the creator of the proposal is notified
