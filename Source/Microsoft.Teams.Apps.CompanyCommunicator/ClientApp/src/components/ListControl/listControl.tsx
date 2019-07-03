import * as React from 'react';
import { DetailsList, Selection, IColumn, CheckboxVisibility } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';
import { Image } from 'office-ui-fabric-react'
import { mergeStyles, unregisterIcons, registerIcons } from 'office-ui-fabric-react/lib/Styling';
import faker from 'faker';
import Person from './person';
import './listControl.scss';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';
import { getDetailsListHeaderStyle, getDetailsListHeaderColumnStyle } from './List.style';

/**
 * Register Icons
 * 
 */

unregisterIcons([
  'SortDown',
  'SortUp'
]);

registerIcons({
  icons: {
    'SortDown': <Image className="SortIcon" src="https://image.flaticon.com/icons/svg/25/25243.svg" styles={{ image: { height: 8, width: 8 } }} />,
    'SortUp': <Image className="SortIcon" src="https://image.flaticon.com/icons/svg/64/64589.svg" styles={{ image: { height: 8, width: 8 } }} />
  }
});

export interface IDetailsListBasicExampleItem {
  name: string;
  status: string;
  Title: string;
  EmployeeID: string;
  comment: string;
  url: string;
}

export interface IDetailsListBasicExampleState {
  items: IDetailsListBasicExampleItem[];
  selectionDetails: string;
  columns: IColumn[];
  selectAccount: number;
  itemsAccount: number;
  width: number;
  height: number;
}

export default class ListControl extends React.Component<{}, IDetailsListBasicExampleState> {
  private _selection: Selection;
  private _allItems: IDetailsListBasicExampleItem[];
  private _columns: IColumn[];
  private _columnsTemp: IColumn[];

