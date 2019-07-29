import * as React from 'react';
import './newMessage.scss';
import './teamTheme.scss';
import { Input, TextArea, Checkbox } from 'msteams-ui-components-react';
import * as AdaptiveCards from "adaptivecards";
import { Button, Loader } from '@stardust-ui/react';
import * as microsoftTeams from "@microsoft/teams-js";
import { RouteComponentProps } from 'react-router-dom';
import { getDraftNotification, getTeams, createDraftNotification, updateDraftNotification } from '../../apis/messageListApi';
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn
} from '../AdaptiveCard/adaptiveCard';
import { Dropdown } from 'office-ui-fabric-react';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';

export interface IDraftMessage {
    id?: string,
    title: string,
    imageLink?: string,
    summary?: string,
    author: string,
    buttonTitle?: string,
    buttonLink?: string,
    teams: any[],
    rosters: any[],
    allUsers: boolean
}

export interface formState {
    title: string,
    summary?: string,
    btnLink?: string,
    imageLink?: string,
    btnTitle?: string,
    author: string,
    card?: any,
    page: string,
    channelBox: boolean,
    teamBox: boolean,
    allUsersBox: boolean,
    teams?: any[],
    exists?: boolean,
    messageId: string,
    loader: boolean;
}

export interface INewMessageProps extends RouteComponentProps {
    getDraftMessagesList?: any;
}

export default class NewMessage extends React.Component<INewMessageProps, formState> {
    private card: any;
    private selectedTeams: string[] = [];
    private selectedRosters: string[] = [];


    constructor(props: INewMessageProps) {
        super(props);
        initializeIcons();
        this.card = getInitAdaptiveCard();

        this.state = {
            title: "",
            summary: "",
            author: "",
            btnLink: "",
            imageLink: "",
            btnTitle: "",
            card: this.card,
            page: "CardCreation",
            channelBox: false,
            teamBox: false,
            allUsersBox: false,
            messageId: "",
            loader: true
        }
    }

