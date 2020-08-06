import * as React from 'react';
import './statusTaskModule.scss';
import { getSentNotification, exportNotification } from '../../apis/messageListApi';
import { RouteComponentProps } from 'react-router-dom';
import * as AdaptiveCards from "adaptivecards";
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { TooltipHost } from 'office-ui-fabric-react';
import { Icon, Loader, List, Image, Button, IconProps } from '@stardust-ui/react';
import * as microsoftTeams from "@microsoft/teams-js";
import {
    getInitAdaptiveCard, setCardTitle, setCardImageLink, setCardSummary,
    setCardAuthor, setCardBtn
} from '../AdaptiveCard/adaptiveCard';
import { ImageUtil } from '../../utility/imageutility';

export interface IListItem {
    header: string,
    media: JSX.Element,
}

export interface IMessage {
    id: string;
    title: string;
    acknowledgements?: string;
    reactions?: string;
    responses?: string;
    succeeded?: string;
    failed?: string;
    unknown?: string;
    sentDate?: string;
    imageLink?: string;
    summary?: string;
    author?: string;
    buttonLink?: string;
    buttonTitle?: string;
    teamNames?: string[];
    rosterNames?: string[];
    groupNames?: string[];
    allUsers?: boolean;
    sendingStartedDate?: string;
    sendingDuration?: string;
    errorMessage?: string;
    warningMessage?: string;
    canDownload?: boolean;
    sendingCompleted?: boolean;
}

export interface IStatusState {
    message: IMessage;
    loader: boolean;
    page: string;
}

class StatusTaskModule extends React.Component<RouteComponentProps, IStatusState> {
    private initMessage = {
        id: "",
        title: ""
    };

    private card: any;

    constructor(props: RouteComponentProps) {
        super(props);
        initializeIcons();
        this.card = getInitAdaptiveCard();

        this.state = {
            message: this.initMessage,
            loader: true,
            page: "ViewStatus",
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
        }
    }

    private getItem = async (id: number) => {
        try {
            const response = await getSentNotification(id);
            response.data.sendingDuration = this.formatNotificationSendingDuration(response.data.sendingStartedDate, response.data.sentDate);
            response.data.sendingStartedDate = this.formatNotificationDate(response.data.sendingStartedDate);
            response.data.sentDate = this.formatNotificationDate(response.data.sentDate);
            this.setState({
                message: response.data
            });
        } catch (error) {
            return error;
        }
    }

    private formatNotificationSendingDuration = (sendingStartedDate: string, sentDate: string) => {
        let sendingDuration = "";
        if (sendingStartedDate && sentDate) {
            let timeDifference = (new Date(sentDate).getTime() - new Date(sendingStartedDate).getTime()) / 1000;
            const hours = Math.floor(timeDifference / 3600);
            timeDifference -= hours * 3600;
            const minutes = Math.floor(timeDifference / 60);
            timeDifference -= minutes * 60;
            const seconds = Math.floor(timeDifference);

            const hoursAsString = ("0" + hours).slice(-2);
            const minutesAsString = ("0" + minutes).slice(-2);
            const secondsAsString = ("0" + seconds).slice(-2);

            sendingDuration = `${hoursAsString}:${minutesAsString}:${secondsAsString}`;
        }
        return sendingDuration;
    }

    private formatNotificationDate = (notificationDate: string) => {
        if (notificationDate) {
            notificationDate = (new Date(notificationDate)).toLocaleString(navigator.language, { year: 'numeric', month: 'numeric', day: 'numeric', hour: 'numeric', minute: 'numeric', hour12: true });
            notificationDate = notificationDate.replace(',', '\xa0\xa0');
        }
        return notificationDate;
    }