  constructor(props: {}) {
    super(props);
    initializeIcons();

    const items = [
      { name: "Cassanda Dunn", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "testing", url: faker.image.avatar() },
      { name: "Chris Naidoo", status: "Away", Title: "Developer", EmployeeID: "#1234567", comment: "testing2", url: faker.image.avatar() },
      { name: "Erika Fuller", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "testing3", url: faker.image.avatar() },
      { name: "Will Little", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "testing4", url: faker.image.avatar() },
      { name: "Ray Tanaka", status: "Away", Title: "Designer I", EmployeeID: "#1234567", comment: "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.", url: faker.image.avatar() },
    ];

    // Populate with items for demos.
    this._allItems = [];

    this._allItems.push(
      { name: "Cassanda Dunn", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.", url: faker.image.avatar() },
      { name: "Chris Naidoo", status: "Away", Title: "Developer", EmployeeID: "#1234567", comment: "testing2", url: faker.image.avatar() },
      { name: "Erika Fuller", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "testing3", url: faker.image.avatar() },
      { name: "Will Little", status: "online", Title: "Designer II", EmployeeID: "#1234567", comment: "testing4", url: faker.image.avatar() },
      { name: "Ray Tanaka", status: "Away", Title: "Designer I", EmployeeID: "#1234567", comment: "testing5", url: faker.image.avatar() },
    );

    /**
     * Build up columns
     * 
     */

    this._columns = [
      {
        key: 'column1',
        name: 'Employee',
        fieldName: 'name',
        minWidth: 188,
        maxWidth: 188,
        isRowHeader: true,
        isSorted: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        className: 'employee',
        onRender: (item) => {
          return <Person url={item.url} name={item.name}></Person>;
        },
      },
      {
        key: 'column2',
        name: 'Status',
        fieldName: 'status',
        minWidth: 62,
        maxWidth: 62,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),

        onRender: (item) => {
          return <span className={item.status}></span>;
        },
      },
      {
        key: 'column3',
        name: 'Title',
        fieldName: 'Title',
        minWidth: 130,
        maxWidth: 130,
        isRowHeader: true,
        isSortedDescending: false,
        sortAscendingAriaLabel: 'Sorted A to Z',
        sortDescendingAriaLabel: 'Sorted Z to A',
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          return <span className="content">{item.Title}</span>;
        },
      },
      {
        key: 'column4',
        name: 'Employee ID',
        fieldName: 'EmployeeID',
        minWidth: 116,
        maxWidth: 116,
        isResizable: true,
        isCollapsible: true,
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          return <span className="content">{item.EmployeeID}</span>;
        },
      },
      {
        key: 'column5',
        name: 'Comment',
        fieldName: 'comment',
        className: 'commentColumn',
        minWidth: 0,
        isCollapsible: true,
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          return <span className="content">{item.comment}</span>;
        }
      },
      {
        key: 'column6',
        name: '',
        fieldName: '',
        minWidth: 32,
        maxWidth: 32,
        isCollapsible: true,
        data: 'string',
        headerClassName: mergeStyles(getDetailsListHeaderColumnStyle()),
        onRender: (item) => {
          return (
            <i className="fas fa-ellipsis-h" id="More"></i>
          );
        }
      }
    ];

    this._columnsTemp = [...this._columns];

    this.state = {
      items: this._allItems,
      selectionDetails: "",
      columns: this._columns,
      selectAccount: 0,
      itemsAccount: this._allItems.length,
      width: window.innerWidth,
      height: window.innerHeight,
    };

    this._selection = new Selection({
      onSelectionChanged: () => {
        this.setState({ selectionDetails: this._getSelectionDetails(items.length) });
      }
    });

    this.updateWindowDimensions = this.updateWindowDimensions.bind(this);
  }

  componentDidMount() {
    this.updateWindowDimensions();
    window.addEventListener('resize', this.updateWindowDimensions);
  }

  componentWillUnmount() {
    window.removeEventListener('resize', this.updateWindowDimensions);
  }

  updateWindowDimensions() {
    this.setState({ width: window.innerWidth, height: window.innerHeight });
    let employeeHeader = document.getElementById('header1-column1-name');

    if ((window.outerWidth - 16) < 1366) {
      if (employeeHeader) {
        employeeHeader.classList.add("moveEmployeeHeader");
      }
    } else {
      if (employeeHeader) {
        employeeHeader.classList.remove("moveEmployeeHeader");
      }
    }

    if ((window.outerWidth - 16) >= 1366) {
      this.setState({
        columns: this._columns,
      });
    }
    else if ((window.outerWidth - 16) > 1200 && (window.outerWidth - 16) < 1366) {
      this._columnsTemp[0].maxWidth = 188;
      this.setState({
        columns: this._columnsTemp,
      });

      this._columnsTemp = [...this._columns];
    }
    else if ((window.outerWidth - 16) > 1100 && (window.outerWidth - 16) <= 1200) {
      this._columnsTemp.splice(4, 1);
      this._columnsTemp[0].maxWidth = 360;
      this.setState({
        columns: this._columnsTemp,
      });

      this._columnsTemp = [...this._columns];
    }
    else {
      this._columnsTemp.splice(3, 2);
      this._columnsTemp[0].maxWidth = 360;
      this.setState({
        columns: this._columnsTemp,
      });
      this._columnsTemp = [...this._columns];
    }

  }

  public render(): JSX.Element {
    const { items } = this.state;
    return (
      <div>
        <Fabric>
          <MarqueeSelection selection={this._selection}>
            <DetailsList
              items={items}
              columns={this.state.columns}
              setKey="set"
              selection={this._selection}
              selectionPreservedOnEmptyClick={true}
              onColumnHeaderClick={this._onColumnClick}
              ariaLabelForSelectionColumn="Toggle selection"
              ariaLabelForSelectAllCheckbox="Toggle selection for all items"
              checkboxVisibility={CheckboxVisibility.always}
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
      items: text ? this._allItems.filter(i => i.name.toLowerCase().indexOf(text) > -1) : this._allItems
    });
  };

  private _onItemInvoked = (item: IDetailsListBasicExampleItem): void => {
    alert(`Item invoked: ${item.name}`);
  };

  private _onColumnClick = (event: any, column: any): void => {
    const { columns } = this.state;
    let { items } = this.state;
    let isSortedDescending = column.isSortedDescending;
    // If we've sorted this column, flip it.
    if (column.isSorted) {
      isSortedDescending = !isSortedDescending;
    }
    // Reset the items and columns to match the state.
    this.setState({
      items: _copyAndSort(items, column.fieldName!, isSortedDescending),
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