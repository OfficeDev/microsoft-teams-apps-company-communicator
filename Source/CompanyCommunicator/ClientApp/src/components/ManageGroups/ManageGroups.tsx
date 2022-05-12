import { ArrowRightIcon, TrashCanIcon, FilesUploadIcon } from '@fluentui/react-icons-northstar';
import { Button, Dropdown, Flex, Image, Layout, Label, List, Text, Loader, Input } from '@fluentui/react-northstar';
import * as microsoftTeams from "@microsoft/teams-js";
import { TFunction } from "i18next";
import * as React from 'react';
import { withTranslation, WithTranslation } from "react-i18next";
import { RouteComponentProps } from 'react-router-dom';
import { createGroupAssociation, searchGroups, getGroupAssociations, deleteGroupAssociation, updateChannelConfig, getChannelConfig } from "../../apis/messageListApi";
import { ImageUtil } from '../../utility/imageutility';
import './ManageGroups.scss';
import Resizer from 'react-image-file-resizer';

//max size of the card 
const maxCardSize = 30720;

type dropdownItem = {
    key: string,
    header: string,
    content: string,
    image: string,
    team: {
        id: string,
    },
}

export interface IGroup {
    GroupId: string,
    GroupName: string,
    GroupEmail: string,
    ChannelId?: string,
}

export interface IChannel {
    ChannelId: string,
    ChannelTitle: string,
    ChannelImage: string,
}

export interface formState {
    loading: boolean,
    loader: boolean,
    channelId?: string, //id of the channel where the message was created
    channelName?: string,
    teamName?: string,
    userPrincipalName?: string,
    groups?: any[],
    groupAccess: boolean,
    noResultMessage: string,
    selectedGroups: dropdownItem[],
    selectedGroupsNum: number,
    allGroups: dropdownItem[],
    allGroupsNum: number,
    groupAlreadyIncluded: boolean,
    imageLink?: string,
    errorImageUrlMessage: string,
    channelTitle?: string,
}

export interface IManageGroupsProps extends RouteComponentProps, WithTranslation {
}

class ManageGroups extends React.Component<IManageGroupsProps, formState> {
    readonly localize: TFunction;
    targetingEnabled: boolean; // property to store value indicating if the targeting mode is enabled or not
    masterAdminUpns: string; // property to store value with the master admins
    fileInput: any;

    constructor(props: IManageGroupsProps) {
        super(props);
        this.localize = this.props.t;
        this.targetingEnabled = false; // by default targeting is disabled
        this.masterAdminUpns = "";
        this.state = {
            loading: false,
            loader: true,
            channelId: "",
            channelName: "",
            teamName: "",
            userPrincipalName: "",
            groupAccess: false,
            noResultMessage: "",
            groupAlreadyIncluded: false,
            selectedGroups: [],
            selectedGroupsNum: 0,
            allGroups: [],
            allGroupsNum: 0,
            imageLink: "",
            errorImageUrlMessage: "",
            channelTitle: "",
        }
        this.escFunction = this.escFunction.bind(this);
        this.fileInput = React.createRef();
        this.handleImageSelection = this.handleImageSelection.bind(this);
    }

    public componentDidMount() {
        const setState = this.setState.bind(this);

        microsoftTeams.initialize();
        document.addEventListener("keydown", this.escFunction, false);

        microsoftTeams.getContext(context => {
            setState({
                channelId: context.channelId,
                channelName: context.channelName,
                teamName: context.teamName,
                userPrincipalName: context.userPrincipalName
            });

            //get all associated groups and set the allGroups and allGroupsNum state
            this.getAllGroupsAssociated();

            //get the channel configuration from the database
            this.GetChannelInfo(context.channelId);
        });

    }

    public componentWillUnmount() {
        document.removeEventListener("keydown", this.escFunction, false);
    }

    public render(): JSX.Element {
        return (
            <div>
                {(this.state.loader) &&
                    <div className="Loader">
                        <Loader />
                    </div>}
                {(!this.state.loader) &&
                    this.renderPage()}
            </div>
        );
    }

    public escFunction(event: any) {
        if (event.keyCode === 27 || (event.key === "Escape")) {
            microsoftTeams.tasks.submitTask();
        }
    }

    //function to handle the selection of the OS file upload box
    private handleImageSelection() {
        //get the first file selected
        const file = this.fileInput.current.files[0];
        if (file) { //if we have a file
            //resize the image to fit in the adaptivecard
            Resizer.imageFileResizer(file, 400, 100, 'JPEG', 80, 0,
                uri => {
                    if (uri.toString().length < maxCardSize) {
                        //lets set the state with the image value
                        this.setState({
                            imageLink: uri.toString()
                        }
                        );
                    } else {
                        //images bigger than 32K cannot be saved, set the error message to be presented
                        this.setState({
                            errorImageUrlMessage: this.localize("ErrorImageTooBig")
                        });
                    }

                },
                'base64'); //we need the image in base64
        }
    }

