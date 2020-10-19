import * as React from 'react';
import './draftMessages.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { connect } from 'react-redux';
import { selectMessage, getDraftMessagesList, getMessagesList } from '../../actions';
import { getBaseUrl } from '../../configVariables';
import * as microsoftTeams from "@microsoft/teams-js";
import { Loader, List, Flex, Text } from '@stardust-ui/react';
import Overflow from '../OverFlow/draftMessageOverflow';

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
  date: string;
  recipients: string;
  acknowledgements?: string;
  reactions?: string;
  responses?: string;
}

export interface IMessageProps {
  messages: IMessage[];
  selectedMessage: any;
  selectMessage?: any;
  getDraftMessagesList?: any;
  getMessagesList?: any;
}

export interface IMessageState {
  message: IMessage[];
  itemsAccount: number;
  loader: boolean;
  teamsTeamId?: string;
  teamsChannelId?: string;
}

class DraftMessages extends React.Component<IMessageProps, IMessageState> {
  private interval: any;
  private timeout: any;
  private isOpenTaskModuleAllowed: boolean;

  constructor(props: IMessageProps) {
    super(props);
    initializeIcons();
    this.isOpenTaskModuleAllowed = true;
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

    this.props.getDraftMessagesList();
    this.interval = setInterval(() => {
      this.props.getDraftMessagesList();
    }, 60000);
  }

  public componentWillReceiveProps(nextProps: any) {
    this.setState({
      message: nextProps.messages,
      loader: false
    })
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
            <Flex.Item shrink={0} grow={1}>
              <Text>{message.title}</Text>
            </Flex.Item>
            <Flex.Item shrink={0} hAlign="end" vAlign="center">
              <Overflow message={message} send={this.updateInterval} title="" />
            </Flex.Item>
          </Flex>
        ),
        styles: { margin: '0.2rem 0.2rem 0 0' },
        onClick: (): void => {
          let url = getBaseUrl() + "/newmessage/" + message.id;
          this.onOpenTaskModule(null, url, "Edit message");
        },
      };
      return out;
    };

    const label = this.processLabels();
    const outList = this.state.message.map(processItem);
    const allDraftMessages = [...label, ...outList];

    if (this.state.loader) {
      return (
        <Loader />
      );
    } else if (this.state.message.length === 0) {
      return (<div className="results">You have no draft messages.</div>);
    }
    else {
      return (
        <List selectable items={allDraftMessages} className="list" />
      );
    }
  }

  private processLabels = () => {
    const out = [{
      key: "labels",
      content: (
        <Flex vAlign="center" fill gap="gap.small">
          <Flex.Item>
            <Text
              truncated
              weight="bold"
              content="Title"
            >
            </Text>
          </Flex.Item>
        </Flex>
      ),
      styles: { margin: '0.2rem 0.2rem 0 0' },
    }];
    return out;
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
        this.props.getDraftMessagesList().then(() => {
          this.props.getMessagesList();
          this.isOpenTaskModuleAllowed = true;
        });
      };

      microsoftTeams.tasks.startTask(taskInfo, submitHandler);
    }
  }

  private updateInterval = () => {
    if (this.interval) {
      clearInterval(this.interval);
    }

    if (this.timeout) {
      clearTimeout(this.timeout);
    }

    this.interval = setInterval(() => {
      this.props.getDraftMessagesList().then(() => {
        this.props.getMessagesList();
      });
    }, 10000)

    this.timeout = setTimeout(() => {
      clearInterval(this.interval);
      this.interval = setInterval(() => {
        this.props.getDraftMessagesList();
      }, 60000);
    }, 60000);
  }
}

const mapStateToProps = (state: any) => {
  return { messages: state.draftMessagesList, selectedMessage: state.selectedMessage };
}

export default connect(mapStateToProps, { selectMessage, getDraftMessagesList, getMessagesList })(DraftMessages);