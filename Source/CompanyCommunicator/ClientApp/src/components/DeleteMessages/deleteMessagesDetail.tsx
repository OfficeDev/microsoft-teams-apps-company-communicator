// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Persona, Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow, useArrowNavigationGroup } from '@fluentui/react-components';
import { Clock20Regular } from '@fluentui/react-icons';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import moment from 'moment';

export const DeleteMessageDetail = (deletedMessages: any) => {
  const { t } = useTranslation();
  const keyboardNavAttr = useArrowNavigationGroup({ axis: 'grid' });

  return (
        <Table {...keyboardNavAttr} role='grid' aria-label={t('deletedMessagesGridNavigation') ?? ''}>
            <TableHeader>
                <TableRow>
                    <TableHeaderCell key='selectedDateRange' style={{ width: '30%' }}>
                        <b>{t('selectedDateRange')}</b>
                    </TableHeaderCell>
                    <TableHeaderCell key='status' style={{ width: '20%' }}>
                        <b>{t('status')}</b>
                    </TableHeaderCell>
                    <TableHeaderCell key='recordsDeleted' style={{ width: '20%' }}>
                        <b>{t('recordsDeleted')}</b>
                    </TableHeaderCell>
                    <TableHeaderCell key='deletedBy' style={{ width: '30%' }}>
                        <b>{t('deletedBy')}</b>
                    </TableHeaderCell>
                </TableRow>
            </TableHeader>
            <TableBody>
                {deletedMessages.deletedMessages?.map((item: any, index: number) => (
                    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
                    <TableRow key={`${index}_key`}>
                        <TableCell tabIndex={0} role='gridcell'>
                            <TableCellLayout truncate media={<Clock20Regular />}>
                                <span>{t(item.selectedDateRange)}</span>
                                <span> (<b>{item.startDate ?? moment(item.startDate).format('MM/DD/YYYY')}</b> {t('to')} <b>{item.endDate ?? moment(item.endDate).format('MM/DD/YYYY')}</b>)</span>
                            </TableCellLayout>
                        </TableCell>
                        <TableCell tabIndex={0} role='gridcell'>
                            <TableCellLayout>
                                {item.status}
                            </TableCellLayout>
                        </TableCell>
                        <TableCell tabIndex={0} role='gridcell'>
                            <TableCellLayout truncate>
                                {item.recordsDeleted.toString()}
                            </TableCellLayout>
                        </TableCell>
                        <TableCell tabIndex={0} role='gridcell'>
                            <TableCellLayout title={item.deletedBy} truncate>
                                <Persona size='extra-small' textAlignment='center' name={item.deletedBy} secondaryText={'Member'} avatar={{ color: 'colorful' }} />
                            </TableCellLayout>
                        </TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>
  );
};
