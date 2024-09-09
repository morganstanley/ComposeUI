try:
    if exists("./screenshots/modules"):
        click(Pattern("./screenshots/datagrid-button.png").similar(0.69))
        chart = wait("./screenshots/open-chart-button.png", 30)
        click(chart)
        if exists("./screenshots/chart-opened.png"): 
            wait(5)
            click(Pattern("./screenshots/goog-symbol.png").exact())
            wait(10)
            exit(0)
        else:
            popup("Chart not available to proceed")
            exit(1)
    else:
        popup("Module not found, can't continue")
        exit(1)
except FindFailed:
    popup("test failed")
    exit(1)
    