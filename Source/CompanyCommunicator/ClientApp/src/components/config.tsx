// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';
import { app, pages } from '@microsoft/teams-js';
import { getBaseUrl } from '../configVariables';

export interface IConfigState {
  url: string;
}

class Configuration extends React.Component<any, IConfigState> {
  constructor(props: any) {
    super(props);
    this.state = {
      url: getBaseUrl() + '/messages?locale={locale}',
    };
  }

  public componentDidMount() {
    void app.initialize().then(() => {
      pages.config.registerOnSaveHandler((saveEvent) => {
        void pages.config.setConfig({
          entityId: 'Company_Communicator_App',
          contentUrl: this.state.url,
          suggestedDisplayName: 'Company Communicator',
        });
        saveEvent.notifySuccess();
      });

      pages.config.setValidityState(true);
    });
  }

  public render(): JSX.Element {
    return (
      <div className='configContainer'>
        <h3>Please click Save to get started.</h3>
      </div>
    );
  }
}

export default Configuration;
