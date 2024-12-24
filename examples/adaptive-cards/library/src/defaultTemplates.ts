export const defaultCard = (title: string, icon:any) => {
	return {
		"type": "AdaptiveCard",
		"version": "1.6",
		"body": [
			{
				"type": "ColumnSet",
				"columns": [
					{
						"type": "Column",
						"items": [
							{
								"type": "Image",
								"style": "Person",
								"url": `${icon}`,
								"size": "Small"
							}
						],
						"width": "auto"
					},
					{
						"type": "Column",
						"items": [
							{
								"type": "TextBlock",
								"weight": "Bolder",
								"text": `${title}`,
								"wrap": true
							}
						],
						"width": "stretch"
					}
				]
			}
		],
		"actions": [
			{
				"type": "Action.OpenUrl",
				"title": "Click me"      
			}
		]
	}
} 

export const defaultCardTemplate = {
    type: "AdaptiveCard",
    version: "1.6",
    body: [
        {
            "type": "ColumnSet",
            "columns": [
                {
                    "type": "Column",
					"items": [
						{
							"type": "Image",
							"style": "Person",
							"url": "/img/check.png",
							"size": "Small"
						}
					],
					"width": "auto"
                },
                {
					"type": "Column",
					"items": [
						{
							"type": "TextBlock",
							"weight": "Bolder",
							"text": "${title}",
							"wrap": true
						}
					],
					"width": "stretch"
				}
            ]
        },
		{
			type: "AdaptiveHTML",
        	templateString: "<p>I am a code cat </p>"
		}
    ],
    actions: [
        {
            type: "Action.OpenUrl",
            title: "Click me"      
        }
    ]
}