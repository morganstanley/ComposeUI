Workflows
=========

# Common parameters
Framework versions are set from shared variables across the repository.
Accessing them can be done via the vars object.

| Variable       | Description                                             | Example value |
| -------------- | ------------------------------------------------------- | ------------- |
| DOTNET_VERSION | The version of dotnet to load with actions/setup-dotnet | 6.0.x         |
| NODE_VERSION   | The version of node.js to load with actions/setup-node  | 18.x          |

# Workflows
## continous-integration.yml
Builds all solutions, creates nuget packages and uploads artifacts for further workflows.

## release.yml
Publishes our packages