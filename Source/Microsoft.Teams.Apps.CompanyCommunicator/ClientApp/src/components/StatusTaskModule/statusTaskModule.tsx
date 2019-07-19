import * as React from 'react';
import './statusTaskModule.scss';
import { getSentNotification } from '../../apis/messageListApi';
import { RouteComponentProps } from 'react-router-dom';
import * as AdaptiveCards from "adaptivecards";
import { Loader } from '@stardust-ui/react';

export interface IMessage {
    id: string;
    title: string;
    acknowledgements?: string;
    reactions?: string;
    responses?: string;
    succeeded?: string;
    failed?: string;
    throttled?: string;
    sentDate?: string;
    imageLink?: string;
    summary?: string;
    author?: string;
    buttonLink?: string;
    buttonTitle?: string;
}

export interface IStatusState {
    message: IMessage;
    loader: boolean;
}

class StatusTaskModule extends React.Component<RouteComponentProps, IStatusState> {
    private initMessage = {
        id: "",
        title: ""
    };

    private card: any;

    constructor(props: RouteComponentProps) {
        super(props);

        this.card = {
            "type": "AdaptiveCard",
            "body": [
                {
                    "type": "TextBlock",
                    "weight": "Bolder",
                    "text": "Title",
                    "size": "ExtraLarge",
                    "wrap": true
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
                    "text": "Sent by: Anonymous"
                }
            ],
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.0"
        };

        this.state = {
            message: this.initMessage,
            loader: true
        };
    }

    public componentDidMount() {
        let params = this.props.match.params;

        if ('id' in params) {
            let id = params['id'];
            this.getItem(id).then(() => {
                this.setState({
                    loader: false
                }, () => {
                    this.card.body[0].text = this.state.message.title;
                    this.card.body[1].url = this.state.message.imageLink;
                    this.card.body[2].text = this.state.message.summary;
                    this.card.body[3].text = "Sent by : " + this.state.message.author;
                    if (this.state.message.buttonTitle !== "" && this.state.message.buttonLink !== "") {
                        this.card.actions = [
                            {
                                "type": "Action.OpenUrl",
                                "title": this.state.message.buttonTitle,
                                "url": this.state.message.buttonLink
                            }
                        ];
                    }

                    let adaptiveCard = new AdaptiveCards.AdaptiveCard();
                    adaptiveCard.parse(this.card);
                    let renderedCard = adaptiveCard.render();
                    document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
                    let link = this.state.message.buttonLink;
                    adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
                });
            });
        }
    }

    private getItem = async (id: number) => {
        try {
            const response = await getSentNotification(id);
            this.setState({
                message: response.data
            });
        } catch (error) {
            return error;
        }
    }

    public render(): JSX.Element {
        if (this.state.loader) {
            return (
                <Loader />
            );
        } else {
            return (
                <div className="taskModule">
                    <div className="formContainer">
                        <div className="formContentContainer" >
                            <div className="contentField">
                                <h3>Title</h3>
                                <span>{this.state.message.title}</span>
                            </div>

                            <div className="contentField">
                                <h3>Create by</h3>
                                <span>Anonymous</span>
                            </div>

                            <div className="contentField">
                                <h3>Date Sent</h3>
                                <span>{this.state.message.sentDate}</span>
                            </div>

                            <div className="contentField">
                                <h3>Results</h3>
                                <label>Success : </label>
                                <span>{this.state.message.succeeded}</span>
                                <br />
                                <label>Failure : </label>
                                <span>{this.state.message.failed}</span>
                                <br />
                                <label>Throttled : </label>
                                <span>{this.state.message.throttled}</span>
                            </div>
                        </div>
                        <div className="adaptiveCardContainer">
                        </div>
                    </div>

                    <div className="footerContainer">
                        <div className="buttonContainer">
                        </div>
                    </div>
                </div>
            );
        }
    }
}

export default StatusTaskModule;