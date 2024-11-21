// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";

const notif = (type) => {
    return {
        "type": "AdaptiveCard",
        "body": [
            {
                "type": "Container",
                "items": [
                    {
                        "type": "TextBlock",
                        "text": `${type} Notification`,
                        "size": "large",
                        "weight": "default"

                    }
                ]
            }
        ]
    }
}

const hostConfig = (bgColor) => {
    return { 
        fontFamily: "Segoe UI, Helvetica Neue, sans-serif",
        containerStyles: {
            default: {
                "backgroundColor": `${bgColor}`
            }
        }}
}

const renderNotification = (notificationType, bgColor ) => {
    let notificationCard = new AdaptiveCards.AdaptiveCard();
    notificationCard.parse(notif(notificationType));
    notificationCard.hostConfig = new AdaptiveCards.HostConfig(hostConfig(bgColor));
    let result = notificationCard.render(document.body);

    setTimeout(() => {
        result.remove();
      }, 3000);
}

// Author a card
var card = {
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "Container",
            "style": "default",
            "id": "mainContainer",
            "items": [
                    {
                    "type": "TextBlock",
                    "text": "Click a button below to send a notification",
                    "size": "large"
                }
            ]            
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Info",
            "id": "infoButton",
        },
        {
            "type": "Action.Submit",
            "title": "Error",
            "style": "destructive",
            "id": "errorButton",
        },
        {
            "type": "Action.Submit",
            "title": "Success",
            "id": "successButton",
        }
    ]
};

// Create an AdaptiveCard instance
var adaptiveCard = new AdaptiveCards.AdaptiveCard();

// Set the adaptive card's event handlers. onExecuteAction is invoked
adaptiveCard.onExecuteAction = function(action) { 
    if(action.id === 'errorButton') {
        renderNotif('Error','#ed8c8c');
    }
    else if (action.id === 'successButton') {
        renderNotif('Success','#c9f5d4');
    } else {
        renderNotif('Info','#a5b0fa');
    }
 }

// Parse the card payload
adaptiveCard.parse(card);

// Render the card to an HTML element:
var renderedCard = adaptiveCard.render(document.body);