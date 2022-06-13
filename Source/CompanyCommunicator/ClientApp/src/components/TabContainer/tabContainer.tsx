// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as React from 'react';
import { withTranslation, WithTranslation } from "react-i18next";
import Messages from '../Messages/messages';
import DraftMessages from '../DraftMessages/draftMessages';
import ScheduledMessages from '../ScheduledMessages/ScheduledMessages';
import './tabContainer.scss';
import * as microsoftTeams from "@microsoft/teams-js";
import { getBaseUrl } from '../../configVariables';
import { Accordion, Button, Flex, Label } from '@fluentui/react-northstar';
import { getDraftMessagesList, getScheduledMessagesList } from '../../actions';
import { getAppSettings } from "../../apis/messageListApi";
import { connect } from 'react-redux';
import { TFunction } from "i18next";


interface ITaskInfo {
    title?: string;
    height?: number;
    width?: number;
    url?: string;
    card?: string;
    fallbackUrl?: string;
    completionBotId?: string;
}

export interface ITaskInfoProps extends WithTranslation {
    getDraftMessagesList?: any;
    getScheduledMessagesList?: any;
}

export interface ITabContainerState {
    url: string;
    channelId?: string;
    channelName?: string;
    teamName?: string;
    userPrincipalName?: string;
    loading: boolean;
}

class TabContainer extends React.Component<ITaskInfoProps, ITabContainerState> {
    readonly localize: TFunction;
    targetingEnabled: boolean; // property to store value indicating if the targeting mode is enabled or not
    masterAdminUpns: string; // property to store value with the master admins

    constructor(props: ITaskInfoProps) {
        super(props);
        this.localize = this.props.t;
        this.targetingEnabled = false; // by default targeting is disabled
        this.masterAdminUpns = "";
        this.state = {
            loading: true,
            url: getBaseUrl() + "/newmessage?locale={locale}",
            channelId: "",
            channelName: "",
            teamName: "",
            userPrincipalName: ""
        }
        this.escFunction = this.escFunction.bind(this);
    }

    public componentDidMount() {
        const setState = this.setState.bind(this);

        microsoftTeams.initialize();
        //- Handle the Esc key
        document.addEventListener("keydown", this.escFunction, false);

        // get the app settings and based on the targeting configuration and user id 
        // decides if the save is enabled or not
        this.getAppSettings().then(() => {
            setState({ loading: false });
        });

        // get teams context variables and store in the state
        microsoftTeams.getContext(context => {
            setState({
                channelId: context.channelId,
                channelName: context.channelName,
                teamName: context.teamName,
                userPrincipalName: context.userPrincipalName
            });
        });
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
        var isMaster = this.isMasterAdmin(this.masterAdminUpns, this.state.userPrincipalName);
        const panels = [
            {
                title: this.localize('DraftMessagesSectionTitle'),
                content: {
                    key: 'sent',
                    content: (
                        <DraftMessages></DraftMessages>
                    ),
                },
            },
            {
                title: this.localize('ScheduledMessagesSectionTitle'),
                content: {
                    key: 'scheduled',
                    content: (
                        <div className="messages">
                            <ScheduledMessages></ScheduledMessages>
                        </div>
                    ),
                },
            },
            {
                title: this.localize('SentMessagesSectionTitle'),
                content: {
                    key: 'draft',
                    content: (
                        <Messages></Messages>
                    ),
                },
            }
        ]
        return (
            <Flex className="tabContainer" column fill gap="gap.small">
                <Flex className="newPostBtn" hAlign="end" vAlign="end" gap="gap.small">
                    {(this.targetingEnabled) &&
                        <div><Label circular content={this.state.teamName} /> <Label circular content={this.state.channelName} /></div>}
                    <Flex.Item push>
                        <Button content={this.localize("NewMessage")} onClick={this.onNewMessage} primary />
                    </Flex.Item>
                    {((this.targetingEnabled) && (isMaster)) &&
                        <Button content={this.localize("ManageGroups")} onClick={this.ManageGroups} />}
                </Flex>
                <Flex className="messageContainer">
                    <Flex.Item grow={1} >
                        <Accordion defaultActiveIndex={[0, 1, 2]} panels={panels} />
                    </Flex.Item>
                </Flex>
            </Flex>
        );
    }

    public onNewMessage = () => {
        let taskInfo: ITaskInfo = {
            url: this.state.url,
            title: this.localize("NewMessage"),
            height: 530,
            width: 1000,
            fallbackUrl: this.state.url,
        }

        let submitHandler = (err: any, result: any) => {
            this.props.getDraftMessagesList();
            this.props.getScheduledMessagesList();
            
        };

        microsoftTeams.tasks.startTask(taskInfo, submitHandler);
    }

    public ManageGroups = () => {
        var strUrl = getBaseUrl() + "/managegroups?locale={locale}";

        let taskInfo: ITaskInfo = {
            url: strUrl,
            title: this.localize("ManageGroups"),
            height: 530,
            width: 1000,
            fallbackUrl: strUrl,
        }

        microsoftTeams.tasks.startTask(taskInfo);
    }

    // get the app configuration values and set targeting mode from app settings
    private getAppSettings = async () => {
        let response = await getAppSettings();
        if (response.data) {
            this.targetingEnabled = (response.data.targetingEnabled === 'true'); //get the targetingenabled value
            this.masterAdminUpns = response.data.masterAdminUpns; //get the array of master admins
        }
    }

    //returns true if the userUpn is listed on masterAdminUpns
    private isMasterAdmin = (masterAdminUpns: string, userUpn?: string) => {
        var ret = false; // default return value
        var masterAdmins = masterAdminUpns.toLowerCase().split(/;|,/).map(element => element.trim());
        //if we get a userUpn as parameter
        if (userUpn) {
            //gets the index of the user on the master admin array
            if (masterAdmins.indexOf(userUpn.toLowerCase()) >= 0) { ret = true; }
        }

        return ret;
    }
}


const mapStateToProps = (state: any) => {
    return { messages: state.draftMessagesList };
}

const tabContainerWithTranslation = withTranslation()(TabContainer);
export default connect(mapStateToProps, { getDraftMessagesList, getScheduledMessagesList })(tabContainerWithTranslation);