import * as React from 'react';
import './newMessage.scss';
import './teamTheme.scss';
import { Input, TextArea, Radiobutton, RadiobuttonGroup } from 'msteams-ui-components-react';
import * as AdaptiveCards from "adaptivecards";
import { Button, Loader, Dropdown, Text } from '@stardust-ui/react';
import * as microsoftTeams from "@microsoft/teams-js";
import { RouteComponentProps } from 'react-router-dom';
import { getDraftNotification, getTeams, createDraftNotification, updateDraftNotification } from '../../apis/messageListApi';
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn
} from '../AdaptiveCard/adaptiveCard';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getBaseUrl } from '../../configVariables';

type dropdownItem = {
    header: string,
    team: {
        id: string,
    },
}

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
    selectedTeams: dropdownItem[],
    selectedRosters: dropdownItem[],
    errorImageUrlMessage: string,
    errorButtonUrlMessage: string,
}

export interface INewMessageProps extends RouteComponentProps {
    getDraftMessagesList?: any;
}

export default class NewMessage extends React.Component<INewMessageProps, formState> {
    private card: any;

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
            teamsOptionSelected: true,
            rostersOptionSelected: false,
            allUsersOptionSelected: false,
            messageId: "",
            loader: true,
            selectedTeamsNum: 0,
            selectedRostersNum: 0,
            selectedRadioBtn: "teams",
            selectedTeams: [],
            selectedRosters: [],
            errorImageUrlMessage: "",
            errorButtonUrlMessage: "",
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
                    const selectedTeams = this.makeDropdownItemList(this.state.selectedTeams, this.state.teams);
                    const selectedRosters = this.makeDropdownItemList(this.state.selectedRosters, this.state.teams);
                    this.setState({
                        exists: true,
                        messageId: id,
                        selectedTeams: selectedTeams,
                        selectedRosters: selectedRosters,
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

    private makeDropdownItemList = (items: any[], fromItems: any[] | undefined) => {
        const dropdownItemList: dropdownItem[] = [];
        items.forEach(element =>
            dropdownItemList.push(
                typeof element !== "string" ? element : {
                    header: fromItems!.find(x => x.teamId === element).name,
                    team: {
                        id: element
                    }
                })
        );
        return dropdownItemList;
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

    private getItem = async (id: number) => {
        try {
            const response = await getDraftNotification(id);
            const draftMessageDetail = response.data;
            const selectedRadioButton = draftMessageDetail.rosters.length > 0 ? "rosters" : draftMessageDetail.allUsers ? "allUsers" : "teams";

            this.setState({
                teamsOptionSelected: draftMessageDetail.teams.length > 0,
                selectedTeamsNum: draftMessageDetail.teams.length,
                rostersOptionSelected: draftMessageDetail.rosters.length > 0,
                selectedRostersNum: draftMessageDetail.rosters.length,
                selectedRadioBtn: selectedRadioButton,
                selectedTeams: draftMessageDetail.teams,
                selectedRosters: draftMessageDetail.rosters
            });

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
                                    label="Image URL"
                                    placeholder="Image URL"
                                    onChange={this.onImageLinkChanged}
                                    errorLabel={this.state.errorImageUrlMessage}
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
                                    label="Button title"
                                    placeholder="Button title"
                                    onChange={this.onBtnTitleChanged}
                                    autoComplete="off"
                                />

                                <Input
                                    className="inputField"
                                    value={this.state.btnLink}
                                    label="Button URL"
                                    placeholder="Button URL"
                                    onChange={this.onBtnLinkChanged}
                                    errorLabel={this.state.errorButtonUrlMessage}
                                    autoComplete="off"
                                />
                            </div>
                            <div className="adaptiveCardContainer">
                            </div>
                        </div>

                        <div className="footerContainer">
                            <div className="buttonContainer">
                                <Button content="Next" disabled={this.isNextBtnDisabled()} id="saveBtn" onClick={this.onNext} primary />
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
                                <h3>Choose how you want to send your message</h3>
                                <RadiobuttonGroup
                                    className="radioBtns"
                                    value={this.state.selectedRadioBtn}
                                    onSelected={this.onGroupSelected}
                                >
                                    <Radiobutton name="grouped" value="teams" label="Send to General channel of specific teams" />
                                    <Dropdown
                                        hidden={!this.state.teamsOptionSelected}
                                        placeholder="Select team(s)"
                                        search
                                        multiple
                                        items={this.getItems()}
                                        value={this.state.selectedTeams}
                                        onSelectedChange={this.onTeamsChange}
                                        noResultsMessage="We couldn't find any matches."
                                    />
                                    <Radiobutton name="grouped" value="rosters" label="Send in chat to specific people" />
                                    <Dropdown
                                        hidden={!this.state.rostersOptionSelected}
                                        placeholder="Choose team(s) members"
                                        search
                                        multiple
                                        items={this.getItems()}
                                        value={this.state.selectedRosters}
                                        onSelectedChange={this.onRostersChange}
                                        noResultsMessage="We couldn't find any matches."
                                    />
                                    <Radiobutton name="grouped" value="allUsers" label="Send in chat to everyone" />
                                    <div className={this.state.selectedRadioBtn === "allUsers" ? "" : "hide"}>
                                        <div className="noteText">
                                            <Text error content="Note: This option sends the message to everyone in your org who has access to the app." />
                                        </div>
                                    </div>
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
        this.setState({
            selectedRadioBtn: value,
            teamsOptionSelected: value === 'teams',
            rostersOptionSelected: value === 'rosters',
            allUsersOptionSelected: value === 'allUsers',
            selectedTeams: value === 'teams' ? this.state.selectedTeams : [],
            selectedTeamsNum: value === 'teams' ? this.state.selectedTeamsNum : 0,
            selectedRosters: value === 'rosters' ? this.state.selectedRosters : [],
            selectedRostersNum: value === 'rosters' ? this.state.selectedRostersNum : 0,
        });
    }

    private isSaveBtnDisabled = () => {
        const teamsSelectionIsValid = (this.state.teamsOptionSelected && (this.state.selectedTeamsNum !== 0)) || (!this.state.teamsOptionSelected);
        const rostersSelectionIsValid = (this.state.rostersOptionSelected && (this.state.selectedRostersNum !== 0)) || (!this.state.rostersOptionSelected);
        const nothingSelected = (!this.state.teamsOptionSelected) && (!this.state.rostersOptionSelected) && (!this.state.allUsersOptionSelected);

        return (!teamsSelectionIsValid || !rostersSelectionIsValid || nothingSelected)
    }

    private isNextBtnDisabled = () => {
        const title = this.state.title;
        const btnTitle = this.state.btnTitle;
        const btnLink = this.state.btnLink;
        return !(title && ((btnTitle && btnLink) || (!btnTitle && !btnLink)) && (this.state.errorImageUrlMessage === "") && (this.state.errorButtonUrlMessage === ""));
    }

    private getItems = () => {
        const resultedTeams: dropdownItem[] = [];
        if (this.state.teams) {
            let remainingUserTeams = this.state.teams;
            if (this.state.selectedRadioBtn !== "allUsers") {
                remainingUserTeams = this.state.selectedRadioBtn === "teams" ? this.state.teams.filter(x => this.state.selectedTeams.findIndex(y => y.team.id === x.teamId) < 0) : this.state.teams.filter(x => this.state.selectedRosters.findIndex(y => y.team.id === x.teamId) < 0);
            }
            remainingUserTeams.forEach((element) => {
                resultedTeams.push({
                    header: element.name,
                    team: {
                        id: element.teamId
                    }
                });
            });
        }
        return resultedTeams;
    }

    private onTeamsChange = (event: any, itemsData: any) => {
        this.setState({
            selectedTeams: itemsData.value,
            selectedTeamsNum: itemsData.value.length,
            selectedRosters: [],
            selectedRostersNum: 0
        })
    }

    private onRostersChange = (event: any, itemsData: any) => {
        this.setState({
            selectedRosters: itemsData.value,
            selectedRostersNum: itemsData.value.length,
            selectedTeams: [],
            selectedTeamsNum: 0
        })
    }

    private onSave = () => {
        const selectedTeams: string[] = [];
        const selctedRosters: string[] = [];
        this.state.selectedTeams.forEach(x => selectedTeams.push(x.team.id));
        this.state.selectedRosters.forEach(x => selctedRosters.push(x.team.id));

        const draftMessage: IDraftMessage = {
            id: this.state.messageId,
            title: this.state.title,
            imageLink: this.state.imageLink,
            summary: this.state.summary,
            author: this.state.author,
            buttonTitle: this.state.btnTitle,
            buttonLink: this.state.btnLink,
            teams: selectedTeams,
            rosters: selctedRosters,
            allUsers: this.state.allUsersOptionSelected
        };

        if (this.state.exists) {
            this.editDraftMessage(draftMessage).then(() => {
                microsoftTeams.tasks.submitTask();
            });
        } else {
            this.postDraftMessage(draftMessage).then(() => {
                microsoftTeams.tasks.submitTask();
            });
        }
    }

    private editDraftMessage = async (draftMessage: IDraftMessage) => {
        try {
            await updateDraftNotification(draftMessage);
        } catch (error) {
            return error;
        }
    }

    private postDraftMessage = async (draftMessage: IDraftMessage) => {
        try {
            await createDraftNotification(draftMessage);
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
        let url = event.target.value.toLowerCase();
        if (!((url === "") || (url.startsWith("https://") || (url.startsWith("data:image/png;base64,")) || (url.startsWith("data:image/jpeg;base64,")) || (url.startsWith("data:image/gif;base64,"))))) {
            this.setState({
                errorImageUrlMessage: "URL must start with https://"
            });
        } else {
            this.setState({
                errorImageUrlMessage: ""
            });
        }

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
        const showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !this.state.author && !event.target.value && !this.state.btnLink);
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
        if (!(event.target.value === "" || event.target.value.toLowerCase().startsWith("https://"))) {
            this.setState({
                errorButtonUrlMessage: "URL must start with https://"
            });
        } else {
            this.setState({
                errorButtonUrlMessage: ""
            });
        }

        const showDefaultCard = (!this.state.title && !this.state.imageLink && !this.state.summary && !this.state.author && !this.state.btnTitle && !event.target.value);
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
        const adaptiveCard = new AdaptiveCards.AdaptiveCard();
        adaptiveCard.parse(this.state.card);
        const renderedCard = adaptiveCard.render();
        const container = document.getElementsByClassName('adaptiveCardContainer')[0].firstChild;
        if (container != null) {
            container.replaceWith(renderedCard);
        } else {
            document.getElementsByClassName('adaptiveCardContainer')[0].appendChild(renderedCard);
        }
        const link = this.state.btnLink;
        adaptiveCard.onExecuteAction = function (action) { window.open(link, '_blank'); }
    }
}