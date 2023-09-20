import * as React from 'react';
import { useTranslation } from 'react-i18next';

import { Body1Strong } from '@fluentui/react-components';

export const PreviewMessageConfirmation = () => {
  const { t } = useTranslation();
  return (
    <div className='dialog-padding'>
      <Body1Strong>{t('previewMessageConfirmation') ?? ''}</Body1Strong>
    </div>
  );
};
