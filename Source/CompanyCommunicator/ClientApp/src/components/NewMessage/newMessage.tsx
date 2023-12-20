/* eslint-disable @typescript-eslint/dot-notation */
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import './newMessage.scss';
import * as AdaptiveCards from 'adaptivecards';
import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import validator from 'validator';
import {
  Button,
  Combobox,
  ComboboxProps,
  Field,
  Input,
  Label,
  LabelProps,
  makeStyles,
  Option,
  Persona,
  Radio,
  RadioGroup,
  RadioGroupOnChangeData,
  shorthands,
  Spinner,
  Text,
  Textarea,
  tokens,
  useId,
  Checkbox,
} from '@fluentui/react-components';
import { InfoLabel } from '@fluentui/react-components/unstable';
import { TimePicker, DatePicker, IComboBox } from '@fluentui/react';
import { initializeIcons } from '@fluentui/react/lib/Icons';
import { ArrowUpload24Regular, Dismiss12Regular } from '@fluentui/react-icons';
import { dialog } from '@microsoft/teams-js';
import {
  GetDraftMessagesSilentAction,
  GetGroupsAction,
  GetTeamsDataAction,
  SearchGroupsAction,
  VerifyGroupAccessAction,
  GetScheduledMessagesSilentAction,
} from '../../actions';
import { createDraftNotification, getDraftNotification, updateDraftNotification } from '../../apis/messageListApi';
import { getBaseUrl } from '../../configVariables';
import { RootState, useAppDispatch, useAppSelector } from '../../store';
import {
  getInitAdaptiveCard,
  setCardAuthor,
  setCardBtn,
  setCardImageLink,
  setCardSummary,
  setCardTitle,
} from '../AdaptiveCard/adaptiveCard';

const validImageTypes = ['image/gif', 'image/jpeg', 'image/png', 'image/jpg'];

interface IMessageState {
  id?: string;
  title: string;
  imageLink?: string;
  summary?: string;
  author?: string;
  buttonTitle?: string;
  buttonLink?: string;
  teams: any[];
  rosters: any[];
  groups: any[];
  allUsers: boolean;
  isScheduled?: boolean;
  scheduledDate?: string;
}

interface ITeamTemplate {
  id: string;
  name: string;
}

const useComboboxStyles = makeStyles({
  root: {
    // Stack the label above the field with a gap
    display: 'grid',
    gridTemplateRows: 'repeat(1fr)',
    justifyItems: 'start',
    ...shorthands.gap('2px'),
    paddingLeft: '36px',
  },
  tagsList: {
    listStyleType: 'none',
    marginBottom: tokens.spacingVerticalXXS,
    marginTop: 0,
    paddingLeft: 0,
    // display: "flex",
    gridGap: tokens.spacingHorizontalXXS,
  },
});

const useFieldStyles = makeStyles({
  styles: {
    marginBottom: tokens.spacingVerticalM,
    gridGap: tokens.spacingHorizontalXXS,
  },
});

enum AudienceSelection {
  Teams = 'Teams',
  Rosters = 'Rosters',
  Groups = 'Groups',
  AllUsers = 'AllUsers',
  None = 'None',
}

enum CurrentPageSelection {
  CardCreation = 'CardCreation',
  AudienceSelection = 'AudienceSelection',
}

let card: any;

const MAX_SELECTED_TEAMS_NUM: number = 20;

initializeIcons(/* optional base url */);

