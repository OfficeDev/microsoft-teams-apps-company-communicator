// <copyright file="draftMessages.tsx" company="Microsoft Corporation">
// Copyright (c) Microsoft.
// Licensed under the MIT License.
// </copyright>

import * as React from 'react';
import { connect } from 'react-redux';
import { withTranslation, WithTranslation } from "react-i18next";
import { initializeIcons } from 'office-ui-fabric-react';
import { Loader, List, Flex, Text } from '@fluentui/react-northstar';
import * as microsoftTeams from "@microsoft/teams-js";
import { getAppSettings } from "../../apis/messageListApi";
import { selectMessage, getScheduledMessagesList, getDraftMessagesList, getMessagesList } from '../../actions';
import { getBaseUrl } from '../../configVariables';
import Overflow from '../OverFlow/scheduledMessageOverflow';
import { TFunction } from "i18next";

export interface ITaskInfo {
    title?: string;
    height?: number;
    width?: number;
    url?: string;
    card?: string;
    fallbackUrl?: string;
    completionBotId?: string;
}

export interface IMessage {
    id: string;
    title: string;
    scheduledDate: string;
    recipients: string;
    acknowledgements?: string;
    reactions?: string;
    responses?: string;
}

export interface IMessageProps extends WithTranslation {
    messages: IMessage[];
    selectedMessage: any;
    selectMessage?: any;
    getDraftMessagesList?: any;
    getScheduledMessagesList?: any;
    getMessagesList?: any;
}

export interface IMessageState {
    message: IMessage[];
    itemsAccount: number;
    loader: boolean;
    teamsTeamId?: string;
    teamsChannelId?: string;
}

class ScheduledMessages extends React.Component<IMessageProps, IMessageState> {
    readonly localize: TFunction;
    private interval: any;
    private isOpenTaskModuleAllowed: boolean;
    targetingEnabled: boolean; // property to store value indicating if the targeting mode is enabled or not
    masterAdminUpns: string; // property to store value with the master admins

    constructor(props: IMessageProps) {
        super(props);
        initializeIcons();
        this.localize = this.props.t;
        this.isOpenTaskModuleAllowed = true;
        this.targetingEnabled = false; // by default targeting is disabled
        this.masterAdminUpns = "";
        this.state = {
            message: props.messages,
            itemsAccount: this.props.messages.length,
            loader: true,
            teamsTeamId: "",
            teamsChannelId: "",
        };
    }

    public componentDidMount() {
        microsoftTeams.initialize();
        microsoftTeams.getContext((context) => {
            this.setState({
                teamsTeamId: context.teamId,
                teamsChannelId: context.channelId,
            });
        });
        
        this.props.getScheduledMessagesList();
        this.interval = setInterval(() => {
            this.props.getScheduledMessagesList();
        }, 60000);
    }

    public componentWillReceiveProps(nextProps: any) {
        if (this.props !== nextProps) {
            this.setState({
                message: nextProps.messages,
                loader: false
            });
        }
    }

    public componentWillUnmount() {
        clearInterval(this.interval);
    }

    public render(): JSX.Element {
        let keyCount = 0;
        const processItem = (message: any) => {
            keyCount++;
            const out = {
                key: keyCount,
                content: (
                    <Flex vAlign="center" fill gap="gap.small">
                        <Flex.Item grow={1} >
                             <Text>{message.title}</Text>
                        </Flex.Item>
                        <Flex.Item push size="24%" shrink={0}>
                            <Text
                            truncated
                            className="semiBold"
                            content={message.scheduledDate} />
                        </Flex.Item>
                        <Flex.Item shrink={0} align="end">
                            <Overflow message={message} title="" />
                        </Flex.Item>
                    </Flex>
                ),
                styles: { margin: '0.2rem 0.2rem 0 0' },
                onClick: (): void => {
                    let url = getBaseUrl() + "/newmessage/" + message.id + "?locale={locale}";
                    this.onOpenTaskModule(null, url, this.localize("EditMessage"));
                },
            };
            return out;
        };

        const label = this.processLabels();
        const outList = this.state.message.map(processItem);
        const allScheduledMessages = [...label, ...outList];

        if (this.state.loader) {
            return (
                <Loader />
            );
        } else if (this.state.message.length === 0) {
            return (<div className="results">{this.localize("EmptyScheduledMessages")}</div>);
        }
        else {
            return (
                <List selectable items={allScheduledMessages} className="list" />
            );
        }
    }

    private processLabels = () => {
        const out = [{
            key: "labels",
            content: (
                <Flex vAlign="center" fill gap="gap.small">
                    <Flex.Item grow={1} >
                        <Text
                            truncated
                            weight="bold"
                            content={this.localize("TitleText")}
                        >
                        </Text>
                    </Flex.Item>
                    <Flex.Item push size="24%" shrink={0}>
                        <Text
                            truncated
                            content={this.localize("ScheduledDate")}
                            weight="bold"
                        >
                        </Text>
                    </Flex.Item>
                    <Flex.Item shrink={0} align="end">
                        <Overflow message="" />
                    </Flex.Item>
                </Flex>
            ),
            styles: { margin: '0.2rem 0.2rem 0 0' },
        }];
        return out;
    }

    // get the app configuration values and set targeting mode from app settings
    private getAppSettings = async () => {
        let response = await getAppSettings();
        if (response.data) {
            this.targetingEnabled = (response.data.targetingEnabled === 'true'); //get the targetingenabled value
            this.masterAdminUpns = response.data.masterAdminUpns; //get the array of master admins
        }
    }

    private onOpenTaskModule = (event: any, url: string, title: string) => {
        if (this.isOpenTaskModuleAllowed) {
            this.isOpenTaskModuleAllowed = false;
            let taskInfo: ITaskInfo = {
                url: url,
                title: title,
                height: 530,
                width: 1000,
                fallbackUrl: url,
            }

            let submitHandler = (err: any, result: any) => {
                this.props.getScheduledMessagesList().then(() => {
                        this.props.getDraftMessagesList();
                        this.props.getMessagesList();
                        this.isOpenTaskModuleAllowed = true;
                });
            };

            microsoftTeams.tasks.startTask(taskInfo, submitHandler);
        }
    }
}

const mapStateToProps = (state: any) => {
    return { messages: state.scheduledMessagesList, selectedMessage: state.selectedMessage };
}

const ScheduledMessagesWithTranslation = withTranslation()(ScheduledMessages);
export default connect(mapStateToProps, { selectMessage, getScheduledMessagesList, getDraftMessagesList, getMessagesList })(ScheduledMessagesWithTranslation);