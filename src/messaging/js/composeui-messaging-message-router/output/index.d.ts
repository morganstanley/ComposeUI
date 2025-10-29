import { IMessaging } from "@morgan-stanley/composeui-messaging-abstractions";
declare global {
    interface Window {
        composeui: {
            messaging: {
                communicator: IMessaging | undefined;
            };
        };
    }
}
