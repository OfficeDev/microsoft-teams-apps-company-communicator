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
  private selection: Selection;
  private allItems: IMessage[];
  private columns: IColumn[];

  constructor(props: {}) {
    super(props);
    initializeIcons();

    this.allItems = [];

    this.allItems.push(
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
      message: this.allItems,
      selectionDetails: "",
      columns: this.columns,
      selectAccount: 0,
      itemsAccount: this.allItems.length,
      width: window.innerWidth,
      height: window.innerHeight,
    };

    this.selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this.getSelectionDetails(this.allItems.length) });
      }
    });

  }


  public render(): JSX.Element {
    const { message } = this.state;
    return (
      <div>
        <Fabric>
          <MarqueeSelection selection={this.selection}>
            <DetailsList
              items={message}
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


  private renderCheckbox(e: any): any {
    return (
      <input className="customCheckbox" checked={e.checked} onChange={() => { }} type="checkbox" />
    )
  }

  private getSelectionDetails(num: number): string {
    let selectionCount = this.selection.getSelectedCount();
    this.setState({
      selectAccount: selectionCount
    });
    return `${selectionCount} items selected`;
  }

  private onFilter = (ev: any, text: any) => {
    this.setState({
      message: text ? this.allItems.filter(i => i.title.toLowerCase().indexOf(text) > -1) : this.allItems
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

