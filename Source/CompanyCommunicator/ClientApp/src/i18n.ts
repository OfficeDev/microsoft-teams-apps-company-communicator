// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import 'moment/min/locales.min';
import i18n from 'i18next';
import Backend from 'i18next-http-backend';
import moment from 'moment';
import { initReactI18next } from 'react-i18next';

export const defaultLocale = () => {
  return 'en-US';
};

void i18n
  // load translation using http -> see /public/locales (i.e. https://github.com/i18next/react-i18next/tree/main/example/react/public/locales)
  // learn more: https://github.com/i18next/i18next-http-backend
  // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
  // @ts-ignore
  .use(Backend)
  // pass the i18n instance to react-i18next.
  .use(initReactI18next)
  // init i18next
  // for all options read: https://www.i18next.com/overview/configuration-options
  .init({
    fallbackLng: defaultLocale(),
    interpolation: {
      escapeValue: false, // not needed for react as it escapes by default
    },
    react: {
      useSuspense: false,
    },
  });

export const updateLocale = () => {
  const search = window.location.search;
  const params = new URLSearchParams(search);
  const locale = params.get('locale') ?? defaultLocale();
  // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
  // @ts-ignore
  void i18n.changeLanguage(locale);
  moment.locale(locale);
};

export const formatDate = (date: string) => {
  if (!date) return date;
  return moment(date).format('l LT');
};

export const formatDuration = (startDate: string, endDate: string) => {
  let result = '';
  const search = window.location.search;
  const params = new URLSearchParams(search);
  const locale = params.get('locale') ?? defaultLocale();
  if (startDate && endDate) {
    const difference = moment(endDate).diff(moment(startDate));
    const totalDuration = moment.duration(difference);
    // Handling the scenario of duration being more than 24 hrs as it is not done by moment.js.
    // eslint-disable-next-line @typescript-eslint/restrict-plus-operands
    const hh = ('0' + Math.floor(totalDuration.asHours())).slice(-2);
    result = formatNumber(parseInt(hh)) + moment.utc(totalDuration.asMilliseconds()).locale(locale).format(':mm:ss');
  }
  return result;
};

export const formatNumber = (number: any) => {
  const search = window.location.search;
  const params = new URLSearchParams(search);
  const locale = params.get('locale') ?? defaultLocale();
  return Number(number).toLocaleString(locale);
};

export default i18n;
