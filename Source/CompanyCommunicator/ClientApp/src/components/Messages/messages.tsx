import * as React from 'react';
import { DetailsList, Selection, IColumn, CheckboxVisibility } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { Image } from 'office-ui-fabric-react'
import { mergeStyles, unregisterIcons, registerIcons } from 'office-ui-fabric-react/lib/Styling';
import './messages.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getDetailsListHeaderStyle, getDetailsListHeaderColumnStyle } from './messages.style';
import { Icon, Label } from '@stardust-ui/react';
import { string } from 'prop-types';


unregisterIcons([
  'SortDown',
  'SortUp'
]);

registerIcons({
  icons: {
    'SortDown': <Image className="sortIcon" src="https://image.flaticon.com/icons/svg/25/25243.svg" styles={{ image: { height: 8, width: 8 } }} />,
    'SortUp': <Image className="sortIcon" src="https://image.flaticon.com/icons/svg/64/64589.svg" styles={{ image: { height: 8, width: 8 } }} />
  }
});

export interface IMessage {
  title: string;
  date: string;
  recipients: string;
  acknowledgements?: string;
  reactions?: string;
  responses?: string;
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

export default class Messages extends React.Component<{}, IMessageState> {
  private _selection: Selection;
  private _allItems: IMessage[];
  private _columns: IColumn[];

  constructor(props: {}) {
    super(props);
    initializeIcons();

    this._allItems = [];

    this._allItems.push(
      { title: "A Testing Message", date: "12/16/2018", recipients: "30,0,1", acknowledgements: "acknowledgements", reactions: "like 3", responses: "view 3" },
      { title: "Testing", date: "11/16/2019", recipients: "40,6,8", acknowledgements: "acknowledgements", reactions: "like 3", responses: "view 3" },
      { title: "Security Advisory Heightened Security During New Year's Eve Celebrations", date: "12/16/2019", recipients: "90,6,8", acknowledgements: "acknowledgements", reactions: "like 3", responses: "view 3" },
      { title: "Security Advisory Heightened Security During New Year's Eve Celebrations", date: "12/16/2019", recipients: "40,6,8", acknowledgements: "acknowledgements", reactions: "like 3", responses: "view 3" },
      { title: "Upcoming Holiday", date: "12/16/2019", recipients: "14,6,8", acknowledgements: "acknowledgements", reactions: "like 3", responses: "view 3" },
    );

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
              <Icon name="stardust-checkmark" xSpacing="after" />{success}
              <Icon name="stardust-close" xSpacing="both" />{failure}
              <Icon name="exclamation-circle" xSpacing="both" />{throttled}
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
      }
    ];


    this.state = {
      message: this._allItems,
      selectionDetails: "",
      columns: this._columns,
      selectAccount: 0,
      itemsAccount: this._allItems.length,
      width: window.innerWidth,
      height: window.innerHeight,
    };

    this._selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this._getSelectionDetails(this._allItems.length) });
      }
    });

  }


  public render(): JSX.Element {
    const { message } = this.state;
    return (
      <div>
        <Fabric>
          <MarqueeSelection selection={this._selection}>
            <DetailsList
              items={message}
              columns={this.state.columns}
              setKey="set"
              selection={this._selection}
              selectionPreservedOnEmptyClick={true}
              onColumnHeaderClick={this._onColumnClick}
              ariaLabelForSelectionColumn="Toggle selection"
              ariaLabelForSelectAllCheckbox="Toggle selection for all items"
              checkboxVisibility={CheckboxVisibility.hidden}
              styles={getDetailsListHeaderStyle()}
              onRenderCheckbox={this.renderCheckbox}
              onItemInvoked={this._onItemInvoked}
            />
          </MarqueeSelection>
        </Fabric>
      </div>
    );
  }


  private renderCheckbox(e: any): any {
    return (
      <input className="customCheckbox" checked={e.checked} onChange={() => { }} type="checkbox" />
    )
  }

  private _getSelectionDetails(num: number): string {
    let selectionCount = this._selection.getSelectedCount();
    this.setState({
      selectAccount: selectionCount
    });

    switch (selectionCount) {
      case 0:
        return 'No items selected';
      case 1:
        return '1 item selected: ';
      default:
        return `${selectionCount} items selected`;
    }
  }

  _onFilter = (ev: any, text: any) => {
    this.setState({
      message: text ? this._allItems.filter(i => i.title.toLowerCase().indexOf(text) > -1) : this._allItems
    });
  };

  private _onItemInvoked = (item: IMessage): void => {
    alert(`Item invoked: ${item.title}`);
  };

  private _onColumnClick = (event: any, column: any): void => {
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