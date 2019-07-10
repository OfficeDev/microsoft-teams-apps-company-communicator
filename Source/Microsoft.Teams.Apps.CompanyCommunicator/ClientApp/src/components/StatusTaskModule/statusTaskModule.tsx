import * as React from 'react';
import './statusTaskModule.scss';
import messageList from '../../apis/messageList';
import { RouteComponentProps } from 'react-router-dom';

export interface IMessage {
    id: string;
    title: string;
    date: string;
    recipients: string;
    acknowledgements?: string;
    reactions?: string;
    responses?: string;
}

export interface IStatusState {
    message: IMessage;
}

class StatusTaskModule extends React.Component<RouteComponentProps, IStatusState> {
    private initMessage = {
        id: "",
        title: "",
        date: "",
        recipients: "",
        acknowledgements: "",
        reactions: "",
        responses: "",
    };

    constructor(props: RouteComponentProps) {
        super(props);

        this.state = {
            message: this.initMessage
        };
    }

    public componentDidMount() {
        let params = this.props.match.params;

        if ('id' in params) {
            let id = params['id'];
            this.getItem(id);
        }

    }

    private getItem = async (id: number) => {
        try {
            const response = await messageList.get('/sentnotifications/' + id);
            this.setState({
                message: response.data
            });
        } catch (error) {
            return error;
        }
    }

    public render(): JSX.Element {
        return (
            <div>
                <div>Status Task Module</div>
                <h3>ID: {this.state.message.id}</h3>
                <h3>{this.state.message.title}</h3>
                <h3>{this.state.message.date}</h3>
                <h3>{this.state.message.recipients}</h3>
                <h3>{this.state.message.responses}</h3>
            </div>
        );
    }
}

export default StatusTaskModule;