export interface IDeleteMessageRequest {
  rowKeyId?: string;
  selectedDateRange: string;
  deletedBy?: string;
  startDate: string;
  endDate: string;
}

export interface IDeletedMessagesHistory {
  selectedDateRange: string;
  recordsDeleted: number;
  deletedBy?: string;
  status: string;
  startDate: Date;
  endDate: Date;
}
