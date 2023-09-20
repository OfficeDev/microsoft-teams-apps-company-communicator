// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React, { useEffect } from 'react';
import { app } from '@microsoft/teams-js';
import { getAuthenticationConsentMetadata } from '../../apis/messageListApi';

const SignInSimpleStart: React.FunctionComponent = () => {
  useEffect(() => {
    void app.initialize().then(() => {
      void app.getContext().then((context) => {
        const windowLocationOriginDomain = window.location.origin.replace('https://', '');
        const loginHint = context.user?.userPrincipalName ? context.user.userPrincipalName : '';
        void getAuthenticationConsentMetadata(windowLocationOriginDomain, loginHint).then((result) => {
          window.location.assign(result);
        });
      });
    });
  }, []);

  return <></>;
};

export default SignInSimpleStart;
