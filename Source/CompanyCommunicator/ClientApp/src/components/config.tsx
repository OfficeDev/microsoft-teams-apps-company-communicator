import React from 'react';
import * as microsoftTeams from "@microsoft/teams-js";

class Configuration extends React.Component {
    constructor(props: {}) {
        super(props);
    }

    componentDidMount() {
        microsoftTeams.initialize();

        microsoftTeams.settings.registerOnSaveHandler((saveEvent) => {
            microsoftTeams.settings.setSettings({
                entityId: "Company_Communicator_App",
                contentUrl: "https://6e444fb7.ngrok.io/messages",
                suggestedDisplayName: "Company Communicator Messages",
                websiteUrl: "https://6e444fb7.ngrok.io/messages",
            });
            saveEvent.notifySuccess();
        });

        microsoftTeams.settings.setValidityState(true);

    }

    render() {
        return (
            <div>
                <h3>Company Communicator App Configuration Page</h3>
            </div>
        );
    }

}

export default Configuration;
