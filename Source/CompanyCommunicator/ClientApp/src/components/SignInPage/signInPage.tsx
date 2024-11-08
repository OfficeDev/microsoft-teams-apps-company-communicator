// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import './signInPage.scss';
import React from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Text } from '@fluentui/react-components';
import { authentication } from '@microsoft/teams-js';
import i18n from '../../i18n';
import { ROUTE_PARTS } from '../../routes';

const SignInPage = () => {
  const { t } = useTranslation();
  const errorMessage = t('SignInPromptMessage');
  // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
  // @ts-ignore
  const lang: string = i18n.language;

  function onSignIn() {
    authentication
      .authenticate({ url: window.location.origin + '/signin-simple-start', isExternal: true })
      .then(() => {
        console.log('Login succeeded!');
        window.location.href = '/messages';
      })
      .catch((error) => {
        // eslint-disable-next-line @typescript-eslint/restrict-plus-operands
        console.log('Login failed: ' + error);
        window.location.href = `/${ROUTE_PARTS.ERROR_PAGE}?locale=${lang}`;
      });
  }

  return (
    <div className='sign-in-content-container'>
      <Text className='info-text' size={500}>
        {errorMessage}
      </Text>
      <div className='space'></div>
      <Button appearance='primary' className='sign-in-button' onClick={onSignIn}>
        {t('SignIn')}
      </Button>
    </div>
  );
};

export default SignInPage;