    //Function calling a click event on a hidden file input
    private handleUploadClick = (event: any) => {
        //reset the error message and the image link as the upload will reset them potentially
        this.setState({
            errorImageUrlMessage: "",
            imageLink: ""
        });
        //fire the fileinput click event and run the handleimageselection function
        this.fileInput.current.click();
    };

    private renderPage = () => {
        return (
            <div className="taskModule">
                <Flex column className="formContainer" vAlign="stretch" gap="gap.small" styles={{ background: "white" }}>
                    <Flex className="nonScrollableContent">
                        <Flex.Item size="size.half">
                            <Flex column className="formContentContainer">
                                <div style={{ minHeight: 30 }} />
                                <div style={{ minHeight: 40 }}>
                                    <Label circular content={this.state.teamName} />
                                    <Label circular content={this.state.channelName} />
                                </div>
                                <div>
                                    <Text content={this.localize("CardImage")} />
                                </div>
                                <div>
                                    <Layout
                                        styles={{ maxWidth: '400px', maxHeight: '110px' }}
                                        renderMainArea={() => <Image src={this.state.imageLink} />}
                                    />
                                </div>
                                <div style={{ minHeight: 40 }}>
                                    <Flex gap="gap.smaller" vAlign="end" className="inputField">
                                        <Input
                                            value={this.state.imageLink}
                                            placeholder={this.localize("ImageURLPlaceHolder")}
                                            onChange={this.onImageLinkChanged}
                                            error={!(this.state.errorImageUrlMessage === "")}
                                            autoComplete="off"
                                            fluid
                                        />
                                        <input type="file" accept="image/"
                                            style={{ display: 'none' }}
                                            onChange={this.handleImageSelection}
                                            ref={this.fileInput} />
                                        <Flex.Item push>
                                            <Button circular onClick={this.handleUploadClick}
                                                size="small"
                                                icon={<FilesUploadIcon />}
                                                title={this.localize("UploadImage")}
                                            />
                                        </Flex.Item>

                                    </Flex>
                                </div>
                                <div style={{ minHeight: 60 }}>
                                    <Input
                                        value={this.state.channelTitle}
                                        onChange={this.onChannelTitleChange}
                                        label={this.localize("CardTitle")}
                                        fluid
                                    />
                                </div>
                                <div>
                                    <Text content={this.localize("TargetGroups")} />
                                    <Flex gap="gap.small">
                                        <Dropdown
                                            search
                                            placeholder={this.localize("SendToGroupsPlaceHolder")}
                                            loadingMessage={this.localize("LoadingText")}
                                            onSearchQueryChange={this.onGroupSearchQueryChange}
                                            noResultsMessage={this.state.noResultMessage}
                                            loading={this.state.loading}
                                            items={this.getGroupItems()}
                                            onChange={this.onGroupsChange}
                                            value={this.state.selectedGroups}
                                            multiple
                                        />
                                        <Flex.Item><Button content="Add" icon={<ArrowRightIcon />} iconPosition="after" text onClick={this.onAddGroups} /></Flex.Item>
                                    </Flex>
                                </div>
                                <div className={this.state.groupAlreadyIncluded ? "ErrorMessage" : "hide"}>
                                    <div className="noteText">
                                        <Text error content={this.localize('GroupAlreadyIncluded')} />
                                    </div>
                                </div>
                            </Flex>
                        </Flex.Item>
                        <Flex.Item size="size.half">
                            <div>
                                <Text align="center" content={this.localize("TargetGroups") + ' for ' + this.state.teamName + '/' + this.state.channelName} />
                                <div className="scrollableContent">
                                    <List items={this.state.allGroups} selectable />
                                </div>
                            </div>
                        </Flex.Item>
                    </Flex>
                </Flex>
                <Flex className="footerContainer" vAlign="end" hAlign="end">
                    <Flex className="buttonContainer" gap="gap.medium">
                        <Button content={this.localize('CloseText')} onClick={this.onClose} />
                    </Flex>
                </Flex>
            </div>
        );
    }

    private onClose = () => {
        //collects values from state and build the draftChannel
        const draftChannel: IChannel = {
            ChannelImage: this.state.imageLink,
            ChannelId: this.state.channelId,
            ChannelTitle: this.state.channelTitle
        }

        //update the channel configuration and submit the task 
        this.UpdateChannelConfig(draftChannel).then(() => {
            microsoftTeams.tasks.submitTask();
        });
    }

    //update or create a new channel configuration based on draftChannel
    private UpdateChannelConfig = async (draftChannel: IChannel) => {
        try {
            await updateChannelConfig(draftChannel);
        } catch (error) {
            return error;
        }
    }

    //get the channel configuration 
    private GetChannelInfo = async (channelid: string) => {
        try {
            const response = await getChannelConfig(channelid);
            const draftChannel = response.data;
            this.setState({
                imageLink: draftChannel.channelImage,
                channelTitle: draftChannel.channelTitle,
            });
        } catch (error) {
            return error;
        }
    }

    private onChannelTitleChange = (event: any) => {
        this.setState({
            channelTitle: event.target.value,
        });
    }

