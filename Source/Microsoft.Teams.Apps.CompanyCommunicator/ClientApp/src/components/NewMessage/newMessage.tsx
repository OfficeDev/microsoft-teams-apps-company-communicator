import * as React from 'react';
import './newMessage.scss';
import './teamTheme.scss';
import { Input, TextArea, Checkbox, Radiobutton, RadiobuttonGroup } from 'msteams-ui-components-react';
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
import { getBaseUrl } from '../../configVariables';

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
    teamsOptionSelected: boolean,
    rostersOptionSelected: boolean,
    allUsersOptionSelected: boolean,
    teams?: any[],
    exists?: boolean,
    messageId: string,
    loader: boolean,
    selectedTeamsNum: number,
    selectedRostersNum: number,
    selectedRadioBtn: string,
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
        this.setDefaultCard(this.card);

        this.state = {
            title: "",
            summary: "",
            author: "",
            btnLink: "",
            imageLink: "",
            btnTitle: "",
            card: this.card,
            page: "CardCreation",
            teamsOptionSelected: false,
            rostersOptionSelected: false,
            allUsersOptionSelected: false,
            messageId: "",
            loader: true,
            selectedTeamsNum: 0,
            selectedRostersNum: 0,
            selectedRadioBtn: "",
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
                    if (this.state.btnLink) {
                        let link = this.state.btnLink;
                        adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); };
                    }
                })
            }
        });
    }

    public setDefaultCard = (card: any) => {
        setCardTitle(card, "Title");
        let imgUrl = getBaseUrl() + "/image/imagePlaceholder.png";
        setCardImageLink(card, imgUrl);
        setCardSummary(card, "Summary");
        setCardAuthor(card, "- Author");
        setCardBtn(card, "Button title", "https://adaptivecards.io");
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
                    teamsOptionSelected: false
                });
            } else {
                this.setState({
                    teamsOptionSelected: true,
                    selectedTeamsNum: draftMessageDetail.teams.length,
                    selectedRadioBtn: "teams",
                });
                this.selectedTeams = draftMessageDetail.teams;
            }

            if (draftMessageDetail.rosters.length === 0) {
                this.setState({
                    rostersOptionSelected: false
                });
            } else {
                this.setState({
                    rostersOptionSelected: true,
                    selectedRostersNum: draftMessageDetail.rosters.length,
                    selectedRadioBtn: "rosters",
                });
                this.selectedRosters = draftMessageDetail.rosters;
            }

            if (draftMessageDetail.allUsers) {
                this.setState({
                    selectedRadioBtn: "allUsers",
                })
            }

            setCardTitle(this.card, draftMessageDetail.title);
            setCardImageLink(this.card, draftMessageDetail.imageLink);
            setCardSummary(this.card, draftMessageDetail.summary);
            setCardAuthor(this.card, draftMessageDetail.author);
            setCardBtn(this.card, draftMessageDetail.buttonTitle, draftMessageDetail.buttonLink);

            this.setState({
                title: draftMessageDetail.title,
                summary: draftMessageDetail.summary,
                btnLink: draftMessageDetail.buttonLink,
                imageLink: draftMessageDetail.imageLink,
                btnTitle: draftMessageDetail.buttonTitle,
                author: draftMessageDetail.author,
                allUsersOptionSelected: draftMessageDetail.allUsers,
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
                                    placeholder="Title (required)"
                                    onChange={this.onTitleChanged}
                                    autoComplete="off"
                                    required
                                />

                                <Input
                                    className="inputField"
                                    value={this.state.imageLink}
                                    label="Image Link"
                                    placeholder="Image link"
                                    onChange={this.onImageLinkChanged}
                                    autoComplete="off"
                                />

                                <TextArea
                                    className="inputField textArea"
                                    autoFocus
                                    placeholder="Summary"
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
                                    autoComplete="off"
                                />

                                <Input
                                    className="inputField"
                                    value={this.state.btnTitle}
                                    label="Button Title"
                                    placeholder="Button title"
                                    onChange={this.onBtnTitleChanged}
                                    autoComplete="off"
                                />

                                <Input
                                    className="inputField"
                                    value={this.state.btnLink}
                                    label="Button Url"
                                    placeholder="Button url"
                                    onChange={this.onBtnLinkChanged}
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
                            <div className="formContentContainer" >
                                <h3>Recipient selection</h3>
                                <h4>Please choose the groups you would like to send your message to:</h4>
                                <RadiobuttonGroup
                                    className="radioBtns"
                                    value={this.state.selectedRadioBtn}
                                    onSelected={this.onGroupSelected}
                                >
                                    <Radiobutton name="grouped" value="teams" label="Send to General channel(s)" />
                                    <Dropdown
                                        placeholder="Select team(s)"
                                        defaultSelectedKeys={this.selectedTeams}
                                        multiSelect
                                        options={this.getItems()}
                                        onChange={this.onTeamsChange}
                                        disabled={!this.state.teamsOptionSelected}
                                        className="dropdown"
                                    />
                                    <Radiobutton name="grouped" value="rosters" label="Send in chat" />
                                    <Dropdown
                                        placeholder="Choose team(s) members"
                                        defaultSelectedKeys={this.selectedRosters}
                                        multiSelect
                                        options={this.getItems()}
                                        onChange={this.onRostersChange}
                                        disabled={!this.state.rostersOptionSelected}
                                        className="dropdown"
                                    />
                                    <Radiobutton name="grouped" value="allUsers" label="Send in chat to all users" />
                                </RadiobuttonGroup>
                            </div>
                            <div className="adaptiveCardContainer">
                            </div>
                        </div>

                        <div className="footerContainer">
                            <div className="buttonContainer">
                                <Button content="Back" onClick={this.onBack} secondary />
                                <Button content="Save as draft" disabled={this.isSaveBtnDisabled()} id="saveBtn" onClick={this.onSave} primary />
                            </div>
                        </div>
                    </div>
                );
            } else {
                return (<div>Error</div>);
            }
        }
    }

    private onGroupSelected = (value: any) => {
        if (value === "teams") {
            this.setState({
                teamsOptionSelected: true,
                rostersOptionSelected: false,
                allUsersOptionSelected: false,
            });
        } else if (value === "rosters") {
            this.setState({
                teamsOptionSelected: false,
                rostersOptionSelected: true,
                allUsersOptionSelected: false,
            });
        } else if (value === "allUsers") {
            this.setState({
                teamsOptionSelected: false,
                rostersOptionSelected: false,
                allUsersOptionSelected: true,
            });
        }
        else {
            this.setState({
                teamsOptionSelected: false,
                rostersOptionSelected: false,
                allUsersOptionSelected: false,
            });
        }
        this.setState({
            selectedRadioBtn: value
        });
    }

    private isSaveBtnDisabled = () => {
        let teamsSelectionIsValid = (this.state.teamsOptionSelected && (this.state.selectedTeamsNum !== 0)) || (!this.state.teamsOptionSelected);
        let rostersSelectionIsValid = (this.state.rostersOptionSelected && (this.state.selectedRostersNum !== 0)) || (!this.state.rostersOptionSelected);
        let nothingSelected = (!this.state.teamsOptionSelected) && (!this.state.rostersOptionSelected) && (!this.state.allUsersOptionSelected);

        if (!teamsSelectionIsValid || !rostersSelectionIsValid || nothingSelected) {
            return true;
        } else {
            return false;
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
            if (option.selected === true) {
                this.selectedTeams.push(option.key);
                this.setState({
                    selectedTeamsNum: this.selectedTeams.length
                });
            } else {
                let index = this.selectedTeams.indexOf(option.key);
                if (index > -1) {
                    this.selectedTeams.splice(index, 1);
                    this.setState({
                        selectedTeamsNum: this.selectedTeams.length
                    });
                }
            }
        }
    }

    private onRostersChange = (event: React.FormEvent<HTMLDivElement>, option?: any, index?: number) => {
        if (option) {
            if (option.selected === true) {
                this.selectedRosters.push(option.key);
                this.setState({
                    selectedRostersNum: this.selectedRosters.length
                });
            } else {
                let index = this.selectedRosters.indexOf(option.key);
                if (index > -1) {
                    this.selectedRosters.splice(index, 1);
                    this.setState({
                        selectedRostersNum: this.selectedRosters.length
                    });
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

        if (this.state.teamsOptionSelected) {
            teams = this.selectedTeams;
        }

        if (this.state.rostersOptionSelected) {
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
                allUsers: this.state.allUsersOptionSelected
            };

            const response = await updateDraftNotification(draftMessage);
        } catch (error) {
            return error;
        }
    }

    private postDraftMessage = async () => {
        let teams: string[] = [];
        let rosters: string[] = [];

        if (this.state.teamsOptionSelected) {
            teams = this.selectedTeams;
        }

        if (this.state.rostersOptionSelected) {
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
                allUsers: this.state.allUsersOptionSelected
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

    private onNext = (event: any) => {
        this.setState({
            page: "AudienceSelection"
        }, () => {
            this.updateCard();
        });
    }

    private onBack = (event: any) => {
        this.setState({
            page: "CardCreation"
        }, () => {
            this.updateCard();
        });
    }

    private onTitleChanged = (event: any) => {
        let showDefaultCard = (!event.target.value && !this.state.imageLink && !this.state.summary && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, event.target.value);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        setCardBtn(this.card, this.state.btnTitle, this.state.btnLink);
        this.setState({
            title: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onImageLinkChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !event.target.value && !this.state.summary && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, event.target.value);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        setCardBtn(this.card, this.state.btnTitle, this.state.btnLink);
        this.setState({
            imageLink: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onSummaryChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !event.target.value && !this.state.author && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, event.target.value);
        setCardAuthor(this.card, this.state.author);
        setCardBtn(this.card, this.state.btnTitle, this.state.btnLink);
        this.setState({
            summary: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onAuthorChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !event.target.value && !this.state.btnTitle && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, event.target.value);
        setCardBtn(this.card, this.state.btnTitle, this.state.btnLink);
        this.setState({
            author: event.target.value,
            card: this.card
        }, () => {
            if (showDefaultCard) {
                this.setDefaultCard(this.card);
            }
            this.updateCard();
        });
    }

    private onBtnTitleChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !this.state.author && !event.target.value && !this.state.btnLink);
        setCardTitle(this.card, this.state.title);
        setCardImageLink(this.card, this.state.imageLink);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        if (event.target.value && this.state.btnLink) {
            setCardBtn(this.card, event.target.value, this.state.btnLink);
            this.setState({
                btnTitle: event.target.value,
                card: this.card
            }, () => {
                if (showDefaultCard) {
                    this.setDefaultCard(this.card);
                }
                this.updateCard();
            });
        } else {
            delete this.card.actions;
            this.setState({
                btnTitle: event.target.value,
            }, () => {
                if (showDefaultCard) {
                    this.setDefaultCard(this.card);
                }
                this.updateCard();
            });
        }
    }

    private onBtnLinkChanged = (event: any) => {
        let showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !this.state.author && !this.state.btnTitle && !event.target.value);
        setCardTitle(this.card, this.state.title);
        setCardSummary(this.card, this.state.summary);
        setCardAuthor(this.card, this.state.author);
        setCardImageLink(this.card, this.state.imageLink);
        if (this.state.btnTitle && event.target.value) {
            setCardBtn(this.card, this.state.btnTitle, event.target.value);
            this.setState({
                btnLink: event.target.value,
                card: this.card
            }, () => {
                if (showDefaultCard) {
                    this.setDefaultCard(this.card);
                }
                this.updateCard();
            });
        } else {
            delete this.card.actions;
            this.setState({
                btnLink: event.target.value
            }, () => {
                if (showDefaultCard) {
                    this.setDefaultCard(this.card);
                }
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