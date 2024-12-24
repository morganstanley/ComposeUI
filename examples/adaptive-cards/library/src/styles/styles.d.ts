declare module '*.css' {
    interface Styles {
      [key: string]: string;
    }
    const content: string;
    export = content;
  }