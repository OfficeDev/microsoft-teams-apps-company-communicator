// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ROUTE_PARTS } from '../routes';
import i18n from '../i18n';
import { authentication } from '@microsoft/teams-js';

export class ApiDecorator {
  public async getJsonResponse(url: string): Promise<any> {
    return await this.handleApiCall('get', url).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('get', response.url).then((result) => result.json());
      } else if (response.status >= 401) {
        this.handleError(response);
      } else {
        return response.json();
      }
    });
  }

  public async getTextResponse(url: string): Promise<any> {
    return await this.handleApiCall('get', url).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('get', response.url).then((result) => result.text());
      } else if (response.status >= 401) {
        this.handleError(response);
      } else {
        return response.text();
      }
    });
  }

  public async postAndGetJsonResponse(url: string, data?: any): Promise<any> {
    return await this.handleApiCall('post', url, data).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('post', response.url, data).then((result) => result.json());
      } else {
        return response.json();
      }
    });
  }

  public async postAndGetTextResponse(url: string, data?: any): Promise<any> {
    return await this.handleApiCall('post', url, data).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('post', response.url, data).then((result) => result.text());
      } else {
        return response.text();
      }
    });
  }

  public async putAndGetJsonResponse(url: string, data?: any): Promise<any> {
    return await this.handleApiCall('put', url, data).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('put', response.url, data).then((result) => result.json());
      } else {
        return response.json();
      }
    });
  }

  public async putAndGetTextResponse(url: string, data?: any): Promise<any> {
    return await this.handleApiCall('put', url, data).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('put', response.url, data).then((result) => result.text());
      } else {
        return response.text();
      }
    });
  }

  public async deleteAndGetJsonResponse(url: string): Promise<any> {
    return await this.handleApiCall('delete', url).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('delete', response.url).then((result) => result.json());
      } else {
        return response.json();
      }
    });
  }

  public async deleteAndGetTextResponse(url: string): Promise<any> {
    return await this.handleApiCall('delete', url).then((response) => {
      if (response.type === 'cors' && response.status >= 401) {
        return this.handleApiCall('delete', response.url).then((result) => result.text());
      } else {
        return response.text();
      }
    });
  }

  private async handleApiCall(verb: string, url: string, data: any = {}): Promise<any> {
    const token = await authentication.getAuthToken();

    try {
      switch (verb) {
        case 'get':
          return await fetch(url, {
            method: 'GET',
            headers: { Accept: 'application/json', 'content-type': 'application/json', Authorization: 'Bearer ' + token },
          });
        case 'post':
          return await fetch(url, {
            method: 'POST',
            headers: { Accept: 'application/json', 'content-type': 'application/json', Authorization: 'Bearer ' + token },
            body: JSON.stringify(data),
          });
        case 'put':
          return await fetch(url, {
            method: 'PUT',
            headers: { Accept: 'application/json', 'content-type': 'application/json', Authorization: 'Bearer ' + token },
            body: JSON.stringify(data),
          });
        case 'delete':
          return await fetch(url, {
            method: 'DELETE',
            headers: { Accept: 'application/json', 'content-type': 'application/json', Authorization: 'Bearer ' + token },
            body: JSON.stringify(data),
          });
        default:
          return await fetch(url, {
            method: 'GET',
            headers: { Accept: 'application/json', 'content-type': 'application/json', Authorization: 'Bearer ' + token },
          });
      }
    } catch (error) {
      this.handleError(error);
      throw error;
    }
  }

  private handleError(error: any): void {
    // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
    // @ts-ignore
    const lang: string = i18n.language;

    if (error?.status) {
      if (error.status === 403) {
        window.location.href = `/${ROUTE_PARTS.ERROR_PAGE}/403?locale=${lang}`;
      } else if (error.status === 401) {
        window.location.href = `/${ROUTE_PARTS.ERROR_PAGE}/401?locale=${lang}`;
      } else {
        window.location.href = `/${ROUTE_PARTS.ERROR_PAGE}?locale=${lang}`;
      }
    } else {
      window.location.href = `/${ROUTE_PARTS.ERROR_PAGE}?locale=${lang}`;
    }
  }
}

const apiCallDecoratorInstance = new ApiDecorator();
export default apiCallDecoratorInstance;
