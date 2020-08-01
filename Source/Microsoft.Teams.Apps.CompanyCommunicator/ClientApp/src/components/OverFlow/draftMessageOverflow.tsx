import React from 'react';
import { Menu } from '@stardust-ui/react';
import { getBaseUrl } from '../../configVariables';
import * as microsoftTeams from "@microsoft/teams-js";
import { connect } from 'react-redux';
import { selectMessage, getMessagesList, getDraftMessagesList } from '../../actions';
import { deleteDraftNotification, duplicateDraftNotification, sendPreview } from '../../apis/messageListApi';

export interface OverflowProps {
    message: any;
    styles?: object;
    title?: string;
    selectMessage?: any;
    getMessagesList?: any;
    getDraftMessagesList?: any;
}

export interface OverflowState {
    teamsTeamId?: string;
    teamsChannelId?: string;
    menuOpen: boolean;
}

export interface ITaskInfo {
    title?: string;
    height?: number;
    width?: number;
    url?: string;
    card?: string;
    fallbackUrl?: string;
    completionBotId?: string;
}

class Overflow extends React.Component<OverflowProps, OverflowState> {
    constructor(props: OverflowProps) {
        super(props);
        this.state = {
            teamsChannelId: '',
            teamsTeamId: '',
            menuOpen: false,
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
    }

    public render(): JSX.Element {
        const items = [
            {
                key: 'more',
                icon: {
                    name: 'more',
                    outline: true,
                },
                menuOpen: this.state.menuOpen,
                active: this.state.menuOpen,
                indicator: false,
                menu: {
                    items: [
                        {
                            key: 'send',
                            content: 'Send',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                let url = getBaseUrl() + "/sendconfirmation/" + this.props.message.id;
                                this.onOpenTaskModule(null, url, "Send confirmation");
                            }
                        },
                        {
                            key: 'preview',
                            content: 'Preview in this channel',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                let payload = {
                                    draftNotificationId: this.props.message.id,
                                    teamsTeamId: this.state.teamsTeamId,
                                    teamsChannelId: this.state.teamsChannelId,
                                }
                                sendPreview(payload).then((response) => {
                                    return response.status;
                                }).catch((error) => {
                                    return error;
                                });
                            }
                        },
                        {
                            key: 'edit',
                            content: 'Edit',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                let url = getBaseUrl() + "/newmessage/" + this.props.message.id;
                                this.onOpenTaskModule(null, url, "Edit message");
                            }
                        },
                        {
                            key: 'duplicate',
                            content: 'Duplicate',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                this.duplicateDraftMessage(this.props.message.id).then(() => {
                                    this.props.getDraftMessagesList();
                                });
                            }
                        },
                        {
                            key: 'divider',
                            kind: 'divider',
                        },
                        {
                            key: 'delete',
                            content: 'Delete',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                this.deleteDraftMessage(this.props.message.id).then(() => {
                                    this.props.getDraftMessagesList();
                                });
                            }
                        },
                    ],
                },
                onMenuOpenChange: (e: any, { menuOpen }: any) => {
                    this.setState({
                        menuOpen: !this.state.menuOpen
                    });
                },
            },
        ];

        return <Menu className="menuContainer" iconOnly items={items} styles={this.props.styles} title={this.props.title} />;
    }

    private onOpenTaskModule = (event: any, url: string, title: string) => {
        let taskInfo: ITaskInfo = {
            url: url,
            title: title,
            height: 530,
            width: 1000,
            fallbackUrl: url,
        };

        let submitHandler = (err: any, result: any) => {
            this.props.getDraftMessagesList().then(() => {
                this.props.getMessagesList();
            });
        };

        microsoftTeams.tasks.startTask(taskInfo, submitHandler);
    }

    private duplicateDraftMessage = async (id: number) => {
        try {
            await duplicateDraftNotification(id);
        } catch (error) {
            return error;
        }
    }

    private deleteDraftMessage = async (id: number) => {
        try {
            await deleteDraftNotification(id);
        } catch (error) {
            return error;
        }
    }
}

const mapStateToProps = (state: any) => {
    return { messages: state.draftMessagesList, selectedMessage: state.selectedMessage };
}

export default connect(mapStateToProps, { selectMessage, getDraftMessagesList, getMessagesList })(Overflow);
