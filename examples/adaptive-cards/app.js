// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import * as AdaptiveCards from "adaptivecards";
import markdownit from 'markdown-it';

const md = markdownit('commonmark');

AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
    result.outputHtml = md.render(text);
    result.didProcess = true;
}

const notificationTemplate = (type) => {
    return {
        type: "AdaptiveCard",
        body: [
            {
                type: "TextBlock",
                text: `${type} Notification`,
                size: "large",
                weight: "default",
            },
        ],
        actions: [
            {
                type: "Action.Submit",
                iconUrl: "/img/close.png",
                id: "closeButton",
            },
        ],
    };
};

const hostConfig = (bgColor) => {
    return {
        fontFamily: "Segoe UI, Helvetica Neue, sans-serif",
        containerStyles: {
            default: {
                backgroundColor: `${bgColor}`,
            },
        },
    };
};

const renderNotification = (notificationType, bgColor) => {
    let notificationCard = new AdaptiveCards.AdaptiveCard();
    notificationCard.parse(notificationTemplate(notificationType));
    notificationCard.hostConfig = new AdaptiveCards.HostConfig(
        hostConfig(bgColor)
    );
    let result = notificationCard.render(document.body);

    notificationCard.onExecuteAction = function (action) {
        result.remove();
    };

    setTimeout(() => {
        if (result) {
            result.remove();
        }
    }, 7000);
};

// Author a card
let card = {
    type: "AdaptiveCard",
    version: "1.6",
    body: [
        {
            type: "Container",
            style: "default",
            id: "mainContainer",
            items: [
                {
                    type: "TextBlock",
                    text: "Click a button below to send a notification",
                    size: "large",
                },
            ],
        },
    ],
    actions: [
        {
            type: "Action.Submit",
            title: "Info",
            id: "infoButton",
        },
        {
            type: "Action.Submit",
            title: "Error",
            style: "destructive",
            id: "errorButton",
        },
        {
            type: "Action.Submit",
            title: "Success",
            id: "successButton",
        },
    ],
};

// Create an AdaptiveCard instance
let adaptiveCard = new AdaptiveCards.AdaptiveCard();

// Set the adaptive card's event handlers. onExecuteAction is invoked
adaptiveCard.onExecuteAction = function (action) {
    if (action.id === "errorButton") {
        renderNotification("Error", "rgb(237 140 140 / 0.3)");
    } else if (action.id === "successButton") {
        renderNotification("Success", "rgb(201 245 212 / 0.3)");
    } else {
        renderNotification("Info", "rgb(165 176 250 / 0.3)");
    }
};

// Parse the card payload
adaptiveCard.parse(card);

// Render the card to an HTML element:
let renderedCard = adaptiveCard.render(document.body);