    public async componentDidMount() {
        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);
        let params = this.props.match.params;
        this.getTeamList().then(() => {
            if ('id' in params) {
                let id = params['id'];
                this.getItem(id).then(() => {
                    this.setState({
                        exists: true,
                        messageId: id
                    })
                });
            } else {
                this.setState({
                    exists: false,
                    loader: false
                }, () => {
                    let adaptiveCard = new AdaptiveCards.AdaptiveCard();
                    adaptiveCard.parse(this.state.card);
                    let renderedCard = adaptiveCard.render();
                    document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
                    let link = this.state.btnLink;
                    adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
                })
            }
        });
    }

    private getTeamList = async () => {
        try {
            const response = await getTeams();
            this.setState({
                teams: response.data
            });
        } catch (error) {
            return error;
        }
    }

    private getTeamName = (id: string) => {
        let teamName = "";
        let teams = this.state.teams;
        if (teams !== undefined) {
            for (let i = 0; i < teams.length; i++) {
                if (teams[i].teamId === id) {
                    return teams[i].name;
                }
            }
        }
        return teamName;
    }

    private getItem = async (id: number) => {
        try {
            const response = await getDraftNotification(id);
            let draftMessageDetail = response.data;
            if (draftMessageDetail.teams.length === 0) {
                this.setState({
                    channelBox: false
                });
            } else {
                this.setState({
                    channelBox: true,
                });
                this.selectedTeams = draftMessageDetail.teams;
            }

            if (draftMessageDetail.rosters.length === 0) {
                this.setState({
                    teamBox: false
                });
            } else {
                this.setState({
                    teamBox: true,
                });
                this.selectedRosters = draftMessageDetail.rosters;
            }

            setCardTitle(this.card, draftMessageDetail.title);
            setCardImageLink(this.card, draftMessageDetail.imageLink);
            setCardSummary(this.card, draftMessageDetail.summary);
            setCardAuthor(this.card, draftMessageDetail.author);
            if (draftMessageDetail.buttonTitle !== "" && draftMessageDetail.buttonLink !== "") {
                setCardBtn(this.card, draftMessageDetail.buttonTitle, draftMessageDetail.buttonLink);
            }

            this.setState({
                title: draftMessageDetail.title,
                summary: draftMessageDetail.summary,
                btnLink: draftMessageDetail.buttonLink,
                imageLink: draftMessageDetail.imageLink,
                btnTitle: draftMessageDetail.buttonTitle,
                author: draftMessageDetail.author,
                allUsersBox: draftMessageDetail.allUsers,
                loader: false
            }, () => {
                this.updateCard();
            });
        } catch (error) {
            return error;
        }
    }

    public componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    public render(): JSX.Element {
        if (this.state.loader) {
            return (
                <div className="Loader">
                    <Loader />
                </div>
            );
        } else {
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
                                    onChange={this.onTitleChanged}
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
                                    onChange={this.onAuthorChanged}
                                    status={this.state.author ? "updated" : undefined}
                                    autoComplete="off"
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
                                <Button content="Next" disabled={this.state.title === ""} id="saveBtn" onClick={this.onNext} primary />
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
                                    <Checkbox checked={this.state.teamBox} label="Send to the team members of a Team(s)" value="teams" onChecked={this.onTeam} disabled={this.state.allUsersBox} />
                                </p>

                                <p className="checkboxBtn">
                                    <Checkbox checked={this.state.allUsersBox} label="Send to all users" value="users" onChecked={this.onAlluser} />
                                </p>
                            </div>

                            <div className="boardSelection">

                                <Dropdown
                                    placeholder="Select team(s)"
                                    defaultSelectedKeys={this.selectedTeams}
                                    multiSelect
                                    options={this.getItems()}
                                    onChange={this.onTeamsChange}
                                />

                                <Dropdown
                                    placeholder="Select roster(s)"
                                    defaultSelectedKeys={this.selectedRosters}
                                    multiSelect
                                    options={this.getItems()}
                                    onChange={this.onRostersChange}
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
    }

    private getItems = () => {
        let teams: any[] = [];
        if (this.state.teams) {
            this.state.teams.forEach((element) => {
                teams.push({
                    key: element.teamId,
                    text: element.name
                });
            });
        }
        return teams;
    }

    private onTeamsChange = (event: React.FormEvent<HTMLDivElement>, option?: any, index?: number) => {
        if (option) {
            if (option.selected == true) {
                this.selectedTeams.push(option.key)
            } else {
                let index = this.selectedTeams.indexOf(option.key);
                if (index > -1) {
                    this.selectedTeams.splice(index, 1);
                }
            }
        }
    }

    private onRostersChange = (event: React.FormEvent<HTMLDivElement>, option?: any, index?: number) => {
        if (option) {
            if (option.selected == true) {
                this.selectedRosters.push(option.key)
            } else {
                let index = this.selectedRosters.indexOf(option.key);
                if (index > -1) {
                    this.selectedRosters.splice(index, 1);
                }
            }
        }
    }

    private onSave = () => {
        if (this.state.exists) {
            this.editDraftMessage().then(() => {
                microsoftTeams.tasks.submitTask();
            });
        } else {
            this.postDraftMessage().then(() => {
                microsoftTeams.tasks.submitTask();
            });
        }
    }

    private editDraftMessage = async () => {
        let teams: string[] = [];
        let rosters: string[] = [];

        if (this.state.channelBox) {
            teams = this.selectedTeams;
        }

        if (this.state.teamBox) {
            rosters = this.selectedRosters;
        }

        try {
            let draftMessage: IDraftMessage = {
                id: this.state.messageId,
                title: this.state.title,
                imageLink: this.state.imageLink,
                summary: this.state.summary,
                author: this.state.author,
                buttonTitle: this.state.btnTitle,
                buttonLink: this.state.btnLink,
                teams: teams,
                rosters: rosters,
                allUsers: this.state.allUsersBox
            };

            const response = await updateDraftNotification(draftMessage);
        } catch (error) {
            return error;
        }
    }

    private postDraftMessage = async () => {
        let teams: string[] = [];
        let rosters: string[] = [];

        if (this.state.channelBox) {
            teams = this.selectedTeams;
        }

        if (this.state.teamBox) {
            rosters = this.selectedRosters;
        }

        try {
            let draftMessage: IDraftMessage = {
                title: this.state.title,
                imageLink: this.state.imageLink,
                summary: this.state.summary,
                author: this.state.author,
                buttonTitle: this.state.btnTitle,
                buttonLink: this.state.btnLink,
                teams: teams,
                rosters: rosters,
                allUsers: this.state.allUsersBox
            };

            const response = await createDraftNotification(draftMessage);
        } catch (error) {
            return error;
        }
    }

    public escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            microsoftTeams.tasks.submitTask();
        }
    }

    private onAlluser = () => {
        this.setState({
            teamBox: false,
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

    private onTitleChanged = (event: any) => {
        setCardTitle(this.card, event.target.value);
        this.setState({
            title: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onSummaryChanged = (event: any) => {
        setCardSummary(this.card, event.target.value);
        this.setState({
            summary: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onAuthorChanged = (event: any) => {
        setCardAuthor(this.card, event.target.value);
        this.setState({
            author: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onImageLinkChanged = (event: any) => {
        setCardImageLink(this.card, event.target.value);
        this.setState({
            imageLink: event.target.value,
            card: this.card
        }, () => {
            this.updateCard();
        });
    }

    private onBtnTitleChanged = (event: any) => {
        if (this.state.btnLink !== "" && event.target.value !== "") {
            setCardBtn(this.card, event.target.value, this.state.btnLink);
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
            setCardBtn(this.card, this.state.btnTitle, event.target.value);
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