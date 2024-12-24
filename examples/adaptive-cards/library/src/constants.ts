import error from './img/error.png';
import info from './img/info.png';
import success from './img/sucess.png';

export const hostConfig = (bgColor:string) => {
    return {
        fontFamily: "Segoe UI, Helvetica Neue, sans-serif",
        containerStyles: {
            default: {
                backgroundColor: `${bgColor}`,
            },
        },
    };
};

export const images = [error,info,success];

export enum Icons {
    Info=info,
    Err=error,
    Success=success
}

export enum Types {
    Info="Information",
    Err="Error",
    Success="Success"
}

export function checkType(type:string)  {
    switch (type) {
        case "error":
            return Types.Err;
        case "success":
            return Types.Success;
        default:
            return Types.Info;
    }
}

export function getIcon(type:Types) {
    switch (type) {
        case Types.Err:
            return Icons.Err;
        case Types.Success:
            return Icons.Success;
        case Types.Info:
            return Icons.Info;
    }
}


