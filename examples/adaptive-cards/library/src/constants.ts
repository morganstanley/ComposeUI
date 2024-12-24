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