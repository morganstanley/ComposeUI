//TODO: Byte can't be found because of a possible breaking change. Investigate why Byte is used from here and why a DTO depends on two framework libs.
// Check out https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Int8Array and other formats.

//import { Byte } from "@angular/compiler/src/util";
import { Guid } from "igniteui-angular-core";

export class ModuleInfo{
    Version: Guid;
    Name: string;
    VersionRedirectedFrom: string;
   // PublicKeyToken: Array<Byte>;
    PublicKeyToken: Array<any>;
    Location: string;
    Information: Array<any>;
}