export class BrowserWindow {

    /*
         const win = new BrowserWindow({
        width: 800,
        height: 600
    });

    win.loadUrl('index.html');
     
     */

    //private url: string;
    constructor(private width?: number, private height?: number) {
        console.debug("width", width, "height", height);
    }

    /*
    public loadUrl(_url: string){
        this.url = _url;

        console.debug("URL", this.url);

    }*/
}