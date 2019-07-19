import * as React from 'react';
import { DetailsList, Selection, IColumn, CheckboxVisibility } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { mergeStyles } from 'office-ui-fabric-react/lib/Styling';
import './draftMessages.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getDetailsListHeaderStyle, getDetailsListHeaderColumnStyle } from './draftMessages.style';
import { connect } from 'react-redux';
import { selectMessage, getDraftMessagesList, getMessagesList } from '../../actions';
import { getBaseUrl } from '../../configVariables';
import * as microsoftTeams from "@microsoft/teams-js";
import { Loader } from '@stardust-ui/react';
import { IButtonProps, CommandBar, DirectionalHint } from 'office-ui-fabric-react';
import { getSentNotification, deleteDraftNotification, duplicateDraftNotification, sentDraftNotification } from '../../apis/messageListApi';

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
  title: string;
  date: string;
  recipients: string;
  acknowledgements?: string;
  reactions?: string;
  responses?: string;
}

export interface IMessageProps {
  messages: IMessage[];
  selectMessage?: any;
  getDraftMessagesList?: any;
  getMessagesList?: any;
}

export interface IMessageState {
  message: IMessage[];
  selectionDetails: string;
  columns: IColumn[];
  selectAccount: number;
  itemsAccount: number;
  width: number;
  height: number;
  sentMessagePayload?: any;
  loader: boolean;
}

class DraftMessages extends React.Component<IMessageProps, IMessageState> {
  private selection: Selection;
  private columns: IColumn[];

  constructor(props: IMessageProps) {
    super(props);
    initializeIcons();

    /**
     * Build up columns
     * 
     */

    this.columns = [
      {
        key: 'column1',
        name: 'Title',
        fieldName: 'Title',
        minWidth: 130,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        className: 'employee',
        onRender: (item) => {
          const onTitleClicked = (id: string) => {
            let url = getBaseUrl() + "/newmessage/" + id;
            this.onOpenTaskModule(null, url, "New Announcement");
          }
          return (
            <span className="content">
              <button className="contentTitle" onClick={() => onTitleClicked(item.id)}>{item.title}</button>
            </span>
          );
        },
      },
      {
        key: 'column2',
        name: '',
        fieldName: 'More',
        minWidth: 110,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          const customButton = (props: IButtonProps) => {
            return (
              <div></div>
            );
          };

          return (
            <CommandBar
              overflowButtonProps={{
                ariaLabel: 'More commands',
                menuProps: {
                  items: [], // Items must be passed for typesafety, but commandBar will determine items rendered in overflow
                  isBeakVisible: true,
                  beakWidth: 20,
                  gapSpace: 10,
                  directionalHint: DirectionalHint.bottomCenter
                },
                className: 'moreBtn'
              }}
              buttonAs={customButton}
              items={this.getItems()}
              overflowItems={this.getOverflowItems(item)}
            />);
        },
      }
    ];

    this.state = {
      message: props.messages,
      selectionDetails: "",
      columns: this.columns,
      selectAccount: 0,
      itemsAccount: this.props.messages.length,
      width: window.innerWidth,
      height: window.innerHeight,
      loader: true
    };

