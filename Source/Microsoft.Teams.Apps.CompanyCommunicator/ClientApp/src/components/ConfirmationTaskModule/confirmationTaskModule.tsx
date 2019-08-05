import * as React from 'react';
import './confirmationTaskModule.scss';
import { getDraftNotification, getConsentSummaries, sendDraftNotification } from '../../apis/messageListApi';
import { RouteComponentProps } from 'react-router-dom';
import * as AdaptiveCards from "adaptivecards";
import { Loader, Button } from '@stardust-ui/react';
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn
} from '../AdaptiveCard/adaptiveCard';
import * as microsoftTeams from "@microsoft/teams-js";

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
    teamNames: string[];
    rosterNames: string[];
    allUsers: boolean;
    messageId: number;
}

class ConfirmationTaskModule extends React.Component<RouteComponentProps, IStatusState> {
    private initMessage = {
        id: "",
        title: ""
    };

    private card: any;

    constructor(props: RouteComponentProps) {
        super(props);

        this.card = getInitAdaptiveCard();

        this.state = {
            message: this.initMessage,
            loader: true,
            teamNames: [],
            rosterNames: [],
            allUsers: false,
            messageId: 0,
        };
    }

    public componentDidMount() {
        microsoftTeams.initialize();

        let params = this.props.match.params;

        if ('id' in params) {
            let id = params['id'];
            this.getItem(id).then(() => {
                getConsentSummaries(id).then((response) => {
                    this.setState({
                        teamNames: response.data.teamNames,
                        rosterNames: response.data.rosterNames,
                        allUsers: response.data.allUsers,
                        messageId: id,
                    }, () => {
                        this.setState({
                            loader: false
                        }, () => {
                            setCardTitle(this.card, this.state.message.title);
                            setCardImageLink(this.card, this.state.message.imageLink);
                            setCardSummary(this.card, this.state.message.summary);
                            setCardAuthor(this.card, this.state.message.author);
                            if (this.state.message.buttonTitle !== "" && this.state.message.buttonLink !== "") {
                                setCardBtn(this.card, this.state.message.buttonTitle, this.state.message.buttonLink);
                            }

                            let adaptiveCard = new AdaptiveCards.AdaptiveCard();
                            adaptiveCard.parse(this.card);
                            let renderedCard = adaptiveCard.render();
                            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
                            let link = this.state.message.buttonLink;
                            adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
                        });
                    });
                });
            });
        }
    }

    private getItem = async (id: number) => {
        try {
            const response = await getDraftNotification(id);
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
                <div className="Loader">
                    <Loader />
                </div>
            );
        } else {
            return (
                <div className="taskModule">
                    <div className="formContainer">
                        <div className="formContentContainer" >
                            <div className="contentField">
                                <h3>Send this message?</h3>
                                <span>Send to the following recipients?</span>
                            </div>

                            <div className="results">
                                {this.displaySelectedTeams()}
                                {this.displayRosterTeams()}
                                {this.displayAllUsers()}
                            </div>
                        </div>
                        <div className="adaptiveCardContainer">
                        </div>
                    </div>

                    <div className="footerContainer">
                        <div className="buttonContainer">
                            <Loader id="sendingLoader" className="hiddenLoader sendingLoader" size="smallest" label="Preparing message" labelPosition="end" />
                            <Button content="Cancel" onClick={this.onCancel} secondary />
                            <Button content="Send" id="saveBtn" onClick={this.onSendMessage} primary />
                        </div>
                    </div>
                </div>
            );
        }
    }

    private onSendMessage = () => {
        let spanner = document.getElementsByClassName("sendingLoader");
        spanner[0].classList.remove("hiddenLoader");
        let id = this.state.messageId;
        sendDraftNotification(this.state.message).then(() => {
            microsoftTeams.tasks.submitTask();
        });
    }

    private onCancel = () => {
        microsoftTeams.tasks.submitTask();
    }

    private displaySelectedTeams = () => {
        let length = this.state.teamNames.length;
        if (length == 0) {
            return (<div />);
        } else {
            return (<div key="teamNames"> <span className="label">Team(s): </span> {this.state.teamNames.map((team, index) => {
                if (length === index + 1) {
                    return (<span key={`teamName${index}`} >{team}</span>);
                } else {
                    return (<span key={`teamName${index}`} >{team}, </span>);
                }
            })}</div>
            );
        }
    }

    private displayRosterTeams = () => {
        let length = this.state.rosterNames.length;
        if (length == 0) {
            return (<div />);
        } else {
            return (<div key="rosterNames"> <span className="label">Team(s) members: </span> {this.state.rosterNames.map((roster, index) => {
                if (length === index + 1) {
                    return (<span key={`rosterName${index}`}>{roster}</span>);
                } else {
                    return (<span key={`rosterName${index}`}>{roster}, </span>);
                }
            })}</div>
            );
        }
    }

    private displayAllUsers = () => {
        if (!this.state.allUsers) {
            return (<div />);
        } else {
            return (<div key="allUsers"> <span className="label">All users</span></div>);
        }
    }
}

export default ConfirmationTaskModule;