// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { TFunction } from "i18next";
import * as AdaptiveCards from "adaptivecards";
import MarkdownIt from "markdown-it";

// Static method to render markdown on the adaptive card
AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
    var md = new MarkdownIt();
    // Teams only supports a subset of markdown as per https://docs.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/cards-format?tabs=adaptive-md%2Cconnector-html#formatting-cards-with-markdown
    md.disable(['image', 'table', 'heading',
        'hr', 'code', 'reference',
        'lheading', 'html_block', 'fence',
        'blockquote', 'strikethrough']);
    // renders the text
    result.outputHtml = md.render(text);
    result.didProcess = true;
}

export const getInitAdaptiveCard = (t: TFunction) => {
    const titleTextAsString = t("TitleText");
    return (
        {
            "type": "AdaptiveCard",
            "body": [
                {
                    "type": "Image",
                    "url": "",
                    "isVisible": false
                },
                {
                    "type": "TextBlock",
                    "text": "",
                    "wrap": true,
                    "isVisible": false
                },
                {
                    "type": "TextBlock",
                    "weight": "Bolder",
                    "text": titleTextAsString,
                    "size": "ExtraLarge",
                    "wrap": true,
                    "separator": true
                },
                {
                    "type": "Image",
                    "spacing": "Default",
                    "url": "",
                    "size": "Stretch",
                    "width": "400px",
                    "altText": ""
                },
                {
                    "type": "TextBlock",
                    "text": "",
                    "wrap": true
                },
                {
                    "type": "TextBlock",
                    "wrap": true,
                    "size": "Small",
                    "weight": "Lighter",
                    "text": ""
                }
            ],
            "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.0"
        }
    );
}

export const getCardTitle = (card: any) => {
    return card.body[2].text;
}

export const setCardTitle = (card: any, title: string) => {
    card.body[2].text = title;
}

export const setCardTarget = (card: any, visibility: boolean) => {
    card.body[0].isVisible = visibility;
    card.body[1].isVisible = visibility;
}

export const setCardTargetTitle = (card: any, title: string) => {
    card.body[1].text = title;
}

export const setCardTargetImage = (card: any, image: string) => {
    card.body[0].url = image;
}

export const getCardImageLink = (card: any) => {
    return card.body[3].url;
}

export const setCardImageLink = (card: any, imageLink?: string) => {
    card.body[3].url = imageLink;
}

export const getCardSummary = (card: any) => {
    return card.body[4].text;
}

export const setCardSummary = (card: any, summary?: string) => {
    card.body[4].text = summary;
}

export const getCardAuthor = (card: any) => {
    return card.body[5].text;
}

export const setCardAuthor = (card: any, author?: string) => {
    card.body[5].text = author;
}

export const getCardBtnTitle = (card: any) => {
    return card.actions[0].title;
}

export const getCardBtnLink = (card: any) => {
    return card.actions[0].url;
}

// set the values collection with buttons to the card actions
export const setCardBtns = (card: any, values: any[]) => {
    if (values !== null) {
            card.actions = values;
    } else {
        delete card.actions;
    }
}

