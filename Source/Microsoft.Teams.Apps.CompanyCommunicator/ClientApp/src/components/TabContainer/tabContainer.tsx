import * as React from 'react';
import Messages from '../Messages/messages';
import './tabContainer.scss';
import * as microsoftTeams from "@microsoft/teams-js";
import { getBaseUrl } from '../configVariables';

interface ITaskInfo {
    title?: string;
    height?: number;
    width?: number;
    url?: string;
    card?: string;
    fallbackUrl?: string;
    completionBotId?: string;
}

export interface ITabContainerState {
    url: string;
}

export default class TabContainer extends React.Component<{}, ITabContainerState> {
    constructor(props: {}) {
        super(props);
        this.state = {
            url: getBaseUrl() + "/newmessage"
        }
        this.escFunction = this.escFunction.bind(this);
    }

    public componentDidMount() {
        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);
    }

    public componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    public escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            microsoftTeams.tasks.submitTask();
        }
    }

    public render(): JSX.Element {
        return (
            <div className="tabContainer">
                <div className="newPostBtn">
                    <button className="primaryBtn" onClick={this.onNewPost}>New Post</button>
                </div>
                <div className="messages">
                    <Messages></Messages>
                </div>
            </div>
        );
    }

    public onNewPost = (event: React.MouseEvent<HTMLButtonElement>) => {
        let taskInfo: ITaskInfo = {
            url: this.state.url,
            title: "New Announcement",
            height: 530,
            width: 1000,
            fallbackUrl: this.state.url,
        }

        let submitHandler = (err: any, result: any) => {
        };

        microsoftTeams.tasks.startTask(taskInfo, submitHandler);
    }
}