export const NewMessage = () => {
  const fileInput = React.createRef<any>();
  const { t } = useTranslation();
  const { id } = useParams() as any;
  const dispatch = useAppDispatch();
  const teams = useAppSelector((state: RootState) => state.messages).teamsData.payload;
  const groups = useAppSelector((state: RootState) => state.messages).groups.payload;
  const queryGroups = useAppSelector((state: RootState) => state.messages).queryGroups.payload;
  const canAccessGroups = useAppSelector((state: RootState) => state.messages).verifyGroup.payload;
  const [selectedRadioButton, setSelectedRadioButton] = React.useState(AudienceSelection.None);
  const [pageSelection, setPageSelection] = React.useState(CurrentPageSelection.CardCreation);
  const [allUsersState, setAllUsersState] = React.useState(false);
  const [imageFileName, setImageFileName] = React.useState('');
  const [imageUploadErrorMessage, setImageUploadErrorMessage] = React.useState('');
  const [titleErrorMessage, setTitleErrorMessage] = React.useState('');
  const [btnLinkErrorMessage, setBtnLinkErrorMessage] = React.useState('');
  const [showMsgDraftingSpinner, setShowMsgDraftingSpinner] = React.useState(false);
  const [allUsersAria, setAllUserAria] = React.useState('none');
  const [groupsAria, setGroupsAria] = React.useState('none');
  const [cardAreaBorderClass, setCardAreaBorderClass] = React.useState('');
  const [messageState, setMessageState] = React.useState<IMessageState>({
    title: '',
    teams: [],
    rosters: [],
    groups: [],
    allUsers: false,
  });

  // Handle selectedOptions both when an option is selected or deselected in the Combobox,
  // and when an option is removed by clicking on a tag
  const [teamsSelectedOptions, setTeamsSelectedOptions] = React.useState<ITeamTemplate[]>([]);
  const [rostersSelectedOptions, setRostersSelectedOptions] = React.useState<ITeamTemplate[]>([]);
  const [searchSelectedOptions, setSearchSelectedOptions] = React.useState<ITeamTemplate[]>([]);
  const [scheduleSendCheckBox, setScheduleSendCheckBox] = React.useState(false);
  const [scheduledDatePicker, setScheduledDatePicker] = React.useState(
    new Date(new Date().setMinutes(new Date().getMinutes() + 30))
  );
  const [scheduledTimePicker, setScheduledTimePicker] = React.useState(
    new Date(new Date().setMinutes(new Date().getMinutes() + 30))
  );
  const [dbscheduledDate, setDbscheduledDate] = React.useState('');
  const [scheduledSendValidation, setscheduledSendValidation] = React.useState(true);
  const [scheduledSendTimeValidation, setscheduledSendTimeValidation] = React.useState(false);

  React.useEffect(() => {
    GetTeamsDataAction(dispatch);
    VerifyGroupAccessAction(dispatch);
  }, []);

  React.useEffect(() => {
    if (
      !messageState.title &&
      !messageState.imageLink &&
      !messageState.summary &&
      !messageState.author &&
      !messageState.buttonTitle &&
      !messageState.buttonLink
    ) {
      card = getInitAdaptiveCard(t('TitleText') ?? '');
      setDefaultCard(card);
    } else {
      setCardTitle(card, messageState.title);
      setCardImageLink(card, messageState.imageLink);
      setCardSummary(card, messageState.summary);
      setCardAuthor(card, messageState.author);
      setCardBtn(card, messageState.buttonTitle, messageState.buttonLink);
    }
    updateAdaptiveCard();
  }, [t, pageSelection, messageState]);

  React.useEffect(() => {
    if (id) {
      GetGroupsAction(dispatch, { id });
      void getDraftNotificationItem(id);
    }
  }, [id]);

  React.useEffect(() => {
    setTeamsSelectedOptions([]);
    setRostersSelectedOptions([]);
    setSearchSelectedOptions([]);
    setAllUsersState(false);
    if (teams && teams.length > 0) {
      const teamsSelected = teams.filter((c) => messageState.teams.some((s) => s === c.id));
      setTeamsSelectedOptions(teamsSelected || []);
      const roastersSelected = teams.filter((c) => messageState.rosters.some((s) => s === c.id));
      setRostersSelectedOptions(roastersSelected || []);
    }
    if (groups && groups.length > 0) {
      const groupsSelected = groups.filter((c) => messageState.groups.some((s) => s === c.id));
      setSearchSelectedOptions(groupsSelected || []);
    }
    if (messageState.allUsers) {
      setAllUsersState(true);
    }
  }, [teams, groups, messageState.teams, messageState.rosters, messageState.allUsers, messageState.groups]);

  React.useEffect(() => {
    let currentDateTime = new Date();
    currentDateTime = new Date(currentDateTime.setMinutes(currentDateTime.getMinutes() + 30));
    if (scheduleSendCheckBox) {
      if (messageState.scheduledDate === undefined) {
        setscheduledSendValidation(false);
      } else if (
        messageState.scheduledDate &&
        new Date(messageState.scheduledDate) <= new Date(currentDateTime.toISOString())
      ) {
        setscheduledSendValidation(false);
        setscheduledSendTimeValidation(true);
      } else if (
        messageState.scheduledDate &&
        new Date(messageState.scheduledDate) > new Date(currentDateTime.toISOString())
      ) {
        setscheduledSendValidation(true);
        setscheduledSendTimeValidation(false);
      }
    } else {
      setscheduledSendValidation(true);
    }
  }, [scheduleSendCheckBox, messageState.scheduledDate, scheduledSendValidation]);

  const getDraftNotificationItem = async (id: number) => {
    try {
      await getDraftNotification(id).then((response) => {
        const draftMessageDetail = response;

        if (draftMessageDetail.teams.length > 0) {
          setSelectedRadioButton(AudienceSelection.Teams);
        } else if (draftMessageDetail.rosters.length > 0) {
          setSelectedRadioButton(AudienceSelection.Rosters);
        } else if (draftMessageDetail.groups.length > 0) {
          setSelectedRadioButton(AudienceSelection.Groups);
        } else if (draftMessageDetail.allUsers) {
          setSelectedRadioButton(AudienceSelection.AllUsers);
        }
        setMessageState({
          ...messageState,
          id: draftMessageDetail.id,
          title: draftMessageDetail.title,
          imageLink: draftMessageDetail.imageLink,
          summary: draftMessageDetail.summary,
          author: draftMessageDetail.author,
          buttonTitle: draftMessageDetail.buttonTitle,
          buttonLink: draftMessageDetail.buttonLink,
          teams: draftMessageDetail.teams,
          rosters: draftMessageDetail.rosters,
          groups: draftMessageDetail.groups,
          allUsers: draftMessageDetail.allUsers,
          isScheduled: draftMessageDetail.isScheduled,
          scheduledDate: draftMessageDetail.scheduledDate,
        });
        setScheduleSendCheckBox(draftMessageDetail.isScheduled);
        if (draftMessageDetail.scheduledDate !== null) {
          setScheduledDatePicker(new Date(draftMessageDetail.scheduledDate));
          setScheduledTimePicker(new Date(draftMessageDetail.scheduledDate));
          setDbscheduledDate(draftMessageDetail.scheduledDate);
        } else {
          initializeTimePicker();
        }
      });
    } catch (error) {
      return error;
    }
  };

  const setDefaultCard = (card: any) => {
    const titleAsString = t('TitleText');
    const summaryAsString = t('Summary');
    const authorAsString = t('Author1');
    const buttonTitleAsString = t('ButtonTitle');
    setCardTitle(card, titleAsString);
    const imgUrl = getBaseUrl() + '/image/imagePlaceholder.png';
    setCardImageLink(card, imgUrl);
    setCardSummary(card, summaryAsString);
    setCardAuthor(card, authorAsString);
    setCardBtn(card, buttonTitleAsString, 'https://adaptivecards.io');
  };

  const initializeTimePicker = () => {
    setScheduledDatePicker(new Date(new Date().setMinutes(new Date().getMinutes() + 30)));
    setScheduledTimePicker(new Date(new Date().setMinutes(new Date().getMinutes() + 30)));
  };

  // update the state variable whenever the checkbox is checked or unchecked
  const handleScheduleSendCheckBox = (event: any) => {
    setScheduleSendCheckBox((scheduleSendCheckBox) => !scheduleSendCheckBox);
    if (event.target.checked) {
      setMessageState({ ...messageState, isScheduled: true });
    } else {
      setMessageState({ ...messageState, isScheduled: false });
      if (messageState.scheduledDate) {
        setMessageState((current) => {
          const { scheduledDate, ...messageState } = current;
          return messageState;
        });
      }
      initializeTimePicker();
    }
  };

  // update the state variable whenever the date is changed in the date picker control
  const handleScheduleSendDate = (selectedDate: Date | null | undefined) => {
    if (selectedDate) {
      setScheduledDatePicker(selectedDate);
      if (scheduledTimePicker && selectedDate !== scheduledTimePicker) {
        selectedDate?.setHours(scheduledTimePicker.getHours());
        selectedDate?.setMinutes(scheduledTimePicker.getMinutes());
        selectedDate?.setSeconds(scheduledTimePicker.getSeconds());
        setScheduledTimePicker(selectedDate);
      }
    }
    if (dbscheduledDate && selectedDate !== new Date(dbscheduledDate)) {
      const tempDate = selectedDate;
      tempDate?.setHours(scheduledTimePicker.getHours());
      tempDate?.setMinutes(scheduledTimePicker.getMinutes());
      tempDate?.setSeconds(scheduledTimePicker.getSeconds());
      setMessageState({ ...messageState, scheduledDate: tempDate?.toISOString() });
    }
  };
  // update the state variable whenever the time is changed in the time picker control
  const handleScheduleSendTime = (_ev: React.FormEvent<IComboBox>, selectedTime: Date) => {
    if (selectedTime) {
      if (scheduledDatePicker && selectedTime !== scheduledDatePicker) {
        selectedTime?.setDate(scheduledTimePicker.getDate());
      }
      setScheduledTimePicker(selectedTime);
      setMessageState({ ...messageState, scheduledDate: selectedTime.toISOString() });
    }
  };

  const updateAdaptiveCard = () => {
    const adaptiveCard = new AdaptiveCards.AdaptiveCard();
    adaptiveCard.parse(card);
    const renderCard = adaptiveCard.render();
    if (renderCard && pageSelection === CurrentPageSelection.CardCreation) {
      document.getElementsByClassName('card-area-1')[0].innerHTML = '';
      document.getElementsByClassName('card-area-1')[0].appendChild(renderCard);
      setCardAreaBorderClass('card-area-border');
    } else if (renderCard && pageSelection === CurrentPageSelection.AudienceSelection) {
      document.getElementsByClassName('card-area-2')[0].innerHTML = '';
      document.getElementsByClassName('card-area-2')[0].appendChild(renderCard);
      setCardAreaBorderClass('card-area-border');
    }
    adaptiveCard.onExecuteAction = function (action: any) {
      window.open(action.url, '_blank');
    };
  };

  const handleUploadClick = (event: any) => {
    if (fileInput.current) {
      fileInput.current.click();
    }
  };

  const checkValidSizeOfImage = (resizedImageAsBase64: string) => {
    const stringLength = resizedImageAsBase64.length - 'data:image/png;base64,'.length;
    const sizeInBytes = 4 * Math.ceil(stringLength / 3) * 0.5624896334383812;
    const sizeInKb = sizeInBytes / 1000;

    if (sizeInKb <= 1024) return true;
    else return false;
  };

  const handleImageSelection = () => {
    const file = fileInput.current?.files[0];

    if (file) {
      const fileType = file['type'];
      const { type: mimeType } = file;

      if (!validImageTypes.includes(fileType)) {
        setImageUploadErrorMessage(t('ErrorImageTypesMessage') ?? '');
        return;
      }

      setImageFileName(file['name']);
      setImageUploadErrorMessage('');

      const fileReader = new FileReader();
      fileReader.readAsDataURL(file);
      fileReader.onload = () => {
        const image = new Image();
        image.src = fileReader.result as string;
        let resizedImageAsBase64 = fileReader.result as string;

        image.onload = function (e: any) {
          const MAX_WIDTH = 1024;

          if (image.width > MAX_WIDTH) {
            const canvas = document.createElement('canvas');
            canvas.width = MAX_WIDTH;
            canvas.height = ~~(image.height * (MAX_WIDTH / image.width));
            const context = canvas.getContext('2d', { alpha: false });
            if (!context) {
              return;
            }
            context.drawImage(image, 0, 0, canvas.width, canvas.height);
            resizedImageAsBase64 = canvas.toDataURL(mimeType);
          }
        };

        if (!checkValidSizeOfImage(resizedImageAsBase64)) {
          setImageUploadErrorMessage(t('ErrorImageSizeMessage') ?? '');
          return;
        }

        setMessageState({ ...messageState, imageLink: resizedImageAsBase64 });
      };
    }
  };

  const isSaveBtnDisabled = () => {
    const msgPageConditions = messageState.title !== '' && imageUploadErrorMessage === '' && btnLinkErrorMessage === '';
    const audPageConditions =
      (teamsSelectedOptions.length > 0 && selectedRadioButton === AudienceSelection.Teams) ||
      (rostersSelectedOptions.length > 0 && selectedRadioButton === AudienceSelection.Rosters) ||
      (searchSelectedOptions.length > 0 && selectedRadioButton === AudienceSelection.Groups) ||
      selectedRadioButton === AudienceSelection.AllUsers;

    if (msgPageConditions && audPageConditions && scheduledSendValidation) {
      return false;
    } else {
      return true;
    }
  };

  const isNextBtnDisabled = () => {
    if (messageState.title !== '' && imageUploadErrorMessage === '' && btnLinkErrorMessage === '') {
      return false;
    } else {
      return true;
    }
  };

  const onSave = () => {
    let finalSelectedTeams: string[] = [];
    let finalSelectedRosters: string[] = [];
    let finalSelectedGroups: string[] = [];
    let finalAllUsers: boolean = false;

    if (selectedRadioButton === AudienceSelection.Teams) {
      finalSelectedTeams = [
        ...teams.filter((t1) => teamsSelectedOptions.some((sp) => sp.id === t1.id)).map((t2) => t2.id),
      ];
    }
    if (selectedRadioButton === AudienceSelection.Rosters) {
      finalSelectedRosters = [
        ...teams.filter((t1) => rostersSelectedOptions.some((sp) => sp.id === t1.id)).map((t2) => t2.id),
      ];
    }
    if (selectedRadioButton === AudienceSelection.Groups) {
      finalSelectedGroups = [...searchSelectedOptions.map((g) => g.id)];
    }
    if (selectedRadioButton === AudienceSelection.AllUsers) {
      finalAllUsers = allUsersState;
    }

    const finalMessage = {
      ...messageState,
      teams: finalSelectedTeams,
      rosters: finalSelectedRosters,
      groups: finalSelectedGroups,
      allUsers: finalAllUsers,
    };

    setShowMsgDraftingSpinner(true);

    if (id) {
      editDraftMessage(finalMessage);
    } else {
      postDraftMessage(finalMessage);
    }
  };

  const editDraftMessage = (msg: IMessageState) => {
    try {
      updateDraftNotification(msg)
        .then(() => {
          if (msg.isScheduled) {
            GetScheduledMessagesSilentAction(dispatch);
          } else {
            GetDraftMessagesSilentAction(dispatch);
          }
        })
        .finally(() => {
          setShowMsgDraftingSpinner(false);
          dialog.url.submit();
        });
    } catch (error) {
      return error;
    }
  };

  const postDraftMessage = (msg: IMessageState) => {
    try {
      createDraftNotification(msg)
        .then(() => {
          if (msg.isScheduled) {
            GetScheduledMessagesSilentAction(dispatch);
          } else {
            GetDraftMessagesSilentAction(dispatch);
          }
        })
        .finally(() => {
          setShowMsgDraftingSpinner(false);
          dialog.url.submit();
        });
    } catch (error) {
      return error;
    }
  };

  const onNext = (event: any) => {
    setPageSelection(CurrentPageSelection.AudienceSelection);
  };

  const onCancel = (event: any) => {
    dialog.url.submit();
  };

  const onBack = (event: any) => {
    setPageSelection(CurrentPageSelection.CardCreation);
    setAllUserAria('none');
    setGroupsAria('none');
  };

  const onTitleChanged = (event: any) => {
    if (event.target.value === '') {
      setTitleErrorMessage(t('titleRequired') ?? '');
    } else {
      setTitleErrorMessage('');
    }
    setMessageState({ ...messageState, title: event.target.value });
  };

  const onImageLinkChanged = (event: any) => {
    const urlOrDataUrl = event.target.value;
    let isGoodLink = true;
    setImageFileName(urlOrDataUrl);

    if (
      !(
        urlOrDataUrl === '' ||
        urlOrDataUrl.startsWith('https://') ||
        urlOrDataUrl.startsWith('data:image/png;base64,') ||
        urlOrDataUrl.startsWith('data:image/jpeg;base64,') ||
        urlOrDataUrl.startsWith('data:image/gif;base64,')
      )
    ) {
      isGoodLink = false;
      setImageUploadErrorMessage(t('ErrorURLMessage') ?? '');
    } else {
      isGoodLink = true;
      setImageUploadErrorMessage('');
    }

    if (isGoodLink) {
      setMessageState({ ...messageState, imageLink: urlOrDataUrl });
    }
  };

  const onSummaryChanged = (event: any) => {
    setMessageState({ ...messageState, summary: event.target.value });
  };

  const onAuthorChanged = (event: any) => {
    setMessageState({ ...messageState, author: event.target.value });
  };

  const onBtnTitleChanged = (event: any) => {
    setMessageState({ ...messageState, buttonTitle: event.target.value });
  };

  const onBtnLinkChanged = (event: any) => {
    if (
      validator.isURL(event.target.value, { require_protocol: true, protocols: ['https'] }) ||
      event.target.value === ''
    ) {
      setBtnLinkErrorMessage('');
    } else {
      // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
      setBtnLinkErrorMessage(`${event.target.value} is invalid. ${t('enterValidURL')}`);
    }
    setMessageState({ ...messageState, buttonLink: event.target.value });
  };

  // generate ids for handling labelling
  const teamsComboId = useId('teams-combo-multi');
  const teamsSelectedListId = `${teamsComboId}-selection`;

  const rostersComboId = useId('rosters-combo-multi');
  const rostersSelectedListId = `${rostersComboId}-selection`;

  const searchComboId = useId('search-combo-multi');
  const searchSelectedListId = `${searchComboId}-selection`;

  // refs for managing focus when removing tags
  const teamsSelectedListRef = React.useRef<HTMLUListElement>(null);
  const teamsComboboxInputRef = React.useRef<HTMLInputElement>(null);

  const rostersSelectedListRef = React.useRef<HTMLUListElement>(null);
  const rostersComboboxInputRef = React.useRef<HTMLInputElement>(null);

  const searchSelectedListRef = React.useRef<HTMLUListElement>(null);
  const searchComboboxInputRef = React.useRef<HTMLInputElement>(null);

  const onTeamsSelect: ComboboxProps['onOptionSelect'] = (event, data) => {
    if (data.selectedOptions.length <= MAX_SELECTED_TEAMS_NUM) {
      setTeamsSelectedOptions(teams.filter((t1) => data.selectedOptions.some((t2) => t2 === t1.id)));
    }
  };

  const onRostersSelect: ComboboxProps['onOptionSelect'] = (event, data) => {
    if (data.selectedOptions.length <= MAX_SELECTED_TEAMS_NUM) {
      setRostersSelectedOptions(teams.filter((t1) => data.selectedOptions.some((t2) => t2 === t1.id)));
    }
  };

  const onSearchSelect: ComboboxProps['onOptionSelect'] = (event, data: any) => {
    if (data.optionText && !searchSelectedOptions.find((x) => x.id === data.optionValue)) {
      setSearchSelectedOptions([...searchSelectedOptions, { id: data.optionValue, name: data.optionText }]);
    }
  };

  const onSearchChange = (event: any) => {
    if (event?.target?.value) {
      const q = encodeURIComponent(event.target.value);
      SearchGroupsAction(dispatch, { query: q });
    }
  };

  const onTeamsTagClick = (option: ITeamTemplate, index: number) => {
    // remove selected option
    setTeamsSelectedOptions(teamsSelectedOptions.filter((o) => o.id !== option.id));

    // focus previous or next option, defaulting to focusing back to the combo input
    const indexToFocus = index === 0 ? 1 : index - 1;
    const optionToFocus = teamsSelectedListRef.current?.querySelector(`#${teamsComboId}-remove-${indexToFocus}`);
    if (optionToFocus) {
      (optionToFocus as HTMLButtonElement).focus();
    } else {
      teamsComboboxInputRef.current?.focus();
    }
  };

  const onRostersTagClick = (option: ITeamTemplate, index: number) => {
    // remove selected option
    setRostersSelectedOptions(rostersSelectedOptions.filter((o) => o.id !== option.id));

    // focus previous or next option, defaulting to focusing back to the combo input
    const indexToFocus = index === 0 ? 1 : index - 1;
    const optionToFocus = rostersSelectedListRef.current?.querySelector(`#${rostersComboId}-remove-${indexToFocus}`);
    if (optionToFocus) {
      (optionToFocus as HTMLButtonElement).focus();
    } else {
      rostersComboboxInputRef.current?.focus();
    }
  };

  const onSearchTagClick = (option: ITeamTemplate, index: number) => {
    // remove selected option
    setSearchSelectedOptions(searchSelectedOptions.filter((o) => o.id !== option.id));

    // focus previous or next option, defaulting to focusing back to the combo input
    const indexToFocus = index === 0 ? 1 : index - 1;
    const optionToFocus = searchSelectedListRef.current?.querySelector(`#${searchComboId}-remove-${indexToFocus}`);
    if (optionToFocus) {
      (optionToFocus as HTMLButtonElement).focus();
    } else {
      searchComboboxInputRef.current?.focus();
    }
  };

  const teamsLabelledBy = teamsSelectedOptions.length > 0 ? `${teamsComboId} ${teamsSelectedListId}` : teamsComboId;
  const rostersLabelledBy =
    rostersSelectedOptions.length > 0 ? `${rostersComboId} ${rostersSelectedListId}` : rostersComboId;
  const searchLabelledBy =
    searchSelectedOptions.length > 0 ? `${searchComboId} ${searchSelectedListId}` : searchComboId;

  const cmbStyles = useComboboxStyles();
  const fieldStyles = useFieldStyles();

  const audienceSelectionChange = (ev: any, data: RadioGroupOnChangeData) => {
    const input = data.value as keyof typeof AudienceSelection;
    setSelectedRadioButton(AudienceSelection[input]);

    if (AudienceSelection[input] === AudienceSelection.AllUsers) {
      setAllUsersState(true);
    } else if (allUsersState) {
      setAllUsersState(false);
    }

    AudienceSelection[input] === AudienceSelection.AllUsers ? setAllUserAria('alert') : setAllUserAria('none');
    AudienceSelection[input] === AudienceSelection.Groups ? setGroupsAria('alert') : setGroupsAria('none');
  };

  return (
    <>
      {pageSelection === CurrentPageSelection.CardCreation && (
        <>
          <span role='alert' aria-label={t('NewMessageStep1') ?? ''} />
          <div className='adaptive-task-grid'>
            <div className='form-area'>
              <Field
                size='large'
                className={fieldStyles.styles}
                label={t('TitleText')}
                required={true}
                validationMessage={titleErrorMessage}
              >
                <Input
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  placeholder={t('PlaceHolderTitle')!}
                  onChange={onTitleChanged}
                  autoComplete='off'
                  size='large'
                  required={true}
                  appearance='filled-darker'
                  value={messageState.title || ''}
                />
              </Field>
              <Field
                size='large'
                className={fieldStyles.styles}
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
                // @ts-ignore
                label={{
                  children: (_: unknown, imageInfoProps: LabelProps) => (
                    <InfoLabel {...imageInfoProps} info={t('ImageSizeInfoContent') ?? ''}>
                      {t('ImageURL')}
                    </InfoLabel>
                  ),
                }}
                validationMessage={imageUploadErrorMessage}
              >
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: '1fr auto',
                    gridTemplateAreas: 'input-area btn-area',
                  }}
                >
                  <Input
                    size='large'
                    style={{ gridColumn: '1' }}
                    appearance='filled-darker'
                    value={imageFileName || ''}
                    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                    placeholder={t('PlaceHolderImageURL')!}
                    onChange={onImageLinkChanged}
                  />
                  <div
                    style={{
                      gridColumn: '2',
                      marginLeft: '8px',
                      marginRight: '5px',
                      paddingTop: '8px',
                      color: 'darkgray',
                    }}
                  >
                    {' '}
                    {t('FieldSeperator')}{' '}
                  </div>
                  {
                    // eslint-disable-next-line @typescript-eslint/ban-ts-comment, @typescript-eslint/prefer-ts-expect-error
                    // @ts-ignore
                    <Button
                      style={{ gridColumn: '3', marginLeft: '5px' }}
                      onClick={handleUploadClick}
                      size='large'
                      appearance='secondary'
                      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                      aria-label={imageFileName ? t('UploadImageSuccessful') : t('UploadImageInfo')}
                      icon={<ArrowUpload24Regular />}
                    >
                      {t('Upload')}
                    </Button>
                  }
                  <input
                    type='file'
                    accept='.jpg, .jpeg, .png, .gif'
                    aria-label='input file upload (hidden)'
                    style={{ display: 'none' }}
                    multiple={false}
                    onChange={handleImageSelection}
                    ref={fileInput}
                  />
                </div>
              </Field>
              <Field size='large' className={fieldStyles.styles} label={t('Summary')}>
                <Textarea
                  size='large'
                  resize='vertical'
                  appearance='filled-darker'
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  placeholder={t('PlaceHolderSummary')!}
                  value={messageState.summary ?? ''}
                  onChange={onSummaryChanged}
                />
              </Field>
              <Field size='large' className={fieldStyles.styles} label={t('Author')}>
                <Input
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  placeholder={t('PlaceHolderAuthor')!}
                  size='large'
                  onChange={onAuthorChanged}
                  autoComplete='off'
                  appearance='filled-darker'
                  value={messageState.author ?? ''}
                />
              </Field>
              <Field size='large' className={fieldStyles.styles} label={t('ButtonTitle')}>
                <Input
                  size='large'
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  placeholder={t('PlaceHolderButtonTitle')!}
                  onChange={onBtnTitleChanged}
                  autoComplete='off'
                  appearance='filled-darker'
                  value={messageState.buttonTitle ?? ''}
                />
              </Field>
              <Field
                size='large'
                className={fieldStyles.styles}
                label={t('ButtonURL')}
                validationMessage={btnLinkErrorMessage}
              >
                <Input
                  size='large'
                  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                  placeholder={t('PlaceHolderButtonURL')!}
                  onChange={onBtnLinkChanged}
                  type='url'
                  autoComplete='off'
                  appearance='filled-darker'
                  value={messageState.buttonLink ?? ''}
                />
              </Field>
            </div>
            <div className='card-area'>
              <div className={cardAreaBorderClass}>
                <div className='card-area-1'></div>
              </div>
            </div>
          </div>
          <div className='fixed-footer'>
            <div className='footer-action-right'>
              <Button id='cancelBtn' onClick={onCancel} appearance='secondary' style={{ marginRight: '16px' }}>
                {t('Cancel')}
              </Button>
              <Button disabled={isNextBtnDisabled()} id='saveBtn' onClick={onNext} appearance='primary'>
                {t('Next')}
              </Button>
            </div>
          </div>
        </>
      )}
      {pageSelection === CurrentPageSelection.AudienceSelection && (
        <>
          <span role='alert' aria-label={t('NewMessageStep2') ?? ''} />
          <div className='adaptive-task-grid new-messages'>
            <div className='form-area'>
              <Label size='large' id='audienceSelectionGroupLabelId'>
                {t('SendHeadingText')}
              </Label>
              <RadioGroup
                defaultValue={selectedRadioButton}
                aria-labelledby='audienceSelectionGroupLabelId'
                onChange={audienceSelectionChange}
              >
                <Radio id='radio1' value={AudienceSelection.Teams} label={t('SendToGeneralChannel')} />
                {selectedRadioButton === AudienceSelection.Teams && (
                  <div className={cmbStyles.root}>
                    <Label id={teamsComboId}>{t('pickTeams')}</Label>
                    {
                      // eslint-disable-next-line multiline-ternary
                      teamsSelectedOptions.length ? (
                        <ul id={teamsSelectedListId} className={cmbStyles.tagsList} ref={teamsSelectedListRef}>
                          {/* The "Remove" span is used for naming the buttons without affecting the Combobox name */}
                          <span id={`${teamsComboId}-remove`} hidden>
                            {t('remove')}
                          </span>
                          {teamsSelectedOptions.map((option, i) => (
                            <li key={option.id}>
                              <Button
                                size='small'
                                shape='rounded'
                                appearance='subtle'
                                icon={<Dismiss12Regular />}
                                iconPosition='after'
                                // eslint-disable-next-line @typescript-eslint/no-confusing-void-expression
                                onClick={() => onTeamsTagClick(option, i)}
                                id={`${teamsComboId}-remove-${i}`}
                                aria-labelledby={`${teamsComboId}-remove ${teamsComboId}-remove-${i}`}
                              >
                                <Persona
                                  name={option.name}
                                  secondaryText={'Team'}
                                  avatar={{ shape: 'square', color: 'colorful' }}
                                />
                              </Button>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <></>
                      )
                    }
                    <Combobox
                      multiselect={true}
                      selectedOptions={teamsSelectedOptions.map((op) => op.id)}
                      appearance='filled-darker'
                      size='large'
                      onOptionSelect={onTeamsSelect}
                      ref={teamsComboboxInputRef}
                      aria-labelledby={teamsLabelledBy}
                      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                      placeholder={teams.length !== 0 ? t('pickOneOrMoreTeams')! : t('NoMatchMessage')!}
                    >
                      {teams.map((opt) => (
                        <Option text={opt.name} value={opt.id} key={opt.id}>
                          <Persona
                            name={opt.name}
                            secondaryText={'Team'}
                            avatar={{ shape: 'square', color: 'colorful' }}
                          />
                        </Option>
                      ))}
                    </Combobox>
                  </div>
                )}
                <Radio id='radio2' value={AudienceSelection.Rosters} label={t('SendToRosters')} />
                {selectedRadioButton === AudienceSelection.Rosters && (
                  <div className={cmbStyles.root}>
                    <Label id={rostersComboId}>{t('pickTeams')}</Label>
                    {
                      // eslint-disable-next-line multiline-ternary
                      rostersSelectedOptions.length ? (
                        <ul id={rostersSelectedListId} className={cmbStyles.tagsList} ref={rostersSelectedListRef}>
                          {/* The "Remove" span is used for naming the buttons without affecting the Combobox name */}
                          <span id={`${rostersComboId}-remove`} hidden>
                            {t('remove')}
                          </span>
                          {rostersSelectedOptions.map((option, i) => (
                            <li key={option.id}>
                              <Button
                                size='small'
                                shape='rounded'
                                appearance='subtle'
                                icon={<Dismiss12Regular />}
                                iconPosition='after'
                                // eslint-disable-next-line @typescript-eslint/no-confusing-void-expression
                                onClick={() => onRostersTagClick(option, i)}
                                id={`${rostersComboId}-remove-${i}`}
                                aria-labelledby={`${rostersComboId}-remove ${rostersComboId}-remove-${i}`}
                              >
                                <Persona
                                  name={option.name}
                                  secondaryText={'Team'}
                                  avatar={{ shape: 'square', color: 'colorful' }}
                                />
                              </Button>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <></>
                      )
                    }
                    <Combobox
                      multiselect={true}
                      selectedOptions={rostersSelectedOptions.map((op) => op.id)}
                      appearance='filled-darker'
                      size='large'
                      onOptionSelect={onRostersSelect}
                      ref={rostersComboboxInputRef}
                      aria-labelledby={rostersLabelledBy}
                      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                      placeholder={teams.length !== 0 ? t('pickOneOrMoreTeams')! : t('NoMatchMessage')!}
                    >
                      {teams.map((opt) => (
                        <Option text={opt.name} value={opt.id} key={opt.id}>
                          <Persona
                            name={opt.name}
                            secondaryText={'Team'}
                            avatar={{ shape: 'square', color: 'colorful' }}
                          />
                        </Option>
                      ))}
                    </Combobox>
                  </div>
                )}
                <Radio id='radio3' value={AudienceSelection.AllUsers} label={t('SendToAllUsers')} />
                <div className={cmbStyles.root}>
                  {selectedRadioButton === AudienceSelection.AllUsers && (
                    <Text id='radio3Note' role={allUsersAria} className='info-text'>
                      {t('SendToAllUsersNote')}
                    </Text>
                  )}
                </div>
                <Radio id='radio4' value={AudienceSelection.Groups} label={t('SendToGroups')} />
                {selectedRadioButton === AudienceSelection.Groups && (
                  <div className={cmbStyles.root}>
                    {!canAccessGroups && (
                      <Text role={groupsAria} className='info-text'>
                        {t('SendToGroupsPermissionNote')}
                      </Text>
                    )}
                    {canAccessGroups && (
                      <>
                        <Label id={searchComboId}>{t('pickGroups')}</Label>
                        {
                          // eslint-disable-next-line multiline-ternary
                          searchSelectedOptions.length ? (
                            <ul id={searchSelectedListId} className={cmbStyles.tagsList} ref={searchSelectedListRef}>
                              {/* The "Remove" span is used for naming the buttons without affecting the Combobox name */}
                              <span id={`${searchComboId}-remove`} hidden>
                                {t('remove')}
                              </span>
                              {searchSelectedOptions.map((option, i) => (
                                <li key={option.id}>
                                  <Button
                                    size='small'
                                    shape='rounded'
                                    appearance='subtle'
                                    icon={<Dismiss12Regular />}
                                    iconPosition='after'
                                    // eslint-disable-next-line @typescript-eslint/no-confusing-void-expression
                                    onClick={() => onSearchTagClick(option, i)}
                                    id={`${searchComboId}-remove-${i}`}
                                    aria-labelledby={`${searchComboId}-remove ${searchComboId}-remove-${i}`}
                                  >
                                    <Persona
                                      name={option.name}
                                      secondaryText={'Group'}
                                      avatar={{ color: 'colorful' }}
                                    />
                                  </Button>
                                </li>
                              ))}
                            </ul>
                          ) : (
                            <></>
                          )
                        }
                        <Combobox
                          appearance='filled-darker'
                          size='large'
                          onOptionSelect={onSearchSelect}
                          onChange={onSearchChange}
                          aria-labelledby={searchLabelledBy}
                          placeholder={t('searchForGroups') ?? ''}
                        >
                          {queryGroups.map((opt) => (
                            <Option text={opt.name} value={opt.id} key={opt.id}>
                              <Persona name={opt.name} secondaryText={'Group'} avatar={{ color: 'colorful' }} />
                            </Option>
                          ))}
                        </Combobox>
                        <Text role={groupsAria} className='info-text'>
                          {t('SendToGroupsNote')}
                        </Text>
                      </>
                    )}
                  </div>
                )}
              </RadioGroup>
              <div>
                <></>
              </div>
              <div>
                <Label size='large' id='MoreOptionsLabelId'>
                  {t('MoreOptions')}
                </Label>
              </div>
              <Checkbox
                id='ScheduleCheckbox'
                label={t('ScheduleSend')}
                defaultChecked={scheduleSendCheckBox}
                onChange={handleScheduleSendCheckBox}
              />
              {scheduleSendCheckBox && (
                <div>
                  <Label
                    id='ScheduleSection'
                    className='info-text'
                    style={{ marginBottom: '5px', display: 'block', marginLeft: '36px' }}
                  >
                    {t('ScheduleSection')}
                  </Label>
                  <Text
                    id='ScheduleNote'
                    className='info-text'
                    style={{ marginBottom: '5px', display: 'block', marginLeft: '36px' }}
                  >
                    {t('ScheduleNote')}
                  </Text>
                  <div className='flex-container schedulesend-datetime'>
                    <DatePicker
                      value={scheduledDatePicker}
                      onSelectDate={handleScheduleSendDate}
                      minDate={new Date()}
                      placeholder='Select a date'
                      ariaLabel={'Scheduled Date required'}
                      className='schedule-datepicker'
                      calloutProps={{ className: 'incidentdatepicker-callout' }}
                    />
                    <TimePicker
                      dateAnchor={scheduledDatePicker}
                      value={scheduledTimePicker}
                      placeholder='Select a time'
                      onChange={handleScheduleSendTime}
                      calloutProps={{ directionalHintFixed: true, doNotLayer: true }}
                      ariaLabel={'Scheduled Time required'}
                      className='schedule-timepicker'
                      useHour12={true}
                      allowFreeform={false}
                    />
                  </div>
                  {scheduledSendTimeValidation && (
                    <div className='validationText'>
                      <Text role='alert'>{t('ScheduleTimeValidation')}</Text>
                    </div>
                  )}
                </div>
              )}
            </div>
            <div className='card-area'>
              <div className={cardAreaBorderClass}>
                <div className='card-area-2'></div>
              </div>
            </div>
          </div>
          <div>
            <div className='fixed-footer'>
              <div className='footer-action-right'>
                <div className='footer-actions-flex'>
                  {showMsgDraftingSpinner && (
                    <Spinner
                      role='alert'
                      id='draftingLoader'
                      size='small'
                      label={t('DraftingMessageLabel')}
                      labelPosition='after'
                    />
                  )}
                  <Button
                    id='backBtn'
                    style={{ marginLeft: '16px' }}
                    onClick={onBack}
                    disabled={showMsgDraftingSpinner}
                    appearance='secondary'
                  >
                    {t('Back')}
                  </Button>
                  <Button
                    style={{ marginLeft: '16px' }}
                    disabled={isSaveBtnDisabled() || showMsgDraftingSpinner}
                    id='saveBtn'
                    onClick={onSave}
                    appearance='primary'
                  >
                    {scheduleSendCheckBox ? t('Schedule') : t('SaveAsDraft')}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </>
  );
};
