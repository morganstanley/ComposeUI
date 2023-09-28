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
