# FCD3 App Directory example

This example contains a JSON file that can be used as the data source for ComposeUI's FDC3 App Directory implementation.
The App Directory contains the `js-chart` and `js-grid` examples.

To run the example:

1. Run the `serve-chart-and-grid.ps1` script to run the local server for the included web apps.
1. Launch the Shell project using the configuration named `Chart and grid`, or use these command line arguments: 
    ```
    --url http://localhost:4200 --FDC3:AppDirectory:Source $(ComposeUIRepositoryRoot)/examples/fdc3-appdirectory/apps.json
    ```
    (you'll have to manually substitute `$(ComposeUIRepositoryRoot)` with the actual root path of the repo).

The native app in `apps-with-intents.json` is given relative to the module root. Native apps can only be started from under that root, which is the `modules` directory next to the Shell executable by default.
To start the example, set the `COMPOSEUI_MODULE_REPOSITORY_ROOT` environment variable to the root path of the repo, and add `--FDC3:AppDirectory:AllowNativeEnvironmentVariables true` to the command line, because the example app sets an environment variable and the environment variables of an app directory are rejected by default.
