import * as React from 'react';
import Messages from '../Messages/messages';
import './tabContainer.scss';
import * as microsoftTeams from "@microsoft/teams-js";

interface ITaskInfo {
    title?: string;
    height?: number;
    width?: number;
    url?: string;
    card?: any;
    fallbackUrl?: any;
    completionBotId?: any;
}

export default class tabContainer extends React.Component {
    constructor(props: {}) {
        super(props);

        this.escFunction = this.escFunction.bind(this);
    }

    componentDidMount() {
        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);

    }

    componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            //Do whatever when esc is pressed
            microsoftTeams.tasks.submitTask(); //- this will return an err object to the completionHandler()
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

    onNewPost = (event: React.MouseEvent<HTMLButtonElement>) => {

        let taskInfo: ITaskInfo = {}
        taskInfo.url = "https://6e444fb7.ngrok.io/newmessage";
        taskInfo.title = "New Announcement";
        taskInfo.height = 530;
        taskInfo.width = 1000;
        taskInfo.fallbackUrl = taskInfo.url;
        taskInfo.completionBotId = null;
        let submitHandler = (err: any, result: any) => {
            console.log(`Submit handler - err: ${err} ${result}`);
        };
        microsoftTeams.tasks.startTask(taskInfo, submitHandler);
    }
}