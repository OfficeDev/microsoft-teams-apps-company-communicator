import * as React from 'react';
import { DetailsList, Selection, IColumn, CheckboxVisibility } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { CommandBar, TooltipHost, TextField } from 'office-ui-fabric-react';
import { mergeStyles } from 'office-ui-fabric-react/lib/Styling';
import { IButtonProps } from 'office-ui-fabric-react/lib/Button';
import { DirectionalHint } from 'office-ui-fabric-react/lib/Callout';
import './messages.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getDetailsListHeaderStyle, getDetailsListHeaderColumnStyle } from './messages.style';
import { Icon } from '@stardust-ui/react';
import { connect } from 'react-redux';
import { selectMessage, getMessagesList } from '../../actions';
import * as microsoftTeams from "@microsoft/teams-js";
import { getBaseUrl } from '../../configVariables';

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
  messagesList: IMessage[];
  selectMessage?: any;
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
}

class Messages extends React.Component<IMessageProps, IMessageState> {
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
            let url = getBaseUrl() + "/viewstatus/" + id;
            this.onOpenTaskModule(null, url, "View Status");
          }
          return (
            <span className="content" >
              <a className="contentTitle" onClick={() => onTitleClicked(item.id)}>{item.title}</a>
            </span>);
        },
      },
      {
        key: 'column2',
        name: 'Recipients',
        fieldName: 'Recipients',
        minWidth: 180,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          let success: string = "0";
          let failure: string = "0";
          let throttled: string = "0";

          if (item != null && item.recipients !== "") {
            let numbers = item.recipients.split(",");
            success = numbers[0];
            failure = numbers[1];
            throttled = numbers[2];
          }

          return (
            <div className="content">
              <TooltipHost content="Success" calloutProps={{ gapSpace: 0 }}>
                <Icon name="stardust-checkmark" xSpacing="after"> </Icon>{success}
              </TooltipHost>

              <TooltipHost content="Failure" calloutProps={{ gapSpace: 0 }}>
                <Icon name="stardust-close" xSpacing="both" />{failure}
              </TooltipHost>

              <TooltipHost content="Throttled" calloutProps={{ gapSpace: 0 }}>
                <Icon name="exclamation-circle" xSpacing="both" />{throttled}
              </TooltipHost>
            </div>
          );
        },
      },
      {
        key: 'column3',
        name: 'Date',
        fieldName: 'Date',
        minWidth: 180,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          return <span className="content">{item.date}</span>;
        },
      },
      {
        key: 'column4',
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
      message: this.props.messagesList,
      selectionDetails: "",
      columns: this.columns,
      selectAccount: 0,
      itemsAccount: this.props.messagesList.length,
      width: window.innerWidth,
      height: window.innerHeight,
    };

    this.selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this.getSelectionDetails(this.state.message.length) });
      }
    });

    this.escFunction = this.escFunction.bind(this);
  }

  public componentDidMount() {
    microsoftTeams.initialize();
    this.props.getMessagesList();
    document.addEventListener("keydown", this.escFunction, false);
  }

  public componentWillUnmount() {
    document.removeEventListener("keydown", this.escFunction, false);
  }

  public componentWillReceiveProps(nextProps: any) {
    if (this.props !== nextProps) {
      this.setState({
        message: nextProps.messagesList
      });
    }
  }

  public render(): JSX.Element {
    return (
      <div>
        <Fabric>
          <TextField
            className="filter"
            label="Filter by title:"
            onChange={this.onFilter}
            styles={{ root: { maxWidth: '300px' } }}
          />
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
              onRenderCheckbox={this.renderCheckbox}
              onItemInvoked={this.onItemInvoked}
            />
          </MarqueeSelection>
        </Fabric>
      </div>
    );
  }

  private escFunction = (event: any) => {
    if (event.keyCode === 27 || (event.key === "Escape")) {
      microsoftTeams.tasks.submitTask();
    }
  }

  private getItems = () => {
    return [];
  };

  private getOverflowItems = (item: any) => {
    let id = item.id;
    return [
      {
        key: 'status',
        name: 'View Status',
        onClick: () => {
          let url = getBaseUrl() + "/viewstatus/" + id;
          this.onOpenTaskModule(null, url, "View Status");
        }
      },
      {
        key: 'content',
        name: 'View Content',
        onClick: () => {
          let url = getBaseUrl() + "/viewcontent/" + id;
          this.onOpenTaskModule(null, url, "View Content");
        }
      },
      {
        key: 'retry',
        name: 'Retry',
        onClick: () => {
          console.log("clicked retry");
        },

      }
    ];
  };

  private onOpenTaskModule = (event: any, url: string, title: string) => {
    let taskInfo: ITaskInfo = {
      url: url,
      title: title,
      height: 530,
      width: 600,
      fallbackUrl: url
    }

    let submitHandler = (err: any, result: any) => {
    };

    microsoftTeams.tasks.startTask(taskInfo, submitHandler);
  }

  private renderCheckbox = (e: any): any => {
    return (
      <input className="customCheckbox" checked={e.checked} onChange={() => { }} type="checkbox" />
    )
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
      message: text ? this.props.messagesList.filter(i => i.title.toLowerCase().indexOf(text) > -1) : this.props.messagesList
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
      message: this.copyAndSort(message, column.fieldName!, isSortedDescending),
      columns: columns.map(col => {
        col.isSorted = col.key === column.key;
        if (col.isSorted) {
          col.isSortedDescending = isSortedDescending;
        }
        return col;
      })
    });
  };

  private copyAndSort<T>(items: T[], columnKey: string, isSortedDescending?: boolean): T[] {
    const key = columnKey as keyof T;
    return items.slice(0).sort((a: T, b: T) => ((isSortedDescending ? a[key] < b[key] : a[key] > b[key]) ? 1 : -1));
  };
}

const mapStateToProps = (state: any) => {
  return { messagesList: state.messagesList };
}

export default connect(mapStateToProps, { selectMessage, getMessagesList })(Messages);