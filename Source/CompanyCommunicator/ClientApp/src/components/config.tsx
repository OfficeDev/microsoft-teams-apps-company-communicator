import React from 'react';
import * as microsoftTeams from "@microsoft/teams-js";


export interface IconfigState {
    selectedButton: undefined
}

class Configuration extends React.Component<{}, IconfigState>{
    constructor(props: {}) {
        super(props);
        this.state = {
            selectedButton: undefined
        }
    }

    componentDidMount() {
        microsoftTeams.initialize();

        microsoftTeams.settings.registerOnSaveHandler((saveEvent) => {

            if (this.state.selectedButton === "list") {
                microsoftTeams.settings.setSettings({
                    entityId: "EmployeeStatus",
                    contentUrl: "https://9842365a.ngrok.io/list",
                    suggestedDisplayName: "Emp Connects Status",
                    websiteUrl: "https://9842365a.ngrok.io/list",
                });
            }
            else {
                microsoftTeams.settings.setSettings({
                    entityId: "EmployeeStatus",
                    contentUrl: "https://9842365a.ngrok.io/list",
                    suggestedDisplayName: "Emp Connects Status",
                    websiteUrl: "https://9842365a.ngrok.io/list",
                });
            }

            saveEvent.notifySuccess();
        });

    }

    render() {
        return (
            <div>
                <form>
                    <input type="radio" name="list" value="list" onClick={this.onClickButton} /> List Control UI Sample
               </form>
            </div>
        );
    }


    onClickButton = (event: any) => {
        microsoftTeams.settings.setValidityState(true);
        this.setState({
            selectedButton: event.target.value
        });
    }

}

export default Configuration;
