import React from 'react';
import { Menu } from '@stardust-ui/react';
import { getBaseUrl } from '../../configVariables';
import * as microsoftTeams from "@microsoft/teams-js";
import { duplicateDraftNotification } from '../../apis/messageListApi';
import { connect } from 'react-redux';
import { selectMessage, getMessagesList, getDraftMessagesList } from '../../actions';

export interface OverflowProps {
    message?: any;
    styles?: object;
    title?: string;
    selectMessage?: any;
    getMessagesList?: any;
    getDraftMessagesList?: any;
}

export interface OverflowState {
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
            menuOpen: false,
        };
    }

    public componentDidMount() {
        microsoftTeams.initialize();
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
                            key: 'status',
                            content: 'View status',
                            onClick: (event: any) => {
                                event.stopPropagation();
                                this.setState({
                                    menuOpen: false,
                                });
                                let url = getBaseUrl() + "/viewstatus/" + this.props.message.id;
                                this.onOpenTaskModule(null, url, "View status");
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
}

const mapStateToProps = (state: any) => {
    return { messagesList: state.messagesList };
}

export default connect(mapStateToProps, { selectMessage, getMessagesList, getDraftMessagesList })(Overflow);
