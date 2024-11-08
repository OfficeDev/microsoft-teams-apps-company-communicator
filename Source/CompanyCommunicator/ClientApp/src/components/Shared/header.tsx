// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import './main.scss';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { Divider, Link, teamsLightTheme, Theme } from '@fluentui/react-components';
import { PersonFeedback24Regular, QuestionCircle24Regular } from '@fluentui/react-icons';
import mslogo from '../../assets/Images/mslogo.png';

interface IHeaderProps {
  theme: Theme;
}

export const Header = (props: IHeaderProps) => {
  const { t } = useTranslation();
  const customHeaderImagePath = process.env.REACT_APP_HEADERIMAGE;
  const customHeaderText = process.env.REACT_APP_HEADERTEXT ? t(process.env.REACT_APP_HEADERTEXT) : t('CompanyCommunicator');

  return (
    <>
      <div className={props.theme === teamsLightTheme ? 'cc-header-light' : 'cc-header'}>
        <div className='cc-main-left'>
          <img src={customHeaderImagePath ?? mslogo} alt='Microsoft logo' className='cc-logo' title={customHeaderText} />
          <span className='cc-title' title={customHeaderText}>
            {customHeaderText}
          </span>
        </div>
        <div className='cc-main-right'>
          <span className='cc-icon-holder'>
            <Link title={t('Support') ?? ''} className='cc-icon-link' target='_blank' href='https://aka.ms/M365CCIssues'>
              <QuestionCircle24Regular className='cc-icon' />
            </Link>
          </span>
          <span className='cc-icon-holder'>
            <Link title={t('Feedback') ?? ''} className='cc-icon-link' target='_blank' href='https://aka.ms/M365CCFeedback'>
              <PersonFeedback24Regular className='cc-icon' />
            </Link>
          </span>
        </div>
      </div>
      <Divider />
    </>
  );
};