    public render(): JSX.Element {
        if (this.state.loader) {
            return (
                <div className="Loader">
                    <Loader />
                </div>
            );
        } else {
            const downloadIcon: IconProps = { name: 'download', size: "medium" };
            if (this.state.page === "ViewStatus") {
                return (
                    <div className="taskModule">
                        <div className="formContainer">
                            <div className="formContentContainer" >
                                <div className="contentField">
                                    <h3>Title</h3>
                                    <span>{this.state.message.title}</span>
                                </div>
                                <div className="contentField">
                                    <h3>Sending started</h3>
                                    <span>{this.state.message.sendingStartedDate}</span>
                                </div>
                                <div className="contentField">
                                    <h3>Completed</h3>
                                    <span>{this.state.message.sentDate}</span>
                                </div>
                                <div className="contentField">
                                    <h3>Duration</h3>
                                    <span>{this.state.message.sendingDuration}</span>
                                </div>
                                <div className="contentField">
                                    <h3>Results</h3>
                                    <label>Success : </label>
                                    <span>{this.state.message.succeeded}</span>
                                    <br />
                                    <label>Failure : </label>
                                    <span>{this.state.message.failed}</span>
                                    <br />
                                    {this.state.message.unknown &&
                                        <>
                                            <label>Unknown : </label>
                                            <span>{this.state.message.unknown}</span>
                                        </>
                                    }
                                </div>
                                <div className="contentField">
                                    {this.renderAudienceSelection()}
                                </div>
                                <div className="contentField">
                                    {this.renderErrorMessage()}
                                </div>
                                <div className="contentField">
                                    {this.renderWarningMessage()}
                                </div>
                            </div>
                            <div className="adaptiveCardContainer">
                            </div>
                        </div>

                        <div className="footerContainer">
                            <div className={this.state.message.canDownload ? "" : "disabled"}>
                                <div className="buttonContainer">
                                    <Loader id="sendingLoader" className="hiddenLoader sendingLoader" size="smallest" label="exporting" labelPosition="end" />
                                    <TooltipHost content={!this.state.message.sendingCompleted ? "" : (this.state.message.canDownload ? "" : "Export in progress")} calloutProps={{ gapSpace: 0 }}>
                                        <Button icon={downloadIcon} disabled={!this.state.message.canDownload || !this.state.message.sendingCompleted} content="Export detailed results" id="exportBtn" onClick={this.onExport} primary />
                                    </TooltipHost>
                                </div>
                            </div>
                        </div>
                    </div>
                );
            }
            else if (this.state.page === "SuccessPage") {
                return (
                    <div className="taskModule">
                        <div className="formContainer">
                            <div className="displayMessageField">
                                <br />
                                <br />
                                <div><span><Icon className="iconStyle" name="stardust-checkmark" xSpacing="before" size="largest" outline /></span>
                                    <h1>Your export is queued</h1></div>
                                <span>You'll be notified in chat when your file is ready to download.</span>
                                <br />
                                <br />
                                <span>Note: You will first get a chat message asking you to give this app permission to upload to your OneDrive.</span>
                                <br />
                                <span>Select "Allow" to proceed.</span>
                            </div>
                        </div>
                        <div className="footerContainer">
                            <div className="buttonContainer">
                                <Button content="Close" id="closeBtn" onClick={this.onClose} primary />
                            </div>
                        </div>
                    </div>

                );
            }
            else {
                return (
                    <div className="taskModule">
                        <div className="formContainer">
                            <div className="displayMessageField">
                                <br />
                                <br />
                                <div><span><Icon className="iconStyle" name="stardust-close" xSpacing="before" size="largest" outline /></span>
                                    <h1 className="light">Something went wrong.</h1></div>
                                <span>Try exporting the results again. If the problem persists, contact your IT admin for help.</span>
                            </div>
                        </div>
                        <div className="footerContainer">
                            <div className="buttonContainer">
                                <Button content="Close" id="closeBtn" onClick={this.onClose} primary />
                            </div>
                        </div>
                    </div>
                );
            }
        }
    }

    private onClose = () => {
        microsoftTeams.tasks.submitTask();
    }

    private onExport = async () => {
        let spanner = document.getElementsByClassName("sendingLoader");
        spanner[0].classList.remove("hiddenLoader");
        await exportNotification(this.state.message.id).then(() => {
            this.setState({ page: "SuccessPage" })
        }).catch(() => {
            this.setState({ page: "ErrorPage" })
        });
    }

    private getItemList = (items: string[]) => {
        let resultedTeams: IListItem[] = [];
        if (items) {
            resultedTeams = items.map((element) => {
                const resultedTeam: IListItem = {
                    header: element,
                    media: <Image src={ImageUtil.makeInitialImage(element)} avatar />
                }
                return resultedTeam;
            });
        }
        return resultedTeams;
    }
    private renderAudienceSelection = () => {
        if (this.state.message.teamNames && this.state.message.teamNames.length > 0) {
            return (
                <div>
                    <h3>Sent to General channel of the following teams</h3>
                    <List items={this.getItemList(this.state.message.teamNames)} />
                </div>);
        } else if (this.state.message.rosterNames && this.state.message.rosterNames.length > 0) {
            return (
                <div>
                    <h3>Sent in chat to people in teams</h3>
                    <List items={this.getItemList(this.state.message.rosterNames)} />
                </div>);
        } else if (this.state.message.groupNames && this.state.message.groupNames.length > 0) {
            return (
                <div>
                    <h3>Sent in chat to everyone in below</h3>
                    <span>M365 groups, Distribution groups or Security Groups</span>
                    <List items={this.getItemList(this.state.message.groupNames)} />
                </div>);
        } else if (this.state.message.allUsers) {
            return (
                <div>
                    <h3>Sent in chat to everyone</h3>
                </div>);
        } else {
            return (<div></div>);
        }
    }
    private renderErrorMessage = () => {
        if (this.state.message.errorMessage) {
            return (
                <div>
                    <h3>Errors</h3>
                    <span>{this.state.message.errorMessage}</span>
                </div>
            );
        } else {
            return (<div></div>);
        }
    }

    private renderWarningMessage = () => {
        if (this.state.message.warningMessage) {
            return (
                <div>
                    <h3>Warnings</h3>
                    <span>{this.state.message.warningMessage}</span>
                </div>
            );
        } else {
            return (<div></div>);
        }
    }
}

export default StatusTaskModule;