import * as React from 'react';
import './newMessage.scss';
import './teamTheme.scss';
import { Input, TextArea } from 'msteams-ui-components-react';
import * as AdaptiveCards from "adaptivecards";
import { Dropdown, Checkbox } from 'msteams-ui-components-react';
import { Button } from '@stardust-ui/react';
import * as microsoftTeams from "@microsoft/teams-js";

export interface formState {
    title: string,
    summary?: string,
    btnLink?: string,
    imageLink?: string,
    btnTitle?: string,
    author: string,
    card?: any,
    page: string,
    channel?: string,
    team?: string,
    channelBox: boolean,
    teamBox: boolean,
    allUsersBox: boolean
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
                    "weight": "Bolder",
                    "text": "",
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
                    "text": "Sent by:"
                }
            ],
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.0"
        };

        this.state = {
            title: "",
            summary: "",
            author: "",
            btnLink: "",
            imageLink: "",
            btnTitle: "",
            card: this.card,
            page: "CardCreation",
            channel: "Team",
            team: "Team",
            channelBox: false,
            teamBox: false,
            allUsersBox: false
        }
    }

    public componentDidMount() {
        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);
        let adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        let renderedCard = adaptiveCard.render();
        document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
        let link = this.state.btnLink;
        adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
    }

    public componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    public render(): JSX.Element {
        if (this.state.page === "CardCreation") {
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

                            <Input
                                className="inputField"
                                value={this.state.imageLink}
                                label="Image Link"
                                placeholder="Image link (optional)"
                                onChange={this.onImageLinkChanged}
                                status={this.state.imageLink ? "updated" : undefined}
                                autoComplete="off"
                            />

                            <TextArea
                                className="inputField textArea"
                                autoFocus
                                placeholder="Summary (optional)"
                                label="Summary"
                                value={this.state.summary}
                                onChange={this.onSummaryChanged}
                            />

                            <Input
                                className="inputField"
                                value={this.state.author}
                                label="Author"
                                placeholder="Author"
                                errorLabel={!this.state.author ? "This value is required" : undefined}
                                onChange={this.onAuthorChanged}
                                status={this.state.author ? "updated" : undefined}
                                autoComplete="off"
                                required
                            />

                            <Input
                                className="inputField"
                                value={this.state.btnTitle}
                                label="Button Title"
                                placeholder="Button title"
                                onChange={this.onBtnTitleChanged}
                                status={this.state.btnTitle ? "updated" : undefined}
                                autoComplete="off"
                            />

                            <Input
                                className="inputField"
                                value={this.state.btnLink}
                                label="Button Url"
                                placeholder="Button url"
                                onChange={this.onBtnLinkChanged}
                                status={this.state.btnLink ? "updated" : undefined}
                                autoComplete="off"
                            />
                        </div>
                        <div className="adaptiveCardContainer">
                        </div>
                    </div>

                    <div className="footerContainer">
                        <div className="buttonContainer">
                            <Button content="Next" disabled={this.state.title === "" || this.state.author === ""} id="saveBtn" onClick={this.onNext} primary />
                        </div>
                    </div>
                </div>
            );
        }
        else if (this.state.page === "AudienceSelection") {
            return (
                <div className="taskModule">
                    <div className="formContainer">

                        <h3>Recipient Selection</h3>
                        <h4>Please choose the groups you would like to send your message to.</h4>

                        <div className="checkboxBtns">
                            <p className="checkboxBtn">
                                <Checkbox checked={this.state.channelBox} label="Send to a Team(s)" value="teamtest" onChecked={this.onChannel} />
                            </p>

                            <p className="checkboxBtn">
                                <Checkbox checked={this.state.teamBox} label="Send to the team members of a Team(s)" value="teams" onChecked={this.onTeam} />
                            </p>

                            <p className="checkboxBtn">
                                <Checkbox checked={this.state.allUsersBox} label="Send to all users" value="users" onChecked={this.onAlluser} />
                            </p>
                        </div>

                        <div className="boardSelection">
                            <Dropdown
                                className="dropDown"
                                autoFocus
                                mainButtonText={this.state.channel}
                                style={{ width: '50%' }}
                                items={[
                                    { text: 'Team 1', onClick: () => { this.setState({ channel: "Team 1" }) } },
                                    { text: 'Team 2', onClick: () => { this.setState({ channel: "Team 2" }) } }
                                ]}
                            />

                            <Dropdown
                                className="dropDown"
                                autoFocus
                                mainButtonText={this.state.team}
                                style={{ width: '50%' }}
                                items={[
                                    { text: 'Team 1', onClick: () => { this.setState({ team: "Team 1" }) } },
                                    { text: 'Team 2', onClick: () => { this.setState({ team: "Team 2" }) } }
                                ]}
                            />
                        </div>
                    </div>

                    <div className="footerContainer">
                        <div className="buttonContainer">
                            <Button content="Back" onClick={this.onBack} secondary />
                            <Button content="Save" disabled={!(this.state.channelBox || this.state.teamBox || this.state.allUsersBox)} id="saveBtn" onClick={this.onSave} primary />
                        </div>
                    </div>
                </div>
            );
        } else {
            return (<div>Error</div>);
        }
    }

    private onSave = () => {
        microsoftTeams.tasks.submitTask();
    }

    public escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            microsoftTeams.tasks.submitTask();
        }
    }

    private onAlluser = () => {
        this.setState({
            allUsersBox: !this.state.allUsersBox
        })
    }

    private onTeam = () => {
        this.setState({
            teamBox: !this.state.teamBox
        })
    }

    private onChannel = (checked: boolean, value?: any) => {
        this.setState({
            channelBox: !this.state.channelBox
        })
    }

    private onNext = (event: any) => {
        this.setState({ page: "AudienceSelection" });
    }

    private onBack = (event: any) => {
        this.setState({
            page: "CardCreation"
        }, () => {
            this.updateCard();
        });
    }

    private onValueChanged = (event: any) => {
        this.card.body[0].text = "" + event.target.value;
        this.setState({
            title: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onSummaryChanged = (event: any) => {
        this.card.body[2].text = "" + event.target.value;
        this.setState({
            summary: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onAuthorChanged = (event: any) => {
        this.card.body[3].text = "Sent by : " + event.target.value;
        this.setState({
            author: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onImageLinkChanged = (event: any) => {
        this.card.body[1].url = event.target.value;
        this.setState({
            imageLink: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onBtnTitleChanged = (event: any) => {
        if (this.state.btnLink !== "" && event.target.value !== "") {
            this.card.actions = [
                {
                    "type": "Action.OpenUrl",
                    "title": event.target.value,
                    "url": this.state.btnLink
                }
            ];

            this.setState({
                btnTitle: event.target.value,
                card: this.card
            }, () => {
                this.updateCard();
            });
        } else {
            delete this.card.actions;
            this.setState({
                btnTitle: event.target.value,
            }, () => {
                this.updateCard();
            });
        }
    }

    private onBtnLinkChanged = (event: any) => {

        if (event.target.value !== "" && this.state.btnTitle !== "") {
            this.card.actions = [
                {
                    "type": "Action.OpenUrl",
                    "title": this.state.btnTitle,
                    "url": event.target.value
                }
            ];

            this.setState({
                btnLink: event.target.value,
                card: this.card
            }, () => {
                this.updateCard();
            });
        } else {
            delete this.card.actions;
            this.setState({
                btnLink: event.target.value
            }, () => {
                this.updateCard();
            });
        }

    }

    private updateCard = () => {
        let adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        let renderedCard = adaptiveCard.render();
        let container = document.getElementsByClassName('adaptiveCardContainer')[0].firstChild;
        if (container != null) {
            container.replaceWith(renderedCard);
        } else {
            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
        }
        let link = this.state.btnLink;
        adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
    }
}