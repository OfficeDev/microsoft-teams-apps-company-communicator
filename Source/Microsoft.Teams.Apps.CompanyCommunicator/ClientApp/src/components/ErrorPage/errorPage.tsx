import React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Text } from '@stardust-ui/react';
import './errorPage.scss';

const ErrorPage: React.FunctionComponent<RouteComponentProps> = props => {
    const { t } = useTranslation();

    function parseErrorMessage(): string {
        const params = props.match.params;
        if ('id' in params) {
            const id = params['id'];
            if (id === "401") {
                return t("UnauthorizedErrorMessage");
            } else if (id === "403") {
                return t("ForbiddenErrorMessage");
            }
        }
        return t("GeneralErrorMessage");
    }

    return (
        <Text content={parseErrorMessage()} className="error-message" error size="medium" />
    );
};

export default ErrorPage;