import * as React from 'react';
import './newMessage.scss';
import { Input, TextArea } from 'msteams-ui-components-react';
import * as AdaptiveCards from "adaptivecards";

export interface formState {
    title: string,
    summary: string,
    articleLink?: string,
    imageLink?: string,
    attachmentLink?: string,
    card?: any
}

export default class NewMessage extends React.Component<{}, formState> {
    private card: any;

    constructor(props: {}) {
        super(props);

        this.card = {
            "type": "AdaptiveCard",
            "body": [
                {
                    "type": "TextBlock",
                    "size": "Large",
                    "weight": "Bolder",
                    "color": "Dark",
                    "text": "Title",
                },
                {
                    "type": "Image",
                    "spacing": "Default",
                    "url": "https://upload.wikimedia.org/wikipedia/commons/thumb/4/49/Seattle_monorail01_2008-02-25.jpg/1024px-Seattle_monorail01_2008-02-25.jpg",
                    "size": "Stretch",
                    "width": "400px",
                    "altText": ""
                },
                {
                    "type": "TextBlock",
                    "spacing": "Large",
                    "separator": true,
                    "height": "stretch",
                    "weight": "Bolder",
                    "text": "Summary",
                    "maxLines": 30,
                    "wrap": true,
                    "width": "400px",
                }
            ],
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.0"
        };

        this.state = {
            title: "",
            summary: "",
            card: {
                "type": "AdaptiveCard",
                "body": [
                    {
                        "type": "TextBlock",
                        "size": "Large",
                        "weight": "Bolder",
                        "color": "Dark",
                        "text": "Title",
                    },
                    {
                        "type": "Image",
                        "spacing": "Default",
                        "url": "https://upload.wikimedia.org/wikipedia/commons/thumb/4/49/Seattle_monorail01_2008-02-25.jpg/1024px-Seattle_monorail01_2008-02-25.jpg",
                        "size": "Stretch",
                        "width": "400px",
                        "altText": ""
                    },
                    {
                        "type": "TextBlock",
                        "spacing": "Large",
                        "separator": true,
                        "height": "stretch",
                        "weight": "Bolder",
                        "text": "Summary",
                        "maxLines": 30,
                        "wrap": true,
                        "width": "400px",
                    }
                ],
                "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                "version": "1.0"
            }
        }
    }

    componentDidMount() {
        let adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        let renderedCard = adaptiveCard.render();
        document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
    }

    render() {
        return (
            <div className="taskModule">

                <div className="formContainer">
                    <div className="formContentContainer" >
                        <Input
                            className="inputField"
                            value={this.state.title}
                            label="Title"
                            placeholder="Title"
                            errorLabel={!this.state.title ? "This value is required" : undefined}
                            onChange={this.onValueChanged}
                            status={this.state.title ? "updated" : undefined}
                            autoComplete="off"
                            required
                        />

                        <TextArea
                            className="inputField textArea"
                            autoFocus
                            placeholder="Summary"
                            label="Summary"
                            errorLabel={!this.state.summary ? "This value is required" : undefined}
                            value={this.state.summary}
                            onChange={this.onSummaryChanged}
                            required />

                        <Input
                            className="inputField"
                            value={this.state.articleLink}
                            label="Article Link"
                            placeholder="Article link (optional)"
                            onChange={this.onArticleLinkChanged}
                            status={this.state.articleLink ? "updated" : undefined}
                            autoComplete="off"
                        />

                        <Input
                            className="inputField"
                            value={this.state.imageLink}
                            label="Image Link"
                            placeholder="Image link (optional)"
                            onChange={this.onImageLinkChanged}
                            status={this.state.imageLink ? "updated" : undefined}
                            autoComplete="off"
                        />

                        <Input
                            className="inputField"
                            value={this.state.attachmentLink}
                            label="Attachment Link"
                            placeholder="Attachment link (optional)"
                            onChange={this.onAttachmentLinkChanged}
                            status={this.state.attachmentLink ? "updated" : undefined}
                            autoComplete="off"
                        />
                    </div>
                    <div className="adaptiveCardContainer">
                    </div>
                </div>


                <div className="footerContainer">
                    <div className="buttonContainer">
                        <button className="secondaryBtn">Back</button>
                        <button className="secondaryBtn">Save as draft</button>
                        <button className="primaryBtn" disabled={this.state.title === "" || this.state.summary === ""}>Schedule</button>
                    </div>
                </div>
            </div>
        );
    }

    onValueChanged = (event: any) => {
        this.card.body[0].text = event.target.value;
        this.setState({
            title: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    onSummaryChanged = (event: any) => {
        this.card.body[2].text = event.target.value;
        this.setState({
            summary: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    onArticleLinkChanged = (event: any) => {
        this.setState({ articleLink: event.target.value });
    }

    onImageLinkChanged = (event: any) => {
        this.setState({ articleLink: event.target.value });
    }

    onAttachmentLinkChanged = (event: any) => {
        this.setState({ articleLink: event.target.value });
    }

    updateCard = () => {
        let adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        let renderedCard = adaptiveCard.render();
        let container = document.getElementsByClassName('adaptiveCardContainer')[0].firstChild;
        if (container != null) {
            container.replaceWith(renderedCard);
        } else {
            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
        }
    }
}