import * as React from 'react';
import { DetailsList, Selection, IColumn, CheckboxVisibility } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { mergeStyles } from 'office-ui-fabric-react/lib/Styling';
import './draftMessages.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getDetailsListHeaderStyle, getDetailsListHeaderColumnStyle } from './draftMessages.style';
import { connect } from 'react-redux';
import { selectMessage, getDraftMessagesList } from '../../actions';

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

class DraftMessages extends React.Component<IMessageProps, IMessageState> {
  private _selection: Selection;
  private _columns: IColumn[];

  constructor(props: IMessageProps) {
    super(props);
    initializeIcons();

    /**
     * Build up columns
     * 
     */

    this._columns = [
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
          return <span className="content">{item.title}</span>;
        },
      },
    ];

    this.state = {
      message: props.messages,
      selectionDetails: "",
      columns: this._columns,
      selectAccount: 0,
      itemsAccount: this.props.messages.length,
      width: window.innerWidth,
      height: window.innerHeight,
    };

    this._selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this.getSelectionDetails(this.state.message.length) });
      }
    });
  }

  public componentDidMount() {
    this.props.getDraftMessagesList();
  }

  public componentWillReceiveProps(nextProps: any) {
    this.setState({
      message: nextProps.messages,
    })
  }

  public render(): JSX.Element {
    return (
      <div>
        <Fabric>
          <MarqueeSelection selection={this._selection}>
            <DetailsList
              items={this.state.message}
              columns={this.state.columns}
              setKey="set"
              selection={this._selection}
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

  private getSelectionDetails = (num: number): string => {
    let selectionCount = this._selection.getSelectedCount();

    this.setState({
      selectAccount: selectionCount
    });

    let selectedItem = this._selection.getSelection();
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

export default connect(mapStateToProps, { selectMessage, getDraftMessagesList })(DraftMessages);