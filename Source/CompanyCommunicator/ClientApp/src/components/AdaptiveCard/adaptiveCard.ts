// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as AdaptiveCards from 'adaptivecards';
import MarkdownIt from 'markdown-it';

AdaptiveCards.AdaptiveCard.onProcessMarkdown = function (text, result) {
  result.outputHtml = new MarkdownIt().render(text);
  result.didProcess = true;
};

export const getInitAdaptiveCard = (titleText: string) => {
  return {
    type: 'AdaptiveCard',
    body: [
      {
        type: 'TextBlock',
        weight: 'Bolder',
        text: titleText,
        size: 'ExtraLarge',
        wrap: true,
      },
      {
        type: 'Image',
        spacing: 'Default',
        url: '',
        altText: '',
        size: 'Auto',
      },
      {
        type: 'TextBlock',
        text: '',
        wrap: true,
      },
      {
        type: 'TextBlock',
        size: 'Small',
        weight: 'Lighter',
        text: '',
        wrap: true,
      },
    ],
    $schema: 'https://adaptivecards.io/schemas/adaptive-card.json',
    version: '1.0',
  };
};

export const getCardTitle = (card: any) => {
  return card.body[0].text;
};

export const setCardTitle = (card: any, title: string) => {
  card.body[0].text = title;
  card.body[1].altText = `Image for ${title}`;
};

export const getCardImageLink = (card: any) => {
  return card.body[1].url;
};

export const setCardImageLink = (card: any, imageLink?: string) => {
  card.body[1].url = imageLink;
};

export const getCardSummary = (card: any) => {
  return card.body[2].text;
};

export const setCardSummary = (card: any, summary?: string) => {
  card.body[2].text = summary;
};

export const getCardAuthor = (card: any) => {
  return card.body[3].text;
};

export const setCardAuthor = (card: any, author?: string) => {
  card.body[3].text = author;
};

export const getCardBtnTitle = (card: any) => {
  return card.actions[0].title;
};

export const getCardBtnLink = (card: any) => {
  return card.actions[0].url;
};

export const setCardBtn = (card: any, buttonTitle?: string, buttonLink?: string) => {
  if (buttonTitle && buttonLink) {
    card.actions = [
      {
        type: 'Action.OpenUrl',
        title: buttonTitle,
        url: buttonLink,
      },
    ];
  } else {
    delete card.actions;
  }
};
