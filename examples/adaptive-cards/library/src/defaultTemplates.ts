import cancel from './img/cancel.png';

export function defaultCard (context:any) {
	const data = { ...context };
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
								"url": `${data.icon}`,
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
								"text": `${data.title}`,
								"wrap": true
							},
							{
								"type": "TextBlock",
								  "text": `${data.message}`,
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
				type: "Action.Submit",
                iconUrl: cancel,
                id: "closeButton",     
			}
		]
	}
} 

export function defaultCardTemplate(context:any) {
	const data = { ...context };

return {
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
							"url": `${data.icon}`,
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
							"text": `${data.title}`,
							"wrap": true
						}
					],
					"width": "stretch"
				}
            ]
        },
		{
			type: "AdaptiveHTML",
        	templateString: `${data.template}`
		}
    ],
    actions: [
        {
            type: "Action.OpenUrl",
            title: "Cancel"      
        },
		{
            type: "Action.OpenUrl",
            title: "Ok"      
        }
    ]
}
}