    this.selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this.getSelectionDetails(this.state.message.length) });
      }
    });
  }

  public componentDidMount() {
    microsoftTeams.initialize();
    this.props.getDraftMessagesList();
  }

  public componentWillReceiveProps(nextProps: any) {
    this.setState({
      message: nextProps.messages,
      loader: false
    })
  }

  public render(): JSX.Element {
    if (this.state.loader) {
      return (
        <Loader />
      );
    } else {
      return (
        <div>
          <Fabric>
            <MarqueeSelection selection={this.selection}>
              <DetailsList
                items={this.state.message}
                columns={this.state.columns}
                setKey="set"
                selection={this.selection}
                selectionPreservedOnEmptyClick={true}
                onColumnHeaderClick={this.onColumnClick}
                ariaLabelForSelectionColumn="Toggle selection"
                ariaLabelForSelectAllCheckbox="Toggle selection for all items"
                checkboxVisibility={CheckboxVisibility.hidden}
                styles={getDetailsListHeaderStyle()}
                onItemInvoked={this.onItemInvoked}
              />
            </MarqueeSelection>
          </Fabric>
        </div>
      );
    }
  }

  private getItems = () => {
    return [];
  };

  private getOverflowItems = (item: any) => {
    let id = item.id;
    return [
      {
        key: 'preview',
        name: 'Preview',
        onClick: () => {

        }
      },
      {
        key: 'edit',
        name: 'Edit',
        onClick: () => {
          let url = getBaseUrl() + "/newmessage/" + id;
          this.onOpenTaskModule(null, url, "New Announcement");
        }
      },
      {
        key: 'delet',
        name: 'Delet',
        onClick: () => {
          this.deleteDraftMessage(id).then(() => {
            this.props.getDraftMessagesList();
          });
        }
      },
      {
        key: 'duplicate',
        name: 'Duplicate',
        onClick: () => {
          this.duplicateDraftMessage(id).then(() => {
            this.props.getDraftMessagesList();
          });
        },
      },
      {
        key: 'sent',
        name: 'Sent',
        onClick: () => {
          this.getDraftMessage(id).then(() => {
            console.log("ddddd payload", this.state.sentMessagePayload);
            this.sentDraftMessage(this.state.sentMessagePayload).then(() => {
              this.props.getDraftMessagesList();
              this.props.getMessagesList();
            });
          });
        },
      }
    ];
  };

  private getDraftMessage = async (id: number) => {
    try {
      const response = await getSentNotification(id);
      this.setState({
        sentMessagePayload: response.data
      });
    } catch (error) {
      return error;
    }
  }

  private sentDraftMessage = async (payload: {}) => {
    try {
      const response = await sentDraftNotification(payload);
    } catch (error) {
      return error;
    }
  }

  private duplicateDraftMessage = async (id: number) => {
    try {
      const response = await duplicateDraftNotification(id);
    } catch (error) {
      return error;
    }
  }

  private deleteDraftMessage = async (id: number) => {
    try {
      const response = await deleteDraftNotification(id);
    } catch (error) {
      return error;
    }
  }

  private onOpenTaskModule = (event: any, url: string, title: string) => {
    let taskInfo: ITaskInfo = {
      url: url,
      title: title,
      height: 530,
      width: 1000,
      fallbackUrl: url
    }

    let submitHandler = (err: any, result: any) => {
      this.props.getDraftMessagesList();
      this.forceUpdate();
    };

    microsoftTeams.tasks.startTask(taskInfo, submitHandler);
  }

  private getSelectionDetails = (num: number): string => {
    let selectionCount = this.selection.getSelectedCount();

    this.setState({
      selectAccount: selectionCount
    });

    let selectedItem = this.selection.getSelection();
    this.props.selectMessage(selectedItem);

    return `${selectionCount} items selected`;
  }

  private onFilter = (ev: any, text: any) => {
    this.setState({
      message: text ? this.state.message.filter(i => i.title.toLowerCase().indexOf(text) > -1) : this.state.message
    });
  };

  private onItemInvoked = (item: IMessage): void => {
    alert(`Item invoked: ${item.title}`);
  };

  private onColumnClick = (event: any, column: any): void => {
    const { columns } = this.state;
    let { message } = this.state;
    let isSortedDescending = column.isSortedDescending;
    // If we've sorted this column, flip it.
    if (column.isSorted) {
      isSortedDescending = !isSortedDescending;
    }
    // Reset the items and columns to match the state.
    this.setState({
      message: _copyAndSort(message, column.fieldName!, isSortedDescending),
      columns: columns.map(col => {
        col.isSorted = col.key === column.key;

        if (col.isSorted) {
          col.isSortedDescending = isSortedDescending;
        }
        return col;
      })
    });
  };
}

function _copyAndSort<T>(items: T[], columnKey: string, isSortedDescending?: boolean): T[] {
  const key = columnKey as keyof T;
  return items.slice(0).sort((a: T, b: T) => ((isSortedDescending ? a[key] < b[key] : a[key] > b[key]) ? 1 : -1));
}

const mapStateToProps = (state: any) => {
  return { messages: state.draftMessagesList };
}

export default connect(mapStateToProps, { selectMessage, getDraftMessagesList, getMessagesList })(DraftMessages);