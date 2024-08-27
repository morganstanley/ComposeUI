if exists("./screenshots/modules"):
    click(Pattern("./screenshots/datagrid-button.png").similar(0.69))
    chart = wait("./screenshots/open-chart-button.png", 20)
    click(chart)
    if exists("./screenshots/chart-opened.png"):
        click(Pattern("./screenshots/goog-symbol.png").exact())
        wait(10)
    else:
        popup("Chart not available to proceed")
        exit()
else:
    popup("Module not found, can't continue")
       
