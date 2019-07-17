import { IDetailsListStyles, IStyle } from 'office-ui-fabric-react';

export const getDetailsListHeaderStyle = (): Partial<IDetailsListStyles> => {
    return {
        headerWrapper: {
            backgroundColor: 'gray',
            padding: 0,
            fontSize: 12,

            selectors: {
                '.ms-DetailsHeader': {
                    border: 0,
                    padding: 0,
                },
                '.ms-DetailsHeader-cellIsCheck': {
                    backgroundColor: 'rgb(243,242,241)',
                },
            }
        }
    };
}

export const getDetailsListHeaderColumnStyle = (): IStyle => {
    return {
        backgroundColor: 'rgb(243,242,241)',
        fontSize: 12,
        paddingLeft: 0,
    };
}