    private getGroupItems() {
        if (this.state.groups) {
            return this.makeDropdownItems(this.state.groups);
        }
        const dropdownItems: dropdownItem[] = [];
        return dropdownItems;
    }

    private onImageLinkChanged = (event: any) => {
        let url = event.target.value.toLowerCase();
        if (!((url === "") || (url.startsWith("https://") || (url.startsWith("data:image/png;base64,")) || (url.startsWith("data:image/jpeg;base64,")) || (url.startsWith("data:image/gif;base64,"))))) {
            this.setState({
                errorImageUrlMessage: this.localize("ErrorURLMessage")
            });
        } else {
            this.setState({
                errorImageUrlMessage: ""
            });
        }

        this.setState({
            imageLink: event.target.value,
        });
    }

    private makeDropdownItems = (items: any[] | undefined) => {
        const resultedTeams: dropdownItem[] = [];
        if (items) {
            items.forEach((element) => {
                resultedTeams.push({
                    key: element.id,
                    header: element.name,
                    content: element.mail,
                    image: ImageUtil.makeInitialImage(element.name),
                    team: {
                        id: element.id,
                    },
                });
            });
        }
        return resultedTeams;
    }

    //executed when a group is added from the combo to the list
    private onAddGroups = () => {
        //for each one of the selected groups
        this.state.selectedGroups.forEach((element) => {
            //create a draft group based on IGroup interface
            var draftGroup: IGroup = {
                GroupId: element.key,
                GroupName: element.header,
                GroupEmail: element.content,
                ChannelId: this.state.channelId,
            }
            //If the group is not already on the list of associated groups, 
            //add the draftGroup to the database calling the webservice
            if (!this.state.allGroups.some(e => e.key === element.key)) {
                //add to the database
                this.saveGroup(draftGroup).then(() => {
                    //clears the combo box with selected groups
                    this.setState({
                        selectedGroups: [],
                        selectedGroupsNum: 0,
                    });

                    //refresh the list of associated groups
                    this.getAllGroupsAssociated();
                });
                //inputItems.push(draftGroup); //temporary, need to call the web service
            } else {
                this.setState({
                    groupAlreadyIncluded: true,
                });
            }
        });
    }

    //called to delete a group from the list
    private onDeleteGroup(id: number, key: string) {
        //removes from the list
        //this.state.allGroups.splice(id, 1);
        this.deleteGroup(key).then(() => {
            this.getAllGroupsAssociated();
        });
    }

    private deleteGroup = async (key: string) => {
        try {
            await deleteGroupAssociation(key);
        } catch (error) {
            return error;
        }
    }

    private saveGroup = async (draftGroup: IGroup) => {
        try {
            await createGroupAssociation(draftGroup);
        } catch (error) {
            return error;
        }
    }

    private getAllGroupsAssociated = async () => {
        var resultListItems: any[] = [];

        try {
            //get inputGroups from database
            const response = await getGroupAssociations(this.state.channelId);
            const inputGroups = response.data;
            var x = 0;
            inputGroups.forEach((element) => {
                resultListItems.push({
                    id: x,
                    key: element.groupId,
                    header: element.groupName,
                    content: element.groupEmail,
                    endMedia: <Button circular size="small" onClick={this.onDeleteGroup.bind(this, x, element.rowKey)} icon={<TrashCanIcon />} />,
                    media: <Image src={ImageUtil.makeInitialImage(element.groupName)} avatar />
                });
                x++;
            });

            this.setState({
                allGroups: resultListItems,
                allGroupsNum: resultListItems.length,
                loader: false,
            });
        } catch (error) {
            return error;
        }
    }

    private onGroupsChange = (event: any, itemsData: any) => {
        this.setState({
            selectedGroups: itemsData.value,
            selectedGroupsNum: itemsData.value.length,
            groups: [],
            groupAlreadyIncluded: false,
        })
    }

    private onGroupSearchQueryChange = async (event: any, itemsData: any) => {

        if (!itemsData.searchQuery) {
            this.setState({
                groups: [],
                noResultMessage: "",
            });
        }
        else if (itemsData.searchQuery && itemsData.searchQuery.length <= 2) {
            this.setState({
                loading: false,
                noResultMessage: this.localize("NoMatchMessage"),
            });
        }
        else if (itemsData.searchQuery && itemsData.searchQuery.length > 2) {
            // handle event trigger on item select.
            const result = itemsData.items && itemsData.items.find(
                (item: { header: string; }) => item.header.toLowerCase() === itemsData.searchQuery.toLowerCase()
            )
            if (result) {
                return;
            }

            this.setState({
                loading: true,
                noResultMessage: "",
            });

            try {
                const query = encodeURIComponent(itemsData.searchQuery);
                const response = await searchGroups(query);
                this.setState({
                    groups: response.data,
                    loading: false,
                    noResultMessage: this.localize("NoMatchMessage")
                });
            }
            catch (error) {
                return error;
            }
        }
    }
}

const manageGroupsWithTranslation = withTranslation()(ManageGroups);
export default manageGroupsWithTranslation;